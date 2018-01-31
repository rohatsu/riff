using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public abstract class RFGraphTaskDefinition : RFTaskDefinition
    {
        public override string Description => GraphProcess.Description;

        public override string GraphName => GraphProcess.GraphName;

        [DataMember]
        public RFGraphProcessDefinition GraphProcess { get; set; }

        public override string ProcessName => GraphProcess.Name;

        public abstract void AddToEngine(RFEngineDefinition engine);
    }

    [DataContract]
    public class RFScheduledGraphTaskDefinition : RFGraphTaskDefinition, IRFScheduledTaskDefinition
    {
        [DataMember]
        public Func<RFSchedulerRange> RangeFunc { get; set; }

        [DataMember]
        public Func<List<RFSchedulerSchedule>> SchedulesFunc { get; set; }

        public override RFSchedulerConfig SchedulerConfig(IRFProcessingContext context)
        {
            return new RFSchedulerConfig
            {
                IsEnabled = true,
                Range = RangeFunc(),
                Schedules = SchedulesFunc()
            };
        }

        [DataMember]
        public RFManualTriggerKey TriggerKey { get; set; }

        public override void AddToEngine(RFEngineDefinition engine)
        {
            var scheduler = engine.AddProcess(
                processName: String.Format("Task {0} Scheduler", TaskName),
                description: string.Format("Schedules task {0}", TaskName),
                processor: () => new RFSchedulerProcessor(new RFSchedulerConfig
                {
                    Range = RangeFunc(),
                    Schedules = SchedulesFunc(),
                    TriggerKey = TriggerKey,
                    GraphInstance = (i) => TriggerKey.GraphInstance.WithDate(i.IntervalEnd.Date)
                }));

            engine.AddIntervalTrigger(scheduler);
        }
    }
}
