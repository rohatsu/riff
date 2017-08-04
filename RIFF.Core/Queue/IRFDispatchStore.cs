using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    public enum DispatchState
    {
        Unknown = 0,
        Queued = 1,
        Started = 2,
        Error = 3,
        Finished = 4,
        Ignored = 5,
        Skipped = 6
    }

    public enum ItemType
    {
        NotSupported = 0,
        Instruction = 1,
        ProcessInstruction = 2,
        GraphProcessInstruction = 3
    }

    [DataContract]
    public class RFErrorQueueItem
    {
        [DataMember]
        public string DispatchKey { get; set; }

        [DataMember]
        public DispatchState DispatchState { get; set; }

        [DataMember]
        public RFGraphInstance Instance { get; set; }

        [DataMember]
        public ItemType ItemType { get; set; }

        [DataMember]
        public DateTimeOffset? LastStart { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string ProcessName { get; set; }

        [DataMember]
        public bool ShouldRetry { get; set; }

        [DataMember]
        public long Weight { get; set; }
    }

    /// <summary>
    /// Persistence for Dispatch Queue serving as an error queue
    /// </summary>
    public interface IRFDispatchStore
    {
        void Finished(RFWorkQueueItem item, RFProcessingResult result);

        IEnumerable<RFErrorQueueItem> GetErrorQueue(int numRecentlyCompleted = 50);

        RFInstruction GetInstruction(string dispatchKey);

        void Ignored(string dispatchKey);

        void Queued(RFWorkQueueItem item, long weight);

        void Started(RFWorkQueueItem item);
    }
}
