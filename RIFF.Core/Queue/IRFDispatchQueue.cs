// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Threading;

namespace RIFF.Core
{
    /// <summary>
    /// Interface for classes which dispatch work items in dependency-aware order
    /// </summary>
    internal interface IRFDispatchQueue : IDisposable
    {
        IRFDispatchStore DispatchStore { get; }

        void ProcessingFinished(RFWorkQueueItem i, RFProcessingResult result);

        int QueuedInstructions(string processingKey);

        void QueueItem(RFWorkQueueItem i);

        RFWorkQueueItem WaitNextItem(RFProcessingMode processingMode, CancellationToken token);
    }
}
