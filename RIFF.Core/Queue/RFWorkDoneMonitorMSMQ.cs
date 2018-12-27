// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Messaging;

namespace RIFF.Core
{
    /// <summary>
    /// Thread which waits on event queue where workers send their results and events, and passes
    /// them to its sinks
    /// </summary>
    internal class RFWorkDoneMonitorMSMQ : RFActiveComponent
    {
        private readonly MessageQueue _eventQueue;
        private readonly IRFEventSink _eventSink;
        private readonly IRFInstructionSink _instructionSink;

        public RFWorkDoneMonitorMSMQ(MessageQueue eventQueue, RFComponentContext context, IRFInstructionSink instructionSink, IRFEventSink eventSink) : base(context)
        {
            _instructionSink = instructionSink;
            _eventSink = eventSink;
            _eventQueue = eventQueue;
            _eventQueue.ReceiveCompleted += _eventQueue_ReceiveCompleted;
        }

        protected override void Run()
        {
            _eventQueue.BeginReceive();
            _context.CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        protected override void Stop()
        {
            _thread.Interrupt();
        }

        private void _eventQueue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
        {
            if (!IsExiting())
            {
                if (e.Message?.Body != null && e.Message.Body is RFWorkQueueItem)
                {
                    var wki = e.Message.Body as RFWorkQueueItem;
                    if (wki.Item is RFEvent)
                    {
                        var evt = wki.Item as RFEvent;
                        Log.Debug(this, "Received event {0} from MSMQ", evt);
                        _eventSink.RaiseEvent(this, evt, wki.ProcessingKey);
                    }
                    else if (wki.Item is RFInstruction)
                    {
                        var ins = wki.Item as RFInstruction;
                        Log.Debug(this, "Received instruction {0} from MSMQ", ins);
                        _instructionSink.QueueInstruction(this, ins, wki.ProcessingKey);
                    }
                    else
                    {
                        Log.Warning(this, "Unknown item type on MSMQ event queue: {0}", wki.Item.GetType().FullName);
                    }
                }
                _eventQueue.BeginReceive();
            }
        }
    }
}
