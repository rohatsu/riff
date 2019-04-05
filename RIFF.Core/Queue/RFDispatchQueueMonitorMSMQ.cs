// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

#if !NETSTANDARD2_0
using System.Messaging;

namespace RIFF.Core
{
    /// <summary>
    /// Queue monitor implementation that sends work items to MSMQ for them to be picked up by worker threads
    /// </summary>
    internal class RFDispatchQueueMonitorMSMQ : RFDispatchQueueMonitorBase
    {
        protected static volatile int _counter = 1;
        protected RFWorkDoneMonitorMSMQ _eventMonitorThread;
        protected MessageQueue _eventQueue;
        protected MessageQueue _workerQueue;
        protected List<RFWorkerThreadMSMQ> _workerThreads;
        protected int _workerThreadsCount;

        public RFDispatchQueueMonitorMSMQ(RFComponentContext context, IRFInstructionSink instructionSink, IRFEventSink eventSink, IRFDispatchQueue dispatchQueue)
        : base(context, instructionSink, eventSink, dispatchQueue)
        {
            var localSuffix = RIFF.Core.RFCore.sShortVersion;
            _workerThreadsCount = RFSettings.GetAppSetting("MSMQWorkerThreads", 4);
            _workerQueue = GetOrCreateQueue("WorkerQueue_" + localSuffix, _context.SystemConfig.Environment);
            _eventQueue = GetOrCreateQueue("EventQueue_" + localSuffix, _context.SystemConfig.Environment);
            _workerQueue.DefaultPropertiesToSend.ResponseQueue = _eventQueue;
            Log.Debug(this, "Using MSMQ Queues");
        }

        protected static MessageQueue GetOrCreateQueue(string queueName, string environment)
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
        }

        protected override void PrepareToRun()
        {
            _eventMonitorThread = new RFWorkDoneMonitorMSMQ(_eventQueue, _context, _instructionSink, _eventSink);
            _eventMonitorThread.StartThread();

            // create worker threads
            // TODO: move starting these out of here (and out of process) one day
            _workerThreads = new List<RFWorkerThreadMSMQ>();
            var msmqSink = new RFEventSinkMSMQ(_eventQueue); // has remote proxies for sending back events

            for (int i = 0; i < _workerThreadsCount; i++)
            {
                var t = new RFWorkerThreadMSMQ(_context, "FormatName:" + _workerQueue.FormatName, msmqSink);
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
                    RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorMSMQ), "Deleting MSMQ queue {0}", _eventQueue.Path);
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
                    RFStatic.Log.Debug(typeof(RFDispatchQueueMonitorMSMQ), "Deleting MSMQ queue {0}", _workerQueue.Path);
                    MessageQueue.Delete(_workerQueue.Path);
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Error deleting MSMQ queue {0}", _workerQueue.Path);
                }
                _workerQueue = null;
            }
        }

        protected override void ProcessQueueItem(RFWorkQueueItem item)
        {
            _workerQueue.Send(item);
        }
    }
}
#endif