// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFEngineDefinition
    {
        /// <summary>
        /// Optionally create a console that can be used to process batch commands via RIFF.Console
        /// </summary>
        [DataMember]
        public IRFEngineConsole Console { get; set; }

        [DataMember]
        public string EngineName { get; private set; }

        [DataMember]
        public Dictionary<string, RFGraphDefinition> Graphs { get; private set; }

        [DataMember]
        public int IntervalSeconds { get; private set; }

        [DataMember]
        public RFKeyDomain KeyDomain { get; private set; }

        [DataMember]
        public Dictionary<string, string> Keys { get; private set; }

        [IgnoreDataMember]
        public KeyValuePair<string, string> LicenseTokens { get; set; }

        [DataMember]
        public TimeSpan MaxRuntime { get; private set; }

        [DataMember]
        public int ProcessCounter { get; private set; }

        [DataMember]
        public Dictionary<string, RFEngineProcessDefinition> Processes { get; private set; }

        [DataMember]
        public List<RFEngineTaskDefinition> Tasks { get; private set; }

        [DataMember]
        public List<IRFEngineTrigger> Triggers { get; private set; }

        [DataMember]
        public Dictionary<string, Func<IRFProcessingContext, IRFBackgroundService>> Services { get; private set; }

        protected RFEngineDefinition(string engineName, RFKeyDomain keyDomain, int intervalSeconds, TimeSpan maxRuntime)
        {
            KeyDomain = keyDomain;
            EngineName = engineName;
            Processes = new Dictionary<string, RFEngineProcessDefinition>();
            Triggers = new List<IRFEngineTrigger>();
            Services = new Dictionary<string, Func<IRFProcessingContext, IRFBackgroundService>>();
            Graphs = new Dictionary<string, RFGraphDefinition>();
            Keys = new Dictionary<string, string>();
            Tasks = new List<RFEngineTaskDefinition>();
            IntervalSeconds = intervalSeconds;
            MaxRuntime = maxRuntime;
            ProcessCounter = 1;
        }

        /// <summary>
        /// Create a new engine.
        /// </summary>
        /// <param name="graphName">Unique user-friendly name.</param>
        /// <param name="keyDomain">Key Domain defining root for all keys.</param>
        /// <returns>Use methods on returned obejct to add engine components.</returns>
        public static RFEngineDefinition Create(string graphName, RFKeyDomain keyDomain, int intervalSeconds = 60, TimeSpan? maxRuntime = null)
        {
            return new RFEngineDefinition(graphName, keyDomain, intervalSeconds, maxRuntime ?? TimeSpan.FromMinutes(20));
        }

        /// <summary>
        /// Configure a trigger for a catalog update to trigger a process (root-based matching)
        /// </summary>
        /// <typeparam name="K">Subclass of RFCatalogKey</typeparam>
        public void AddCatalogUpdateTrigger<K>(Func<K, bool> evaluator, RFEngineProcessDefinition processConfig) where K : RFCatalogKey
        {
            Triggers.Add(new RFCatalogUpdateTrigger(e => (e is K) && evaluator(e as K), processConfig));
        }

        /// <summary>
        /// Configures process to automatically run after another process
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="triggeringProcess"></param>
        /// <param name="triggeredProcess"></param>
        /// <returns></returns>
        public RFEngineTaskDefinition AddChainedTask(string taskName, RFEngineProcessDefinition triggeringProcess, RFEngineProcessDefinition triggeredProcess, bool isSystem)
        {
            var task = new RFChainedEngineTaskDefinition
            {
                TaskName = taskName,
                TaskProcess = triggeredProcess,
                TriggerProcess = triggeringProcess,
                IsSystem = isSystem
            };
            Tasks.Add(task);
            task.AddToEngine(this);
            return task;
        }

        /// <summary>
        /// Configure a trigger to run a process on each interval (for schedulers).
        /// </summary>
        /// <param name="processConfig"></param>
        public void AddIntervalTrigger(RFEngineProcessDefinition processConfig)
        {
            AddTrigger(new RFSingleCommandTrigger(e => e is RFIntervalEvent ie ?
                new RFIntervalInstruction(processConfig.Name, ie) : null));
            /*AddCatalogUpdateTrigger<RFCatalogKey>(
                i => i.Equals(IntervalDocumentKey()),
                processConfig);*/
        }

        /// <summary>
        /// Define a new process inside the engine.
        /// </summary>
        /// <param name="processName">Unique user-friendly name.</param>
        /// <param name="processor">
        /// Function that creates the processor (may pass static config etc.)
        /// </param>
        /// <param name="instanceParams">
        /// Function that derives instance parameters based on processing instruction.
        /// </param>
        /// <returns></returns>
        public RFEngineProcessDefinition AddProcess(string processName, string description, Func<IRFEngineProcessor> processor, Func<RFInstruction, RFEngineProcessorParam> instanceParams = null)
        {
            if (Processes.ContainsKey(processName))
            {
                throw new Exception(String.Format("Already registered process {0}", processName));
            }
            processor(); // test instantiation
            var newProcess = new RFEngineProcessDefinition
            {
                Name = processName,
                InstanceParams = instanceParams ?? (i => i.ExtractParam()),
                Processor = processor,
                Description = description
            };
            Processes.Add(processName, newProcess);
            return newProcess;
        }

        /// <summary>
        /// Define a new process inside the engine.
        /// </summary>
        /// <param name="processName">Unique user-friendly name.</param>
        /// <param name="processor">
        /// Function that creates the processor (may pass static config etc.)
        /// </param>
        /// <param name="instanceParams">
        /// Function that derives instance parameters based on processing instruction.
        /// </param>
        /// <returns></returns>
        public RFEngineProcessDefinition<P> AddProcess<P>(string processName, string description, Func<RFEngineProcessor<P>> processor, Func<RFInstruction, P> instanceParams = null) where P : RFEngineProcessorParam
        {
            if (Processes.ContainsKey(processName))
            {
                throw new Exception(String.Format("Already registered process {0}", processName));
            }
            processor(); // test instantiation
            var newProcess = new RFEngineProcessDefinition<P>
            {
                Name = processName,
                InstanceParams = instanceParams ?? (i => i.ExtractParam()?.ConvertTo<P>()),
                Processor = processor,
                Description = description
            };
            Processes.Add(processName, newProcess);
            return newProcess;
        }
        /*
        /// <summary>
        /// Configure a trigger for an event to start a process.
        /// </summary>
        public void AddProcessTrigger(Func<RFEvent, bool> evaluator, RFEngineProcessDefinition processConfig)
        {
            Triggers.Add(new RFSingleCommandTrigger(e => evaluator(e) ? new RFParamProcessInstruction(processConfig.Name, null) : null));
        }
        */
        /// <summary>
        /// Define a new process inside the engine triggered by a key update.
        /// </summary>
        /// <param name="processName">Unique user-friendly name.</param>
        /// <param name="processor">
        /// Function that creates the processor (may pass static config etc.)
        /// </param>
        /// <param name="triggerKey">Key that triggers the execution (matched by root).</param>
        /// <returns></returns>
        public RFEngineProcessDefinition AddProcessWithCatalogTrigger<P>(string processName, string description, Func<IRFEngineProcessor> processor,
            RFCatalogKey triggerKey) where P : RFEngineProcessorParam
        {
            if (Processes.ContainsKey(processName))
            {
                throw new Exception(String.Format("Already registered process {0}", processName));
            }
            processor(); // test instantiation

            //Func<RFInstruction, RFEngineProcessorParam> instanceParams = i => i.ExtractParam();// new P().ExtractFrom;

            var newProcess = new RFEngineProcessDefinition
            {
                Name = processName,
                InstanceParams = i => i.ExtractParam()?.ConvertTo<P>(),
                Processor = processor,
                Description = description
            };
            Processes.Add(processName, newProcess);

            AddCatalogUpdateTrigger<RFCatalogKey>(k => k.MatchesRoot(triggerKey), newProcess);

            return newProcess;
        }

        /// <summary>
        /// Configures process to automatically run on specific schedule
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="schedules"></param>
        /// <param name="range"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        public RFEngineTaskDefinition AddScheduledTask(string taskName, List<RFSchedulerSchedule> schedules, RFSchedulerRange range, RFEngineProcessDefinition process, bool isSystem)
        {
            return AddScheduledTask(taskName, () => schedules, () => range, process, isSystem);
        }

        /// <summary>
        /// Configures process to automatically run on specific schedule
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="schedules"></param>
        /// <param name="range"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        public RFEngineTaskDefinition AddScheduledTask(string taskName, Func<List<RFSchedulerSchedule>> schedulesFunc, Func<RFSchedulerRange> rangeFunc, RFEngineProcessDefinition process, bool isSystem)
        {
            var task = new RFScheduledEngineTaskDefinition
            {
                RangeFunc = rangeFunc,
                SchedulesFunc = schedulesFunc,
                TaskName = taskName,
                TaskProcess = process,
                IsSystem = isSystem
            };
            Tasks.Add(task);
            task.AddToEngine(this);
            return task;
        }

        /// <summary>
        /// Add a custom trigger.
        /// </summary>
        /// <param name="trigger"></param>
        public void AddTrigger(IRFEngineTrigger trigger)
        {
            Triggers.Add(trigger);
        }

        /// <summary>
        /// Configures process to automatically run on catalog update
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="triggerKey"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        public RFEngineTaskDefinition AddTriggeredTask(string taskName, RFCatalogKey triggerKey, RFEngineProcessDefinition process)
        {
            var task = new RFTriggeredEngineTaskDefinition
            {
                TriggerKey = triggerKey,
                TaskName = taskName,
                TaskProcess = process
            };
            Tasks.Add(task);
            task.AddToEngine(this);
            return task;
        }

        /// <summary>
        /// Create and add a new graph to the ending to run graph processes.
        /// </summary>
        /// <param name="graphName">Unique user-friendly name for the graph.</param>
        /// <returns>Use methods on the returned object to create graph processes.</returns>
        public RFGraphDefinition CreateGraph(string graphName)
        {
            var newGraph = new RFGraphDefinition(graphName, this);
            Graphs.Add(graphName, newGraph);
            return newGraph;
        }

        /// <summary>
        /// System will be supplying heartbeats on this key.
        /// </summary>
        public RFCatalogKey IntervalDocumentKey()
        {
            return KeyDomain.CreateSystemKey(RFPlane.Ephemeral, "interval", null);
        }
        /*
        /// <summary>
        /// Provide a description of a key for transparency.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public RFCatalogKey RegisterKey(RFCatalogKey key, string description)
        {
            Keys.Add(RFXMLSerializer.SerializeContract(key), description);
            return key;
        }*/

        public void AddService(string serviceName, Func<IRFProcessingContext, IRFBackgroundService> service)
        {
            if (Services.ContainsKey(serviceName))
            {
                throw new Exception(String.Format("Already registered service {0}", serviceName));
            }
            Services.Add(serviceName, service);
        }
    }
}
