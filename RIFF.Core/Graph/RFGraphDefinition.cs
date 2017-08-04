// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Represents a configuration of a graph within an engine, which can be auto-instantiated based
    /// on parameters (date, string).
    /// </summary>
    [DataContract]
    public class RFGraphDefinition
    {
        [DataMember]
        public string GraphName { get; private set; }

        [DataMember]
        public List<RFGraphTaskDefinition> GraphTasks { get; private set; }

        [DataMember]
        public Dictionary<string, RFGraphProcessDefinition> Processes { get; private set; }

        [IgnoreDataMember]
        protected RFEngineDefinition EngineConfig { get; private set; }

        [IgnoreDataMember]
        protected RFKeyDomain KeyDomain { get { return EngineConfig.KeyDomain; } }

        public RFGraphDefinition(string graphName, RFEngineDefinition engineConfig)
        {
            GraphName = graphName;
            EngineConfig = engineConfig;
            GraphTasks = new List<RFGraphTaskDefinition>();
            Processes = new Dictionary<string, RFGraphProcessDefinition>();
        }

        public static string GetFullName(string graphName, string processName)
        {
            if (!string.IsNullOrWhiteSpace(graphName))
            {
                return string.Format("{0}/{1}", graphName, processName);
            }
            else
            {
                return processName;
            }
        }

        /// <summary>
        /// Register a new graph process. Use methods on the returned object to map IO.
        /// </summary>
        /// <param name="processName">Unique user-friendly name within this graph.</param>
        /// <param name="processor">Function creating the processor.</param>
        /// <returns></returns>
        public RFGraphProcessDefinition AddProcess(string processName, string description, Func<IRFGraphProcessorInstance> processor)
        {
            if (Processes.ContainsKey(processName))
            {
                throw new Exception(String.Format("Already registered process {0}", processName));
            }
            processor(); // test instantiation
            var newProcess = new RFGraphProcessDefinition
            {
                Name = processName,
                Processor = processor,
                GraphName = GraphName,
                Description = description,
                IOMappings = new List<RFGraphIOMapping>()
            };
            Processes.Add(processName, newProcess);
            return newProcess;
        }

        /// <summary>
        /// Register a new graph process. Use methods on the returned object to map IO.
        /// </summary>
        /// <param name="processName">Unique user-friendly name within this graph.</param>
        /// <param name="processor">Function creating the processor.</param>
        /// <returns></returns>
        public RFGraphProcessDefinition<D> AddProcess<D>(string processName, string description, Func<RFGraphProcessor<D>> processor) where D : RFGraphProcessorDomain, new()
        {
            if (Processes.ContainsKey(processName))
            {
                throw new Exception(String.Format("Already registered process {0}", processName));
            }
            processor(); // test instantiation
            var newProcess = new RFGraphProcessDefinition<D>
            {
                Name = processName,
                Processor = processor,
                GraphName = GraphName,
                Description = description,
                IOMappings = new List<RFGraphIOMapping>()
            };
            Processes.Add(processName, newProcess);
            return newProcess;
        }

        /// <summary>
        /// Configures graph process to automatically trigger on specific schedule
        /// </summary>
        /// <returns></returns>
        public RFGraphTaskDefinition AddScheduledTask<D>(string taskName, List<RFSchedulerSchedule> schedules, RFSchedulerRange range, RFGraphProcessDefinition process, RFGraphInstance instance)
            where D : RFGraphProcessorDomain
        {
            var triggerName = RFEnum.FromString(taskName);
            var triggerKey = RFManualTriggerKey.CreateKey(EngineConfig.KeyDomain, triggerName, instance);

            // map to process' input
            process.MapInput<D>(d => d.Trigger, triggerKey);

            var task = new RFScheduledGraphTaskDefinition
            {
                Range = range,
                Schedules = schedules,
                TaskName = taskName,
                GraphProcess = process,
                TriggerKey = triggerKey
            };
            GraphTasks.Add(task);

            task.AddToEngine(EngineConfig);

            return task;
        }
    }
}
