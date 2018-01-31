using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace RIFF.Core
{

    public class RFSchedulerService : RFBackgroundService
    {
        public static readonly string SERVICE_NAME = "Scheduler Service";

        public const string RELOAD_COMMAND = "reload";

        public const string LIST_COMMAND = "list";

        protected List<Func<IRFProcessingContext, RFSchedulerConfig>> _configFuncs;

        protected List<RFSchedulerConfig> _configs;

        protected DateTime _lastTrigger;

        protected IRFProcessingContext _context;

        private object _sync = new object();

        public RFSchedulerService(IRFProcessingContext context, List<Func<IRFProcessingContext, RFSchedulerConfig>> configFuncs)
        {
            _context = context;
            _configFuncs = configFuncs;
            _lastTrigger = DateTime.Now;
            Reload();
        }

        protected void Reload()
        {
            lock(_sync)
            {
                _configs = _configFuncs.Select(c => c(_context)).ToList();
            }
        }

        public override List<RFInstruction> CustomCommand(string command, string param)
        {
            switch(command)
            {
                case RELOAD_COMMAND:
                    Reload();
                    break;
                case LIST_COMMAND:
                    List();
                    break;
            }
            return null;
        }

        protected void List()
        {
            lock(_sync)
            {
                int n = 1;
                foreach(var config in _configs)
                {
                    Console.WriteLine($"Schedule #{n++}");
                    Console.WriteLine($"  Trigger Key = {config.TriggerKey.FriendlyString()}");
                    Console.WriteLine($"  Is Enabled = {config.IsEnabled}");
                    Console.WriteLine($"  Schedules = {String.Join(",", config.Schedules)}");
                    Console.WriteLine($"  Range = {config.Range}");
                }
            }
        }

        public override void Start()
        {
            _context.SystemLog.Debug(this, "Scheduler Service starting");
            while(!IsExiting())
            {
                Thread.Sleep(1000 - DateTime.Now.Millisecond);

                var now = DateTime.Now;
                var interval = new RFInterval(_lastTrigger, now);
                lock(_sync)
                {
                    foreach(var config in _configs)
                    {
                        if(config.ShouldTrigger(interval))
                        {
                            var key = config.TriggerKey;
                            if(config.GraphInstance != null)
                            {
                                key = key.CreateForInstance(config.GraphInstance(interval));
                                _context.SaveEntry(RFDocument.Create(key, new RFGraphProcessorTrigger { TriggerStatus = true, TriggerTime = interval.IntervalEnd }));
                            }
                            else
                            {
                                _context.SaveEntry(RFDocument.Create(key, new RFScheduleTrigger { LastTriggerTime = interval.IntervalEnd }));
                            }
                        }
                    }
                    _lastTrigger = now;
                }
            }
        }

        public override void Stop()
        {
            _context.SystemLog.Debug(this, "Scheduler Service stopping");
        }
    }

}