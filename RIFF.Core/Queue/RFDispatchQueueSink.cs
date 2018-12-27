// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    /// <summary>
    /// Sink implementation that sends all events to internal dispatch queue
    /// </summary>
    internal class RFDispatchQueueSink : RFPassiveComponent, IRFInstructionSink, IRFEventSink
    {
        private IRFDispatchQueue _destinationQueue;

        public RFDispatchQueueSink(RFComponentContext context, IRFDispatchQueue destinationQueue)
        : base(context)
        {
            _destinationQueue = destinationQueue;
        }

        public void QueueInstruction(object issuedBy, RFInstruction i, string processingKey)
        {
            _destinationQueue.QueueItem(new RFWorkQueueItem
            {
                Item = i,
                ProcessingKey = processingKey
            });
            if (i.ExtractParam() is RFEngineProcessorKeyParam)
            {
                // don't log interval updates
                if ((i.ExtractParam() as RFEngineProcessorKeyParam)?.Key.Plane == RFPlane.Ephemeral)
                {
                    return;
                }
            }
            RFStatic.Log.LogInstruction(issuedBy, i);
        }

        public void RaiseEvent(object raisedBy, RFEvent e, string processingKey)
        {
            _destinationQueue.QueueItem(new RFWorkQueueItem
            {
                Item = e,
                ProcessingKey = processingKey
            });
            RFStatic.Log.LogEvent(raisedBy, e);
        }
    }
}
