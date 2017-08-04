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
        public RFSchedulerRange Range { get; set; }

        public override string SchedulerRange => Range?.ToString() ?? String.Empty;

        public override string SchedulerSchedule => Schedules != null ? String.Join(", ", Schedules.Select(s => s.ToString())) : String.Empty;

        [DataMember]
        public List<RFSchedulerSchedule> Schedules { get; set; }

        [DataMember]
        public RFManualTriggerKey TriggerKey { get; set; }

        /*public override string Trigger
        {
            get
            {
                return TriggerKey != null ? TriggerKey.FriendlyString() : String.Empty;
            }
        }*/

        public override void AddToEngine(RFEngineDefinition engine)
        {
            var scheduler = engine.AddProcess(
                processName: String.Format("Task {0} Scheduler", TaskName),
                description: string.Format("Schedules task {0}", TaskName),
                processor: () => new RFSchedulerProcessor(new RFSchedulerConfig
                {
                    Range = Range,
                    Schedules = Schedules,
                    //IntervalKey = engine.IntervalDocumentKey(),
                    TriggerKey = TriggerKey,
                    GraphInstance = (i) => TriggerKey.GraphInstance.WithDate(i.IntervalEnd.Date)
                }));

            engine.AddIntervalTrigger(scheduler);
        }
    }
}
