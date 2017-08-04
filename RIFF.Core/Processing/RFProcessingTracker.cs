// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace RIFF.Core
{
    [DataContract]
    public class RFProcessingTracker
    {
        [IgnoreDataMember]
        public ManualResetEvent CompletionEvent { get; private set; }

        [DataMember]
        public string CurrentProcess { get; private set; }

        [DataMember]
        public DateTimeOffset? EndTime { get; private set; }

        [DataMember]
        public string Error { get; private set; }

        [DataMember]
        public int FinishedCycles { get; private set; }

        [DataMember]
        public bool IsComplete { get; private set; }

        [IgnoreDataMember]
        public List<RFCatalogKey> Keys { get; private set; }

        [DataMember]
        public int KeyCount { get; private set; }

        // TODO: make getters thread-safe?
        [DataMember]
        public Dictionary<string, string> Messages { get; private set; }

        [DataMember]
        public int ProcessingCycles { get; private set; }

        [DataMember]
        public string ProcessingKey { get; private set; }

        [DataMember]
        public int RemainingCycles { get; private set; }

        [DataMember]
        public DateTimeOffset StartTime { get; private set; }

        [IgnoreDataMember]
        protected object mSync = new object();

        public static RFProcessingTracker Dummy => new RFProcessingTracker("dummy")
        {
            IsComplete = true,
            EndTime = DateTime.Now
        };

        public RFProcessingTracker(string processingKey)
        {
            ProcessingKey = processingKey;
            CompletionEvent = new ManualResetEvent(false);
            Messages = new Dictionary<string, string>();
            Keys = new List<RFCatalogKey>();
            CurrentProcess = "initializing";
            IsComplete = false;
            FinishedCycles = 0;
            RemainingCycles = 0;
            ProcessingCycles = 0;
            Error = String.Empty;
            StartTime = DateTimeOffset.Now;
            EndTime = null;
        }

        public void CycleFinished(string processName, RFProcessingResult result)
        {
            lock (mSync)
            {
                FinishedCycles++;
                ProcessingCycles--;
                var singleMessage = result.Messages?.FirstOrDefault();
                if (result.IsError)
                {
                    LogError(singleMessage ?? "Unknown error");
                    LogMessage(processName, "ERROR: " + singleMessage ?? String.Empty);
                }
                else if (result.UpdatedKeys != null && result.UpdatedKeys.Count > 0)
                {
                    LogMessage(processName, singleMessage ?? string.Format("OK ({0} update{1})", result.UpdatedKeys.Count, result.UpdatedKeys.Count == 1 ? "" : "s"));
                }
                else if (result.WorkDone)
                {
                    LogMessage(processName, singleMessage ?? "OK");
                }
                // otherwise don't bother to output name - no updates
                if (result.UpdatedKeys != null)
                {
                    foreach (var k in result.UpdatedKeys)
                    {
                        LogKey(k);
                    }
                }
            }
        }

        public void CyclesRemaining(int numCycles)
        {
            lock (mSync)
            {
                RemainingCycles = numCycles;
            }
        }

        public void CycleStarted(string processName)
        {
            lock (mSync)
            {
                SetProcess(processName);
                ProcessingCycles++;
            }
        }

        public TimeSpan GetDuration()
        {
            if (IsComplete && EndTime.HasValue)
            {
                return EndTime.Value - StartTime;
            }
            else
            {
                return DateTimeOffset.Now - StartTime;
            }
        }

        public bool IsError()
        {
            return !string.IsNullOrWhiteSpace(Error);
        }

        public bool IsOK()
        {
            return !IsError() && IsComplete;
        }

        public void SetComplete()
        {
            lock (mSync)
            {
                if (IsComplete)
                {
                    RFStatic.Log.Info(typeof(RFProcessingTracker), "Second completion event for request {0}", ProcessingKey);
                }
                else
                {
                    RFStatic.Log.Info(typeof(RFProcessingTracker), "First completion event for request {0}", ProcessingKey);
                    CyclesRemaining(0);
                    ProcessingCycles = 0;
                    IsComplete = true;
                    EndTime = DateTimeOffset.Now;
                    CompletionEvent.Set();
                }
            }
        }

        private void LogError(string error)
        {
            if (string.IsNullOrWhiteSpace(Error))
            {
                Error = error;
            }
        }

        private void LogKey(RFCatalogKey key)
        {
            lock (mSync)
            {
                Keys.Add(key);
                KeyCount++;
            }
        }

        private void LogMessage(string process, string message)
        {
            lock (mSync)
            {
                if (!Messages.ContainsKey(process))
                {
                    Messages.Add(process, message);
                }
                else
                {
                    // overwrite previous
                    Messages[process] = message;
                }
            }
        }

        private void SetProcess(string process)
        {
            lock (mSync)
            {
                CurrentProcess = process;
            }
        }
    }

    [DataContract]
    public class RFProcessingTrackerHandle
    {
        [DataMember]
        public string TrackerCode { get; set; }
    }
}
