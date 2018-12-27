// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RIFF.Core
{
    /// <summary>
    /// Queue that releases work items in dependency order and only when there's no conflict with
    /// calculations being run
    /// </summary>
    internal class RFGraphDispatchQueue : IRFDispatchQueue
    {
        public IRFDispatchStore DispatchStore { get { return _dispatchStore; } }
        private IRFDispatchStore _dispatchStore;
        private List<RFWorkQueueItem> _inProgress; // track what is in progress
        private Dictionary<string, int> _numQueuedInstructions; // number of outstanding instructions per processing key (to determine if requests have finished)
        private SortedDictionary<long, List<RFWorkQueueItem>> _pendingInstructions; // work items in the queue but not ready to be processed due to dependencies being worked on
        private Dictionary<string, SortedSet<string>> _processDependencies; // static data of dependencies between processes
        private Dictionary<string, int> _processWeights; // static data of calculation order
        private SortedSet<string> _exclusiveProcesses; // tasks which cannot be run concurrently
        private List<RFWorkQueueItem> _readyQueue; // work items ready to be sent to workers
        private volatile object _statsSync;
        private volatile object _sync; // internal monitor

        // internal sync for statistics
        public RFGraphDispatchQueue(Dictionary<string, int> weights, Dictionary<string, SortedSet<string>> dependencies, SortedSet<string> exclusiveProcesses, RFComponentContext context)
        {
            _sync = new object();
            _statsSync = new object();
            _processWeights = weights;
            _inProgress = new List<RFWorkQueueItem>();
            _pendingInstructions = new SortedDictionary<long, List<RFWorkQueueItem>>();
            _readyQueue = new List<RFWorkQueueItem>();
            _processDependencies = dependencies ?? new Dictionary<string, SortedSet<string>>();
            _dispatchStore = context.DispatchStore;
            _numQueuedInstructions = new Dictionary<string, int>();
            _exclusiveProcesses = exclusiveProcesses;
        }

        public void ProcessingFinished(RFWorkQueueItem i, RFProcessingResult result)
        {
            Monitor.Enter(_sync);
            if (i.Item is RFProcessInstruction)
            {
                _inProgress.Remove(i);//.Item as RFProcessInstruction);
                RFStatic.Log.Debug(typeof(RFGraphDispatchQueue), "Processing finished for {0} ({1} in progress)", i.Item, _inProgress.Count);
                _dispatchStore.Finished(i, result);
            }
            RefreshReadyQueue();
            RefreshInstructionCount();
            if (!InProgress(i.ProcessingKey))
            {
                QueueItem(new RFWorkQueueItem { Item = new RFRequestCompleted(), ProcessingKey = i.ProcessingKey }); // notify monitor this request is done
            }
            Monitor.PulseAll(_sync); // inprogress finished and nothing left
            Monitor.Exit(_sync);
        }

        public int QueuedInstructions(string processingKey)
        {
            if (processingKey != null)
            {
                lock (_statsSync)
                {
                    if (_numQueuedInstructions.ContainsKey(processingKey))
                    {
                        return _numQueuedInstructions[processingKey];
                    }
                }
            }
            return 0;
        }

        public void QueueItem(RFWorkQueueItem i)
        {
            if (i.Item == null)
            {
                throw new RFSystemException(this, "Invalid empty work item received.");
            }
            Monitor.Enter(_sync);

            // ignore if already running or queued (timers)
            if (i.Item is RFProcessInstruction && (_inProgress.Any() || _readyQueue.Any()))
            {
                if (i.Item is RFGraphProcessInstruction)
                {
                    // if one processor subscribes to multiple outputs of another processor duplicate
                    // instruction can happen
                    var gpi = i.Item as RFGraphProcessInstruction;
                    var running = _inProgress.Where(p => p.Item is RFGraphProcessInstruction).Select(p => p.Item as RFGraphProcessInstruction).ToList();
                    if (running.Any(r => r.Instance.Equals(gpi.Instance) && r.ProcessName == gpi.ProcessName))
                    {
                        RFStatic.Log.Debug(this, "Ignoring instruction {0} for as we have one running already", gpi);
                        return; // ignore already in progress
                    }

                    var ready = _readyQueue.Where(p => p.Item is RFGraphProcessInstruction).Select(p => p.Item as RFGraphProcessInstruction).ToList();
                    if (ready.Any(r => r.Instance.Equals(gpi.Instance) && r.ProcessName == gpi.ProcessName))
                    {
                        RFStatic.Log.Debug(this, "Ignoring instruction {0} for as we have one ready already", gpi);
                        return; // ignore already ready
                    }
                }
                // not sure this was used - probably not since this throws away an instruction rather than delays it
                /*
                else
                {
                    var pi = i.Item as RFProcessInstruction;
                    if (pi.Event is RFIntervalEvent) // allow concurrent processing of multiple files or external triggers but not timers
                    {
                        if (_inProgress.Any())
                        {
                            var inProgress = _inProgress.Where(q => q.Item is RFProcessInstruction).Select(q => q.Item as RFProcessInstruction);
                            if (inProgress.Any(q => q.ProcessName == pi.ProcessName))
                            {
                                return; // ignore already in progress - for example FTP taking longer than timed trigger
                            }
                        }

                        if (_readyQueue.Any())
                        {
                            var queued = _readyQueue.Where(q => q.Item is RFProcessInstruction).Select(q => q.Item as RFProcessInstruction);
                            if (queued.Any(q => q.ProcessName == pi.ProcessName))
                            {
                                return; // ignore already same process queued
                            }
                        }
                    }
                }*/
            }

            if (i.Item is RFGraphProcessInstruction)
            {
                var gpi = i.Item as RFGraphProcessInstruction;

                if (gpi.Instance != null && gpi.Instance.ValueDate.HasValue)
                {
                    long datePart = (gpi.Instance.ValueDate.Value.ToYMD() - (long)20000000) * 1000000;

                    if (_processWeights.ContainsKey(gpi.ProcessName))
                    {
                        var weight = _processWeights[gpi.ProcessName];
                        var queueKey = datePart + weight;
                        if (_pendingInstructions.ContainsKey(queueKey))
                        {
                            var potentialDupes = _pendingInstructions[queueKey].Select(d => d.Item as RFGraphProcessInstruction);

                            // ignore duplicate
                            if (potentialDupes.Any(qi => qi.ProcessName.Equals(gpi.ProcessName) && qi.Instance.Equals(gpi.Instance)))
                            {
                                RFStatic.Log.Debug(this, "Ignoring instruction {0} for as we have one in the queue already", gpi);
                                Monitor.Exit(_sync);
                                return;
                            }
                        }
                        else
                        {
                            _pendingInstructions.Add(queueKey, new List<RFWorkQueueItem>());
                        }

                        _pendingInstructions[queueKey].Add(i);
                        _dispatchStore.Queued(i, queueKey);
                    }
                    else
                    {
                        RFStatic.Log.Info(this, "Assuming zero weighted for unweighted graph instruction {0}/{1}", gpi.ProcessName,
                        gpi.Instance.ToString());

                        if (!_pendingInstructions.ContainsKey(datePart))
                        {
                            _pendingInstructions.Add(datePart, new List<RFWorkQueueItem>());
                        }
                        _pendingInstructions[datePart].Add(i);
                        _dispatchStore.Queued(i, datePart);
                    }
                }
                else
                {
                    RFStatic.Log.Warning(this, "Received graph instruction for process {0} without instance date.", gpi.ProcessName);
                    if (!_pendingInstructions.ContainsKey(0))
                    {
                        _pendingInstructions.Add(0, new List<RFWorkQueueItem>());
                    }
                    _pendingInstructions[0].Add(i); // no instance date
                    _dispatchStore.Queued(i, 0);
                }
                RefreshReadyQueue();
            }
            else if (i.Item is RFProcessInstruction)
            {
                var pi = i.Item as RFProcessInstruction;
                if (!_pendingInstructions.ContainsKey(0))
                {
                    _pendingInstructions.Add(0, new List<RFWorkQueueItem>());
                }
                // if we already have an identical pending instruction, we ignore (meaning we'll
                // still queue if there's an identical one already *in progress*) the logic here is
                // that if there's been multiple instances of the same event, we only process once
                var potentialDupes = _pendingInstructions[0].Select(p => p.Item as RFProcessInstruction).Where(p => p != null && p.ProcessName == pi.ProcessName);
                if (!potentialDupes.Any(d => RFEngineProcessorParam.Equals(d?.ExtractParam(), pi?.ExtractParam())))
                {
                    _pendingInstructions[0].Add(i); // no instance date
                    _dispatchStore.Queued(i, 0);
                    RefreshReadyQueue();
                }
                /*else
                {
                    RFStatic.Log.Debug(this, "Ignoring already pending RFProcessInstruction {0}", pi);
                }*/
            }
            else
            {
                _readyQueue.Add(i); // straight to readyqueue
                _dispatchStore.Queued(i, 0);
                Monitor.PulseAll(_sync);
            }
            RefreshInstructionCount();
            Monitor.Exit(_sync);
        }

        public RFWorkQueueItem WaitNextItem(RFProcessingMode processingMode, CancellationToken token)
        {
            try
            {
                Monitor.Enter(_sync);
                if (token.IsCancellationRequested)
                {
                    Monitor.Exit(_sync);
                    return null;
                }
                while (!_readyQueue.Any() && !token.IsCancellationRequested)
                {
                    Monitor.Wait(_sync);
                }
                if (token.IsCancellationRequested)
                {
                    Monitor.Exit(_sync);
                    return null;
                }
                var i = _readyQueue.First();
                _readyQueue.Remove(i);
                ProcessingStarted(i);
                RefreshInstructionCount();
                Monitor.PulseAll(_sync);
                Monitor.Exit(_sync);
                return i;
            }
            catch (ThreadInterruptedException)
            {
                RFStatic.Log.Info(typeof(RFGraphDispatchQueue), "Aborting WaitNextItem");
                Monitor.Exit(_sync);
                return null;
            }
            catch (ThreadAbortException)
            {
                RFStatic.Log.Info(typeof(RFGraphDispatchQueue), "Aborting WaitNextItem");
                Monitor.Exit(_sync);
                return null;
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(typeof(RFGraphDispatchQueue), "Exception in WaitNextItem", ex);
                Monitor.Exit(_sync);
                return null;
            }
        }

        // can start if none of its dependencies are running or about to run
        private bool CanStart(RFWorkQueueItem i)
        {
            if (i.Item is RFGraphProcessInstruction)
            {
                var gpi = i.Item as RFGraphProcessInstruction;
                if (_processDependencies.ContainsKey(gpi.ProcessName))
                {
                    var depends = _processDependencies[gpi.ProcessName];
                    return !_inProgress.Select(p => p.Item as RFProcessInstruction).Any(p => p != null && depends.Contains(p.ProcessName))
                        && !_readyQueue.Select(p => p.Item as RFProcessInstruction).Any(p => p != null && depends.Contains(p.ProcessName));
                }
            }
            else if (i.Item is RFProcessInstruction)
            {
                // queue up identical instructions
                var pi = i.Item as RFProcessInstruction;
                var existingForSameProcess = _inProgress.Select(p => p.Item as RFProcessInstruction).Where(p => p != null && p.ProcessName == pi.ProcessName)
                    .Union(_readyQueue.Select(p => p.Item as RFProcessInstruction).Where(p => p != null && p.ProcessName == pi.ProcessName));
                if (existingForSameProcess.Any())
                {
                    if(_exclusiveProcesses.Contains(pi.ProcessName))
                    {
                        // cannot run concurrently - queue
                        return false;
                    }
                    return !existingForSameProcess.Any(e => RFEngineProcessorParam.Equals(e.ExtractParam(), pi.ExtractParam())); // catalog update on same key events will be queued
                }
            }
            return true;
        }

        private bool InProgress(string processingKey)
        {
            return string.IsNullOrWhiteSpace(processingKey) || QueuedInstructions(processingKey) != 0;
        }

        private bool IsEmpty()
        {
            return !_pendingInstructions.Any() && !_readyQueue.Any() && !_inProgress.Any();
        }

        private void ProcessingStarted(RFWorkQueueItem item)
        {
            if (item.Item is RFProcessInstruction)
            {
                _inProgress.Add(item);
                RFStatic.Log.Debug(typeof(RFGraphDispatchQueue), "Processing started for {0} ({1} in progress)", item.Item, _inProgress.Count);
                _dispatchStore.Started(item);
            }
            RefreshReadyQueue();
        }

        private void RefreshInstructionCount()
        {
            lock (_statsSync)
            {
                _numQueuedInstructions =
                    _readyQueue.Select(q => q.ProcessingKey)
                    .Union(_pendingInstructions.SelectMany(p => p.Value.Select(v => v.ProcessingKey)))
                    .Union(_inProgress.Select(i => i.ProcessingKey)).Where(q => q != null)
                    .GroupBy(p => p).ToDictionary(p => p.Key, p => p.Count());
            }
        }

        private void RefreshReadyQueue()
        {
            if (!_readyQueue.Any() && _pendingInstructions.Any())
            {
                // if there's nothing in ready queue, see if anything can be added
                foreach (var qk in _pendingInstructions)
                {
                    foreach (var qv in qk.Value)
                    {
                        if (CanStart(qv))
                        {
                            qk.Value.Remove(qv);
                            _readyQueue.Add(qv);
                            if (!qk.Value.Any())
                            {
                                _pendingInstructions.Remove(qk.Key);
                            }
                            Monitor.PulseAll(_sync);
                            return; // up to one at the time
                        }
                    }
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above. GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    //mReadyQueue.Dispose();
                    _readyQueue = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~RFGraphWorkQueue() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}
