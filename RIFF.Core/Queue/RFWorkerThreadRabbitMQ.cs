// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Data.Common;

namespace RIFF.Core
{
    /// <summary>
    /// Thread that processes instructions received via RabbitMQ
    /// </summary>
    internal class RFWorkerThreadRabbitMQ : RFActiveComponent, IDisposable
    {
        protected IRFEventSink _eventSink;
        protected IModel _channel;
        protected string _workerQueue;
        private readonly EventingBasicConsumer _consumer;
        private readonly RFFormatterRabbitMQ _formatter;
        private string _consumerTag;

        public RFWorkerThreadRabbitMQ(RFComponentContext context, IModel channel, string workerQueueName, IRFEventSink eventSink)
        : base(context)
        {
            _eventSink = eventSink;
            _channel = channel;
            _workerQueue = workerQueueName;
            _formatter = new RFFormatterRabbitMQ();
            _consumer = new EventingBasicConsumer(channel);
            _consumer.Received += (ch, ea) =>
            {
                _queue_ReceiveCompleted(_formatter.Read(ea.Body));
            };
        }

        public void Dispose()
        {
        }

        protected override void Run()
        {
            _consumerTag = _channel.BasicConsume(_workerQueue, true, _consumer);
            _context.CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        protected override void Stop()
        {
            _channel.BasicCancel(_consumerTag);
            _channel.Close();
            _thread.Interrupt();
        }

        private void _queue_ReceiveCompleted(object body)
        {
            try
            {
                var i = body as RFWorkQueueItem;
                if (i != null && !IsExiting())
                {
                    Log.Debug(this, "Received msg {0} from RabbitMQ", i.Item);
                    ProcessInstructionThread(i);
                }
            }
            catch (RabbitMQClientException mqe)
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
                //_workerQueue.BeginReceive();
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
                    // send result and all buffered events/instructions to external event manager
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
