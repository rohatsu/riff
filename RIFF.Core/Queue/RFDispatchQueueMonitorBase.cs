// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Threading;

namespace RIFF.Core
{
    /// <summary>
    /// Thread class waiting on dispatch queue and passing over its work items to worker queue
    /// </summary>
    internal abstract class RFDispatchQueueMonitorBase : RFActiveComponent
    {
        public IRFDispatchQueue DispatchQueue { get { return _dispatchQueue; } }
        protected IRFDispatchQueue _dispatchQueue;
        protected IRFEventSink _eventSink;
        protected IRFInstructionSink _instructionSink;
        protected RFIntervalComponent _interval;
        protected RFRequestTracker _requestTracker;

        protected RFDispatchQueueMonitorBase(RFComponentContext context, IRFInstructionSink instructionSink, IRFEventSink eventSink, IRFDispatchQueue dispatchQueue)
        : base(context)
        {
            _requestTracker = new RFRequestTracker();
            _instructionSink = instructionSink;
            _eventSink = eventSink;
            _dispatchQueue = dispatchQueue;
        }

        /// <summary>
        /// Ensure there's work to be done, if false there's nothing left
        /// </summary>
        /// <param name="processingKey"></param>
        public bool BeginRequest(string processingKey)
        {
            if (!string.IsNullOrWhiteSpace(processingKey))
            {
                if (_dispatchQueue.QueuedInstructions(processingKey) == 0)
                {
                    // looks like no work will be done (?)
                    _requestTracker.RequestFinished(processingKey);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Prepare queue for submitting request's events
        /// </summary>
        public RFProcessingTracker PrepareRequest(string processingKey)
        {
            return _requestTracker.RequestStarted(processingKey);
        }

        protected virtual void PrepareToRun()
        {
        }

        protected virtual void PrepareToStop()
        {
        }

        protected void ProcessItem(RFWorkQueueItem item)
        {
            if (item != null)
            {
                try
                {
                    var content = item.Item;
                    if (content is RFInstruction i)
                    {
                        if (i.ForceProcessLocally())
                        {
                            // processing results will go directly into our internal queues rather than be distributed
                            var result = _context.Engine.Process(content as RFInstruction, _context.GetProcessingContext(item.ProcessingKey, _instructionSink, _eventSink, this));
                            ProcessingFinished(item, result);
                        }
                        else
                        {
                            Log.Debug(this, "Received instruction {0}", content);

                            ProcessQueueItem(item);
                        }
                    }
                    else if (content is RFEvent)
                    {
                        if (!(content is RFIntervalEvent) && !((content is RFCatalogUpdateEvent) && (content as RFCatalogUpdateEvent).Key.Plane == RFPlane.Ephemeral))
                        {
                            Log.Debug(this, "Received event {0}", content);
                        }
                        if (content is RFProcessingFinishedEvent)
                        {
                            var fe = content as RFProcessingFinishedEvent;

                            RFStatic.Log.Debug(this, "Processing finished event for {0} with {1} events", fe.Item, (fe.WorkQueueItems ?? new RFWorkQueueItem[0]).Length);
                            // queue all resulting events and instructions
                            foreach (var wi in fe.WorkQueueItems ?? new RFWorkQueueItem[0])
                            {
                                if (wi.Item is RFEvent)
                                {
                                    // react to events immediately as they might queue calculations
                                    // and impact calculation order
                                    _context.Engine.React(wi.Item as RFEvent, _context.GetProcessingContext(item.ProcessingKey, _instructionSink, _eventSink, this));
                                }
                                else
                                {
                                    // queue process instructions
                                    _dispatchQueue.QueueItem(wi);
                                }
                            }

                            // this will ensure all processing artifacts are queued before marking
                            // processing as complete (avoid finishing while events still pending)
                            ProcessingFinished(fe.Item, fe.Result);
                        }
                        _context.Engine.React(content as RFEvent, _context.GetProcessingContext(item.ProcessingKey, _instructionSink, _eventSink, this));
                        _dispatchQueue.ProcessingFinished(item, null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Exception processing queue item ", item);
                }
            }
        }

        protected abstract void ProcessQueueItem(RFWorkQueueItem item);

        protected override void Run()
        {
            PrepareToRun();

            if (_context.SystemConfig.ProcessingMode != RFProcessingMode.RFSinglePass)
            {
                // TODO: move this to environment initialization
                _interval = new RFIntervalComponent(_context, _eventSink);
                _interval.StartThread();
            }
            try
            {
                do
                {
                    var nextItem = _dispatchQueue.WaitNextItem(_context.SystemConfig.ProcessingMode, _context.CancellationTokenSource.Token);
                    if (nextItem == null)
                    {
                        _isExiting = true;
                        PrepareToStop();
                        return;
                    }
                    else if (nextItem.Item is RFRequestCompleted)
                    {
                        _requestTracker.RequestFinished(nextItem.ProcessingKey);
                    }
                    else
                    {
                        _requestTracker.CycleStarted(nextItem, _dispatchQueue.QueuedInstructions(nextItem.ProcessingKey));
                        ProcessItem(nextItem);
                    }
                }
                while (!IsExiting());
            }
            catch (ThreadAbortException)
            {
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Exception in main thread!");
            }
            Log.Debug(this, "Exiting...");
            PrepareToStop();
        }

        protected override void Stop()
        {
            if (_interval != null)
            {
                _interval.Shutdown();
            }
            _thread.Interrupt();
        }

        private void ProcessingFinished(RFWorkQueueItem i, RFProcessingResult result)
        {
            _requestTracker.CycleFinished(i, result);
            _dispatchQueue.ProcessingFinished(i, result);
        }
    }
}
