// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Represents work item transferrable via MSMQ queues (event/instruction/result/completion)
    /// </summary>
    [DataContract]
    public class RFWorkQueueItem
    {
        [DataMember]
        public IRFWorkQueueableItem Item { get; set; }

        [DataMember]
        public string ProcessingKey { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is RFWorkQueueItem)
            {
                var q = obj as RFWorkQueueItem;
                if (q.Item.Equals(Item) && q.ProcessingKey == ProcessingKey)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Item.GetHashCode() + (ProcessingKey ?? "null").GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Item, ProcessingKey ?? "null");
        }
    }

    internal class RFRequestCompleted : IRFWorkQueueableItem
    {
        public ItemType DispatchItemType()
        {
            return ItemType.NotSupported;
        }

        public string DispatchKey()
        {
            return "RequestCompleted";
        }
    }
}
