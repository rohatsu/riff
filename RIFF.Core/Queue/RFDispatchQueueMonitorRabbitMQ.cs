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

        protected string _eventQueue = "event";
        protected string _workerQueue = "worker";
        protected string _eventExchange = "event";
        protected string _workerExchange = "worker";

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

            _channel.ExchangeDeclare(_eventExchange, "fanout", true, false);
            _channel.QueueDeclare(_eventQueue, true, true, false);
            _channel.QueueBind(_eventQueue, _eventExchange, null);

            _channel.ExchangeDeclare(_workerExchange, "fanout", true, false);
            _channel.QueueDeclare(_workerQueue, true, false, false);
            _channel.QueueBind(_workerQueue, _workerExchange, null);

            Log.Debug(this, "Using RabbitMQ Queues");
        }

        /*protected static MessageQueue GetOrCreateQueue(string queueName, string environment)
        {
            var workQueueName = Environment.MachineName + @"\PRIVATE$\RIFF_" + queueName + "_" + environment;
            if (MessageQueue.Exists(workQueueName))
            {
                RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorMSMQ), "Opening MSMQ queue {0}", workQueueName);
                var queue = new MessageQueue(workQueueName);
                queue.Formatter = new RFFormatterMSMQ();
                queue.Purge();
                return queue;
            }
            else
            {
                RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorMSMQ), "Creating MSMQ queue {0}", workQueueName);
                var queue = MessageQueue.Create(workQueueName);
                queue.Formatter = new RFFormatterMSMQ();
                return queue;
            }
        }*/

        protected override void PrepareToRun()
        {
            _eventMonitorThread = new RFWorkDoneMonitorRabbitMQ(_channel, _eventQueue, _context, _instructionSink, _eventSink);
            _eventMonitorThread.StartThread();

            // create worker threads
            // TODO: move starting these out of here (and out of process) one day
            _workerThreads = new List<RFWorkerThreadRabbitMQ>();
            var msmqSink = new RFEventSinkRabbitMQ(_channel, _eventExchange); // has remote proxies for sending back events

            for (int i = 0; i < _workerThreadsCount; i++)
            {
                var t = new RFWorkerThreadRabbitMQ(_context, _channel, _workerQueue, msmqSink);
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
/*            if (_eventQueue != null)
            {
                try
                {
                    RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorRabbitMQ), "Deleting RabbitMQ queue {0}", _eventQueue.Path);
                    MessageQueue.Delete(_eventQueue.Path);
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Error deleting MSMQ queue {0}", _eventQueue.Path);
                }
                _eventQueue = null;
            }
            if (_workerQueue != null)
            {
                try
                {
                    RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorRabbitMQ), "Deleting RabbitMQ queue {0}", _workerQueue.Path);
                    MessageQueue.Delete(_workerQueue.Path);
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Error deleting MSMQ queue {0}", _workerQueue.Path);
                }
                _workerQueue = null;
            }*/
            _channel.Close();
            _connection.Close();
        }

        protected override void ProcessQueueItem(RFWorkQueueItem item)
        {
            IBasicProperties props = _channel.CreateBasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = 2;
            _channel.BasicPublish(_workerExchange, null, props, _formatter.Write(item));
        }
    }
}
