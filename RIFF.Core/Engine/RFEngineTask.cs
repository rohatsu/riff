using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFChainedEngineTaskDefinition : RFEngineTaskDefinition
    {
        public override string Trigger { get { return string.Format("After {0}", TriggerProcess.Name); } }

        [DataMember]
        public RFEngineProcessDefinition TriggerProcess { get; set; }

        public override void AddToEngine(RFEngineDefinition engine)
        {
            engine.AddTrigger(new RFSingleCommandTrigger(e => ((e is RFProcessingFinishedEvent) && (e as RFProcessingFinishedEvent).GetFinishedProcessName() == TriggerProcess.Name)
                ? new RFParamProcessInstruction(TaskProcess.Name, null) : null));
        }
    }

    [DataContract]
    public abstract class RFEngineTaskDefinition : RFTaskDefinition
    {
        public override string Description
        {
            get
            {
                return TaskProcess.Description;
            }
        }

        public override string ProcessName
        {
            get
            {
                return TaskProcess.Name;
            }
        }

        [DataMember]
        public RFEngineProcessDefinition TaskProcess { get; set; }

        public abstract void AddToEngine(RFEngineDefinition engine);
    }

    [DataContract]
    public class RFScheduledEngineTaskDefinition : RFEngineTaskDefinition, IRFScheduledTaskDefinition
    {
        [DataMember]
        public RFSchedulerRange Range { get; set; }

        public override string SchedulerRange
        {
            get
            {
                return Range?.ToString() ?? String.Empty;
            }
        }

        public override string SchedulerSchedule
        {
            get
            {
                return Schedules != null ? String.Join(", ", Schedules.Select(s => s.ToString())) : String.Empty;
            }
        }

        [DataMember]
        public List<RFSchedulerSchedule> Schedules { get; set; }

        public override void AddToEngine(RFEngineDefinition engine)
        {
            var triggerName = RFEnum.FromString(TaskName);
            var triggerKey = RFSchedulerTriggerKey.Create(engine.KeyDomain, triggerName);

            var scheduler = engine.AddProcess(
                processName: String.Format("Task {0} Scheduler", TaskName),
                description: string.Format("Schedules task {0}", TaskName),
                processor: () => new RFSchedulerProcessor(new RFSchedulerConfig
                {
                    Range = Range,
                    Schedules = Schedules,
                    //IntervalKey = engine.IntervalDocumentKey(),
                    TriggerKey = triggerKey
                }));

            engine.AddIntervalTrigger(scheduler);
            engine.AddCatalogUpdateTrigger<RFSchedulerTriggerKey>(k => k.TriggerName.Equals(triggerName), TaskProcess);
        }
    }

    [DataContract]
    public class RFTriggeredEngineTaskDefinition : RFEngineTaskDefinition
    {
        public override string Trigger { get { return TriggerKey != null ? string.Format("Key: {0}", TriggerKey.FriendlyString()) : "Manual"; } }

        [DataMember]
        public RFCatalogKey TriggerKey { get; set; }

        public override void AddToEngine(RFEngineDefinition engine)
        {
            engine.AddCatalogUpdateTrigger<RFCatalogKey>(k => k.MatchesRoot(TriggerKey), TaskProcess);
        }
    }
}
