// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RIFF.Core
{
    public class SmtpAsyncAppender : SmtpAppender
    {
        public string Environment { get; set; }
        public int SendInterval { get; set; } = 10;

        protected virtual void PrepareSubject(LoggingEvent[] events)
        {
            var errors = events.Where(e => e.Level >= Level.Error);
            if (errors.Any())
            {
                var evt = errors.First();
                string msg = evt.ExceptionObject == null ? evt.RenderedMessage : evt.ExceptionObject.Message;

                Subject = RFStringHelpers.Limit(string.Format("[{0}] {1}", evt.Level, msg), 125);

                if (errors.Count() > 1)
                {
                    Subject += $" (+{errors.Count() - 1})";
                }
            }
            else
            {
                Subject = "Errors";
            }

            if (Environment != null)
            {
                Subject = Environment + ": " + Subject;
            }
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            foreach (var evt in events)
            {
                _buffer.Add(evt);
            }
        }

        protected override void SendEmail(string messageBody)
        {
            Task.Run(() => base.SendEmail(messageBody));
        }

        public SmtpAsyncAppender()
        {
            _senderThread = new Thread(SenderThread);
            _senderThread.Start();
        }

        public void SenderThread()
        {
            while (true)
            {
                var isCancelled = RFStatic.CancellationToken.WaitHandle.WaitOne(SendInterval * 1000);
                if(isCancelled || RFStatic.IsShutdown)
                {
                    return;
                }
                var toSend = new List<LoggingEvent>();
                while (_buffer.TryTake(out var evt))
                {
                    toSend.Add(evt);
                }
                if (toSend.Any())
                {
                    try
                    {
                        PrepareSubject(toSend.ToArray());
                        base.SendBuffer(toSend.ToArray());
                    } catch(Exception)
                    {

                    }
                }
            }
        }

        private System.Threading.Thread _senderThread;
        private ConcurrentBag<LoggingEvent> _buffer = new ConcurrentBag<LoggingEvent>();
    }
}
