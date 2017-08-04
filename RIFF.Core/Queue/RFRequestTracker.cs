// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    internal class RFRequestTracker
    {
        protected Dictionary<string, RFProcessingTracker> _requests;

        public RFRequestTracker()
        {
            _requests = new Dictionary<string, RFProcessingTracker>();
        }

        public void CycleFinished(RFWorkQueueItem i, RFProcessingResult result)
        {
            if (i.ProcessingKey != null && _requests.ContainsKey(i.ProcessingKey) && i.Item is RFProcessInstruction)
            {
                _requests[i.ProcessingKey].CycleFinished((i.Item as RFProcessInstruction).ToString(), result);
            }
        }

        public void CycleStarted(RFWorkQueueItem i, int cyclesRemaining)
        {
            if (i.ProcessingKey != null && _requests.ContainsKey(i.ProcessingKey) && i.Item is RFProcessInstruction)
            {
                _requests[i.ProcessingKey].CyclesRemaining(cyclesRemaining);
                _requests[i.ProcessingKey].CycleStarted((i.Item as RFProcessInstruction).ToString());
            }
        }

        public void RequestFinished(string processingKey)
        {
            if (processingKey != null && _requests.ContainsKey(processingKey))
            {
                _requests[processingKey].SetComplete();
            }
        }

        public RFProcessingTracker RequestStarted(string processingKey)
        {
            if (processingKey != null)
            {
                if (_requests.ContainsKey(processingKey))
                {
                    _requests.Remove(processingKey);
                }
                var tracker = new RFProcessingTracker(processingKey);
                _requests.Add(processingKey, tracker);
                return tracker;
            }
            return new RFProcessingTracker("null");
        }
    }
}
