// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RIFF.Core
{
    /// <summary>
    /// Thread which waits on event queue where workers send their results and events, and passes
    /// them to its sinks
    /// </summary>
    internal class RFWorkDoneMonitorRabbitMQ : RFActiveComponent
    {
        private readonly IModel _channel;
        private readonly string _eventQueue;
        private readonly IRFEventSink _eventSink;
        private readonly IRFInstructionSink _instructionSink;
        private readonly RFFormatterRabbitMQ _formatter;
        private readonly EventingBasicConsumer _consumer;
        private string _consumerTag;

        public RFWorkDoneMonitorRabbitMQ(IModel channel, string eventQueue, RFComponentContext context, IRFInstructionSink instructionSink, IRFEventSink eventSink) : base(context)
        {
            _instructionSink = instructionSink;
            _eventSink = eventSink;
            _channel = channel;
            _eventQueue = eventQueue;
            _formatter = new RFFormatterRabbitMQ();
            _consumer = new EventingBasicConsumer(channel);
            _consumer.Received += (ch, ea) =>
            {
                _eventQueue_ReceiveCompleted(_formatter.Read(ea.Body));
            };
        }

        protected override void Run()
        {
            _consumerTag = _channel.BasicConsume(_eventQueue, true, _consumer);
            _context.CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        protected override void Stop()
        {
            _channel.BasicCancel(_consumerTag);
            _thread.Interrupt();
        }

        private void _eventQueue_ReceiveCompleted(object body)
        {
            if (!IsExiting())
            {
                if (body != null && body is RFWorkQueueItem)
                {
                    var wki = body as RFWorkQueueItem;
                    if (wki.Item is RFEvent)
                    {
                        var evt = wki.Item as RFEvent;
                        Log.Debug(this, "Received event {0} from RabbitMQ", evt);
                        _eventSink.RaiseEvent(this, evt, wki.ProcessingKey);
                    }
                    else if (wki.Item is RFInstruction)
                    {
                        var ins = wki.Item as RFInstruction;
                        Log.Debug(this, "Received instruction {0} from RabbitMQ", ins);
                        _instructionSink.QueueInstruction(this, ins, wki.ProcessingKey);
                    }
                    else
                    {
                        Log.Warning(this, "Unknown item type on RabbitMQ event queue: {0}", wki.Item.GetType().FullName);
                    }
                }
            }
        }
    }
}
