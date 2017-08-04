// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;
using System.Threading;

namespace RIFF.Core
{
    [DataContract]
    public class RFInterval
    {
        [DataMember]
        public DateTime IntervalEnd { get; private set; }

        [DataMember]
        public DateTime IntervalStart { get; private set; }

        public RFInterval(DateTime intervalStart, DateTime intervalEnd)
        {
            IntervalStart = intervalStart;
            IntervalEnd = intervalEnd;
        }

        public static RFInterval Now()
        {
            return new RFInterval(DateTime.Now, DateTime.Now);
        }

        public bool Includes(DateTime dt)
        {
            return (dt > IntervalStart && dt <= IntervalEnd);
        }
    }

    [DataContract]
    public class RFIntervalEvent : RFEvent
    {
        public RFInterval Interval { get; set; }

        public RFIntervalEvent(RFInterval interval)
        {
            Interval = interval;
        }
    }

    internal class RFIntervalComponent : RFActiveComponent
    {
        private RFSchedulerRange _downtime;
        private IRFEventSink _eventManager;
        private int _intervalLength = 60000;
        private volatile bool _isSuspended;

        public RFIntervalComponent(RFComponentContext context, IRFEventSink eventManager)
                    : base(context)
        {
            _eventManager = eventManager;
            _intervalLength = context.SystemConfig.IntervalLength;
            if (_intervalLength == 0)
            {
                _intervalLength = 60000;
            }
            _downtime = context.SystemConfig.Downtime;
        }

        public void Resume()
        {
            _isSuspended = false;
        }

        public void Suspend()
        {
            _isSuspended = true;
        }

        protected override void Run()
        {
            var prevNow = DateTime.Now;
            if (_intervalLength > 1000)
            {
                // if interval is longer than a second, align ticks to next full minute
                var secondsToAlign = 60 - prevNow.TimeOfDay.Seconds;
                for (int n = 0; n < secondsToAlign && !IsExiting(); ++n)
                {
                    Thread.Sleep(1000);
                }
            }
            while (!IsExiting())
            {
                if (!_isSuspended)
                {
                    var now = DateTime.Now;
                    var interval = new RFInterval(prevNow, now);
                    if (!IsDowntime(interval))
                    {
                        _eventManager.RaiseEvent(this, new RFIntervalEvent(interval), null);
                        prevNow = now;
                    }
                }

                // sleep one second max at a time
                for (int n = 0; n < _intervalLength / 1000 && !IsExiting(); ++n)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected override void Stop()
        {
            _thread.Interrupt();
        }

        private bool IsDowntime(RFInterval interval)
        {
            return _downtime?.InRange(interval) ?? false;
        }
    }
}
