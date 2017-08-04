// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Threading.Tasks;

namespace RIFF.Core
{
    /// <summary>
    /// InProcess implementation of worker processes (no queues)
    /// </summary>
    internal class RFDispatchQueueMonitorInProc : RFDispatchQueueMonitorBase
    {
        public RFDispatchQueueMonitorInProc(RFComponentContext context, IRFInstructionSink instructionManager, IRFEventSink eventManager, IRFDispatchQueue workQueue)
        : base(context, instructionManager, eventManager, workQueue)
        {
            Log.Debug(this, "Using InProc Queue");
        }

        protected override void ProcessQueueItem(RFWorkQueueItem item)
        {
            Task.Run(() => ProcessInstructionThread(item), _context.CancellationTokenSource.Token);
        }

        private void ProcessInstructionThread(RFWorkQueueItem i)
        {
            try
            {
                Log.Debug(this, "Started thread to process instruction {0}", i.Item as RFProcessInstruction);
                var result = _context.Engine.Process(i.Item as RFInstruction, _context.GetProcessingContext(i.ProcessingKey, _instructionSink, _eventSink, this));
                _eventSink.RaiseEvent(this, new RFProcessingFinishedEvent(i, result, null), i.ProcessingKey); // we do not store work queue items in-proc
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Exception Thread processing queue item ", i);
                _eventSink.RaiseEvent(this, new RFProcessingFinishedEvent(i, RFProcessingResult.Error(new string[] { ex.Message }, false), null), i.ProcessingKey);
            }
        }
    }
}
