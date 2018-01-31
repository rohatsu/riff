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

        public override void AddToEngine(RFEngineDefinition engine)
        {
            var triggerName = RFEnum.FromString(TaskName);
            var triggerKey = RFSchedulerTriggerKey.Create(engine.KeyDomain, triggerName);

            var scheduler = engine.AddProcess(
                processName: String.Format("Task {0} Scheduler", TaskName),
                description: string.Format("Schedules task {0}", TaskName),
                processor: () => new RFSchedulerProcessor(new RFSchedulerConfig
                {
                    Range = RangeFunc(),
                    Schedules = SchedulesFunc(),
                    TriggerKey = triggerKey
                }));

            engine.AddIntervalTrigger(scheduler);
            engine.AddCatalogUpdateTrigger<RFSchedulerTriggerKey>(k => k.TriggerName.Equals(triggerName), TaskProcess);
        }
    }

    [DataContract]
    public class RFSchedulerTaskDefinition : RFEngineTaskDefinition, IRFScheduledTaskDefinition
    {
        public static readonly string CONFIG_SECTION = "Scheduled Tasks";

        [DataMember]
        public Func<IRFProcessingContext, RFSchedulerRange> RangeFunc { get; set; }

        [DataMember]
        public Func<IRFProcessingContext, List<RFSchedulerSchedule>> SchedulesFunc { get; set; }

        public override RFSchedulerConfig SchedulerConfig(IRFProcessingContext context)
        {
            return new RFSchedulerConfig
            {
                Range = RangeFunc(context),
                Schedules = SchedulesFunc(context),
                IsEnabled = context.UserConfig.GetBool(CONFIG_SECTION, TaskName, true, true, "Is Enabled")
            };
        }

        public override void AddToEngine(RFEngineDefinition engine)
        {
            var triggerName = RFEnum.FromString(TaskName);
            var triggerKey = RFSchedulerTriggerKey.Create(engine.KeyDomain, triggerName);
            engine.AddCatalogUpdateTrigger<RFSchedulerTriggerKey>(k => k.TriggerName.Equals(triggerName), TaskProcess);

            engine.Schedules.Add(c => new RFSchedulerConfig
            {
                Range = RangeFunc(c),
                Schedules = SchedulesFunc(c),
                TriggerKey = triggerKey,
                IsEnabled = c.UserConfig.GetBool(CONFIG_SECTION, TaskName, true, true, "Is Enabled")
            });
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
            if(TriggerKey != null)
            {
                engine.AddCatalogUpdateTrigger<RFCatalogKey>(k => k.MatchesRoot(TriggerKey), TaskProcess);
            }
        }
    }
}
