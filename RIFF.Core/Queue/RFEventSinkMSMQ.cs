// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Messaging;

namespace RIFF.Core
{
    /// <summary>
    /// Sends all events from a worker to MSMQ for them to be picked up by queue monitor
    /// </summary>
    internal class RFEventSinkMSMQ : IRFEventSink
    {
        private readonly MessageQueue _eventQueue;

        public RFEventSinkMSMQ(MessageQueue eventQueue)
        {
            _eventQueue = eventQueue;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
        public void RaiseEvent(object raisedBy, RFEvent e, string processingKey)
        {
            lock (_eventQueue) // Send is not thread-safe, would need to use thread-local instances
            {
                _eventQueue.Send(new RFWorkQueueItem { Item = e, ProcessingKey = processingKey });
            }
            RFStatic.Log.Debug(typeof(RFEventSinkMSMQ), "Sent event {0} to MSMQ", e);
        }
    }
}
