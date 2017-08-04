// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public enum RFEventType
    {
        [EnumMember]
        System = 1,

        [EnumMember]
        Interval = 2,

        [EnumMember]
        Scheduler = 3,

        [EnumMember]
        User = 4
    }

    [DataContract]
    public class RFCatalogUpdateEvent : RFEvent
    {
        [DataMember]
        public RFCatalogKey Key { get; set; }

        public override bool Equals(object obj)
        {
            return Key?.Equals((obj as RFCatalogUpdateEvent)?.Key) ?? false;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    [DataContract]
    public class RFEvent : IRFWorkQueueableItem
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        public RFEvent()
        {
            Timestamp = DateTime.Now;
        }

        public ItemType DispatchItemType()
        {
            return ItemType.NotSupported;
        }

        public string DispatchKey()
        {
            return "Event";
        }
    }

    /// <summary>
    /// Contains user-readable results and all the artifacts (events and instructions) resulting from processing
    /// </summary>
    [DataContract]
    public class RFProcessingFinishedEvent : RFEvent
    {
        [DataMember]
        public RFWorkQueueItem Item { get; set; }

        [DataMember]
        public RFProcessingResult Result { get; set; }

        [DataMember]
        public RFWorkQueueItem[] WorkQueueItems { get; set; }

        public RFProcessingFinishedEvent(RFWorkQueueItem item, RFProcessingResult result, RFWorkQueueItem[] queueItems)
        {
            Item = item;
            Timestamp = DateTime.Now;
            Result = result;
            WorkQueueItems = queueItems;
        }

        public string GetFinishedProcessName()
        {
            if (Item.Item is RFProcessInstruction)
            {
                return (Item.Item as RFProcessInstruction).ProcessName;
            }
            return null;
        }
    }

    [DataContract]
    public class RFProcessingResult
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public SortedSet<string> Messages { get; set; }

        [DataMember]
        public bool ShouldRetry { get; set; }

        [DataMember]
        public List<RFCatalogKey> UpdatedKeys { get; set; }

        [DataMember]
        public bool WorkDone { get; set; }

        public static RFProcessingResult Error(string message, bool shouldRetry)
        {
            return new RFProcessingResult
            {
                WorkDone = true,
                Messages = new SortedSet<string> { message },
                IsError = true,
                ShouldRetry = shouldRetry
            };
        }

        public static RFProcessingResult Error(IEnumerable<string> messages, bool shouldRetry)
        {
            return new RFProcessingResult
            {
                WorkDone = true,
                Messages = messages != null ? new SortedSet<string>(messages) : new SortedSet<string>(),
                IsError = true,
                ShouldRetry = shouldRetry
            };
        }

        public static RFProcessingResult Success(bool workDone, IEnumerable<string> messages = null)
        {
            return new RFProcessingResult
            {
                WorkDone = workDone,
                Messages = messages != null ? new SortedSet<string>(messages) : new SortedSet<string>()
            };
        }

        public void AddMessage(string message)
        {
            if (message.NotBlank())
            {
                if (Messages == null)
                {
                    Messages = new SortedSet<string> { message };
                }
                else
                {
                    Messages.Add(message);
                }
            }
        }

        public void AddMessages(IEnumerable<string> messages)
        {
            if (messages != null)
            {
                if (Messages == null)
                {
                    Messages = new SortedSet<string>(messages);
                }
                else
                {
                    Messages.UnionWith(messages);
                }
            }
        }

        // if an error, can it be retried later (i.e. database connection issue etc.)
    }

    public interface IRFWorkQueueableItem
    {
        ItemType DispatchItemType();

        string DispatchKey();
    }
}
