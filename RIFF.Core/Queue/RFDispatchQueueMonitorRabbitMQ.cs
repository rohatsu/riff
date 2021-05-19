// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    /// <summary>
    /// Queue monitor implementation that sends work items to RabbitMQ for them to be picked up by worker threads
    /// </summary>
    internal class RFDispatchQueueMonitorRabbitMQ : RFDispatchQueueMonitorBase
    {
        protected static volatile int _counter = 1;
        protected RFWorkDoneMonitorRabbitMQ _eventMonitorThread;

        protected string _eventQueue = "riff:event";
        protected string _workerQueue = "riff:worker";
        protected string _eventExchange = "riff:event";
        protected string _workerExchange = "riff:worker";

        protected List<RFWorkerThreadRabbitMQ> _workerThreads;
        protected int _workerThreadsCount;

        protected IConnection _connection;
        protected IModel _channel;
        protected RFFormatterRabbitMQ _formatter;

        public RFDispatchQueueMonitorRabbitMQ(RFComponentContext context, IRFInstructionSink instructionSink, IRFEventSink eventSink, IRFDispatchQueue dispatchQueue)
        : base(context, instructionSink, eventSink, dispatchQueue)
        {
            var factory = new ConnectionFactory() { Uri = new Uri(RFSettings.GetAppSetting("RabbitMQUri")) };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _formatter = new RFFormatterRabbitMQ();

            _workerThreadsCount = RFSettings.GetAppSetting("RabbitMQWorkerThreads", 4);
            _channel.ExchangeDeclare(_workerExchange, "fanout", true, false);
            _channel.QueueDeclare(_workerQueue, true, false, false);
            _channel.QueueBind(_workerQueue, _workerExchange, string.Empty);

            // our private event queue
            var process = System.Diagnostics.Process.GetCurrentProcess();
            _eventQueue = string.Join("_", _eventQueue, Environment.MachineName, process.Id, System.IO.Path.GetFileNameWithoutExtension(process.MainModule.FileName)).Replace('.', '_').ToLower();

            _channel.ExchangeDeclare(_eventExchange, "fanout", true, false);
            _channel.QueueDeclare(_eventQueue, true, true, true);
            _channel.QueueBind(_eventQueue, _eventExchange, string.Empty);

            Log.Debug(this, "Using RabbitMQ Queues");
        }

        protected override void PrepareToRun()
        {
            // event monitoring
            _eventMonitorThread = new RFWorkDoneMonitorRabbitMQ(_channel, _eventQueue, _context, _instructionSink, _eventSink); // process locally any events received
            _eventMonitorThread.StartThread();

            // create worker threads
            _workerThreads = new List<RFWorkerThreadRabbitMQ>();
            var rabbitSink = new RFEventSinkRabbitMQ(_channel, _eventExchange); // has remote proxies for sending back events

            for (int i = 0; i < _workerThreadsCount; i++)
            {
                var t = new RFWorkerThreadRabbitMQ(_context, _channel, _workerQueue, rabbitSink);
                t.StartThread();
                _workerThreads.Add(t);
            }
        }

        protected override void PrepareToStop()
        {
            if (_eventMonitorThread != null)
            {
                _eventMonitorThread.Shutdown();
                _eventMonitorThread = null;
            }
            if (_workerThreads != null)
            {
                foreach (var t in _workerThreads)
                {
                    t.Shutdown();
                }
                _workerThreads = null;
            }
            if (_eventQueue != null)
            {
                try
                {
                    RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorRabbitMQ), "Deleting RabbitMQ queue {0}", _eventQueue);
                    _channel.QueueDelete(_eventQueue);
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Error deleting RabbitMQ queue {0}", _eventQueue);
                }
                _eventQueue = null;
            }
            if (_channel != null)
                _channel.Close();
            if (_connection != null)
                _connection.Close();
        }

        protected override void ProcessQueueItem(RFWorkQueueItem item)
        {
            if (_channel != null)
            {
                IBasicProperties props = _channel.CreateBasicProperties();
                props.ContentType = "text/plain";
                props.DeliveryMode = 2;
                _channel.BasicPublish(_workerExchange, string.Empty, props, _formatter.Write(item));
            }
        }
    }
}
