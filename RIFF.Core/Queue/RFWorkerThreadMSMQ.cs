// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Data.Common;
using System.Messaging;
using System.Transactions;

namespace RIFF.Core
{
    /// <summary>
    /// Thread that processes instructions received via MSMQ
    /// </summary>
    internal class RFWorkerThreadMSMQ : RFActiveComponent, IDisposable
    {
        protected IRFEventSink _eventSink;
        protected MessageQueue _workerQueue;

        public RFWorkerThreadMSMQ(RFComponentContext context, string workerQueueName, IRFEventSink eventSink)
        : base(context)
        {
            _eventSink = eventSink;
            _workerQueue = new MessageQueue(workerQueueName);
            _workerQueue.Formatter = new RFFormatterMSMQ();
            _workerQueue.ReceiveCompleted += _queue_ReceiveCompleted;
        }

        public void Dispose()
        {
            ((IDisposable)_workerQueue).Dispose();
        }

        protected override void Run()
        {
            _workerQueue.BeginReceive();
            _context.CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        protected override void Stop()
        {
            _thread.Interrupt();
        }

        private void _queue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
        {
            try
            {
                var i = e.Message?.Body as RFWorkQueueItem;
                if (i != null && !IsExiting())
                {
                    Log.Debug(this, "Received msg {0} from MSMQ", i.Item);
                    ProcessInstructionThread(i);
                }
            }
            catch (MessageQueueException mqe)
            {
                if (!IsExiting())
                {
                    Log.Warning(this, "Shutting down worker thread: {0}", mqe.Message);
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                if (!IsExiting())
                {
                    Log.Exception(this, ex, "Exception in worker thread!");
                }
            }
            if (!IsExiting())
            {
                _workerQueue.BeginReceive();
            }
            else
            {
                Log.Debug(this, "Exiting....");
            }
        }

        private void ProcessInstructionThread(RFWorkQueueItem i)
        {
            try
            {
                // To make transaction work we'd have to use a single SQL connection to do all reads and writes
                /* using (var ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(30)
                }))*/
                {
                    Log.Debug(this, "Started thread to process instruction {0}", i.Item as RFProcessInstruction);
                    var sink = new RFBufferingSink(); // create thread-local manager to buffer events and instructions, then send them back in bulk
                    RFProcessingResult result = null;
                    try
                    {
                        result = _context.Engine.Process(i.Item as RFInstruction, _context.GetProcessingContext(i.ProcessingKey, sink, sink, null));
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(this, ex, "Exception Thread processing queue item ", i);
                        result = RFProcessingResult.Error(new string[] { ex.Message }, ex is DbException || ex is TimeoutException);
                    }
                    // send result and all buffered events/instructions to external event manager (since
                    // we can't guarantee delivery order with MSMQ)
                    _eventSink.RaiseEvent(this, new RFProcessingFinishedEvent(i, result, sink.GetItems()), i.ProcessingKey);
                    //ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Exception Thread processing queue item ", i);
            }
        }
    }
}
