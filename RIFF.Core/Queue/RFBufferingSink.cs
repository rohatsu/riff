// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    /// <summary>
    /// Implementation of event sink that holds onto the events, so that they can be sent in bulk later
    /// </summary>
    internal class RFBufferingSink : IRFEventSink, IRFInstructionSink
    {
        private List<RFWorkQueueItem> _items;

        public RFBufferingSink()
        {
            _items = new List<RFWorkQueueItem>();
        }

        public RFWorkQueueItem[] GetItems()
        {
            return _items.ToArray();
        }

        public void QueueInstruction(object issuedBy, RFInstruction i, string processingKey)
        {
            _items.Add(new RFWorkQueueItem
            {
                Item = i,
                ProcessingKey = processingKey
            });
        }

        public void RaiseEvent(object raisedBy, RFEvent e, string processingKey)
        {
            _items.Add(new RFWorkQueueItem
            {
                Item = e,
                ProcessingKey = processingKey
            });
        }
    }
}
