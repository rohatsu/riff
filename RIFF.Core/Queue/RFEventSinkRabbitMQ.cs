// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RIFF.Core
{
    /// <summary>
    /// Sends all events from a worker to RabbitMQ for them to be picked up by queue monitor
    /// </summary>
    internal class RFEventSinkRabbitMQ : IRFEventSink
    {
        private readonly IModel _channel;
        private readonly string _eventExchange;
        private readonly RFFormatterRabbitMQ _formatter;

        public RFEventSinkRabbitMQ(IModel channel, string eventExchange)
        {
            _channel = channel;
            _eventExchange = eventExchange;
            _formatter = new RFFormatterRabbitMQ();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
        public void RaiseEvent(object raisedBy, RFEvent e, string processingKey)
        {
            try
            {
                lock (_eventExchange) // Send is not thread-safe, would need to use thread-local instances
                {
                    IBasicProperties props = _channel.CreateBasicProperties();
                    props.ContentType = "text/plain";
                    props.DeliveryMode = 2;
                    _channel.BasicPublish(_eventExchange, string.Empty, props, _formatter.Write(new RFWorkQueueItem { Item = e, ProcessingKey = processingKey }));
                }
                RFStatic.Log.Debug(typeof(RFEventSinkRabbitMQ), "Sent event {0} to RabbitMQ", e);
            }
            catch (RabbitMQClientException)
            {
                if (!RFStatic.IsShutdown)
                {
                    throw;
                }
            }
        }
    }
}
