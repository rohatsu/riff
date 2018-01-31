// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RIFF.Core
{
    internal class RFSimpleEngine : RFPassiveComponent, IRFEngine
    {
        protected RFEngineDefinition _config;
        protected Dictionary<string, RFEngineProcess> _processes;
        protected List<RFEventReactor> _reactors;
        protected List<RFBackgroundServiceComponent> _services;

        public RFSimpleEngine(RFEngineDefinition config, RFComponentContext componentContext)
            : base(componentContext)
        {
            _config = config;
            _reactors = new List<RFEventReactor>();
            _processes = new Dictionary<string, RFEngineProcess>();
            _services = new List<RFBackgroundServiceComponent>();
        }

        public Dictionary<string, SortedSet<string>> GetDependencies()
        {
            var map = RFGraphMapper.MapGraphs(_config.Graphs.Values, true);
            return map.CalculateDependencies();
        }

        public Dictionary<string, int> GetWeights()
        {
            var map = RFGraphMapper.MapGraphs(_config.Graphs.Values, false);
            return map.CalculateWeights();
        }

        public SortedSet<string> GetExclusiveProcesses()
        {
            return new SortedSet<string>(_config.Processes.Where(p => p.Value.IsExclusive).Select(t => t.Value.Name));
        }

        public void Initialize(IRFProcessingContext serviceContext)
        {
            Log.Info(this, "Initializing RFSimpleEngine.");

            foreach(var processConfig in _config.Processes)
            {
                var newProcess = new RFEngineProcess(processConfig.Value.Name, processConfig.Value as RFEngineProcessDefinition, _config.KeyDomain);
                _processes.Add(processConfig.Value.Name, newProcess);
            }

            foreach(var trigger in _config.Triggers)
            {
                _reactors.Add(new RFTriggerReactor(trigger, _context.GetReadingContext()));
            }

            foreach(var graphConfig in _config.Graphs.Values)
            {
                AddGraph(graphConfig);
            }

            foreach(var service in _config.Services)
            {
                var backgroundService = new RFBackgroundServiceComponent(_context, service.Value(serviceContext));
                _services.Add(backgroundService);
                _reactors.Add(new RFServiceReactor { ServiceName = service.Key, Service = backgroundService });
            }

            if(_config.Schedules.Any())
            {
                var schedulerService = new RFBackgroundServiceComponent(_context, new RFSchedulerService(serviceContext, _config.Schedules));
                _services.Add(schedulerService);
                _reactors.Add(new RFServiceReactor { ServiceName = RFSchedulerService.SERVICE_NAME, Service = schedulerService });
            }
        }

        public RFProcessingResult Process(RFInstruction i, IRFProcessingContext processingContext)
        {
            try
            {
                if(i is RFProcessInstruction)
                {
                    var pi = i as RFProcessInstruction;
                    if(_processes.ContainsKey(pi.ProcessName))
                    {
                        var process = _processes[pi.ProcessName];
                        return ProcessInstruction(process, i as RFProcessInstruction, processingContext);
                    }
                    else
                    {
                        var msg = String.Format("Process {0} referenced by instruction {1} not found in engine configuration.", pi.ProcessName, i);
                        Log.Error(this, msg);
                        return RFProcessingResult.Error(new string[] { msg }, false);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Exception(this, ex, "Exception processing instruction {0}", i);
                return RFProcessingResult.Error(new string[] { ex.Message }, false);
            }
            return new RFProcessingResult();
        }

        public void React(RFEvent e, IRFProcessingContext processingContext)
        {
            var allInstructions = new BlockingCollection<RFInstruction>();
            if(e is RFIntervalEvent ie) // silently store intervals in database?
            {
                processingContext.SaveEntry(new RFDocument
                {
                    Content = ie.Interval,
                    Key = _config.IntervalDocumentKey(),
                    Type = typeof(RFInterval).FullName
                }, false, true);
            }
            Parallel.ForEach(_reactors, reactor =>
            {
                try
                {
                    var instructions = reactor.React(e);
                    if(instructions != null)
                    {
                        foreach(var instruction in instructions)
                        {
                            allInstructions.Add(instruction);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Exception(this, ex, "Reactor {0} threw an exception processing event {1}", reactor, e);
                }
            });

            foreach(var instruction in allInstructions)
            {
                processingContext.QueueInstruction(this, instruction);
            }
        }

        protected void AddGraph(RFGraphDefinition graphConfig)
        {
            foreach(var graphProcess in graphConfig.Processes.Values)
            {
                var graphProcessName = RFGraphDefinition.GetFullName(graphConfig.GraphName, graphProcess.Name);
                _processes.Add(graphProcessName,
                    new RFEngineProcess(graphProcessName,
                    new RFEngineProcessDefinition
                    {
                        InstanceParams = i => i.ExtractParam().ConvertTo<RFEngineProcessorGraphInstanceParam>(),
                        Name = graphProcessName,
                        Description = graphProcess.Description,
                        Processor = () => new RFGraphProcess(graphProcess)
                    }, _config.KeyDomain));

                // check for missing inputs
                foreach(var p in graphProcess.Processor().CreateDomain().GetType().GetProperties())
                {
                    if(RFReflectionHelpers.IsMandatory(p) && graphProcess.IOMappings.SingleOrDefault(m => m.Property.FullName() == p.FullName()) == null)
                    {
                        throw new RFSystemException(this, "Unmapped mandatory property {0} on processor {1}", p.FullName(), graphProcessName);
                    }
                }

                // auto-react to inputs
                foreach(var ioMapping in graphProcess.IOMappings)
                {
                    var ioBehaviour = ioMapping.Property.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute;
                    var dateBehaviour = ioMapping.DateBehaviour;
                    if(ioBehaviour != null && ioBehaviour.IOBehaviour == RFIOBehaviour.Input)
                    {
                        switch(dateBehaviour)
                        {
                            case RFDateBehaviour.Range:
                                _reactors.Add(RFGraphReactor.RangeReactor(ioMapping.Key, graphProcessName, _context.GetReadingContext(), ioMapping.RangeUpdateFunc, graphProcess.Processor().MaxInstance));
                                break;

                            case RFDateBehaviour.Exact:
                            case RFDateBehaviour.Latest:
                            case RFDateBehaviour.Previous:
                                _reactors.Add(RFGraphReactor.SimpleReactor(ioMapping.Key, dateBehaviour, graphProcessName, _context.GetReadingContext(), graphProcess.Processor().MaxInstance));
                                break;
                        }
                    }
                }
            }
        }

        protected void LogProcessingStat(RFEngineProcess process, RFProcessInstruction i, Stopwatch sw, DateTimeOffset startTime)
        {
            try
            {
                RFGraphInstance graphInstance = null;
                if(i is RFGraphProcessInstruction)
                {
                    return; // these are logged by graph stats
                    //graphInstance = (i as RFGraphProcessInstruction).Instance;
                }
                var statsKey = RFEngineStatsKey.Create(_config.KeyDomain, graphInstance);
                var statsDocument = _context.Catalog.LoadItem(statsKey, 0) as RFDocument;
                if(statsDocument == null)
                {
                    statsDocument = RFDocument.Create(statsKey,
                        new RFEngineStats
                        {
                            GraphInstance = graphInstance,
                            Stats = new Dictionary<string, RFEngineStat>()
                        });
                }
                var stat = new RFEngineStat
                {
                    ProcessName = process.Name,
                    LastDuration = sw.ElapsedMilliseconds,
                    LastRun = startTime
                };
                var statsItem = statsDocument.GetContent<RFEngineStats>();
                if(!statsItem.Stats.ContainsKey(process.Name))
                {
                    statsItem.Stats.Add(process.Name, stat);
                }
                else
                {
                    statsItem.Stats[process.Name] = stat;
                }
                statsDocument.UpdateTime = DateTimeOffset.Now;
                _context.Catalog.SaveItem(statsDocument, true); // don't keep versions
            }
            catch(Exception ex)
            {
                Log.Warning(this, "Error saving processing stats for process {0}: {1}", process.Name, ex.Message);
            }
        }

        protected RFProcessingResult ProcessInstruction(RFEngineProcess process, RFProcessInstruction i, IRFProcessingContext processingContext)
        {
            var result = new RFProcessingResult();
            try
            {
                var sw = Stopwatch.StartNew();
                var startTime = DateTimeOffset.Now;
                bool completed = false;
                try
                {
                    var processorInstance = process.CreateInstance();
                    if(processorInstance != null)
                    {
                        if(_config.MaxRuntime.Ticks > 0)
                        {
                            var maxRuntime = TimeSpan.FromTicks(Math.Max(processorInstance.MaxRuntime().Ticks, _config.MaxRuntime.Ticks));
                            var timerTask = Task.Delay(maxRuntime).ContinueWith(t =>
                             {
                                 if(!completed)
                                 {
                                     try
                                     {
                                         processorInstance.Cancel();
                                     }
                                     catch(Exception ex)
                                     {
                                         Log.Warning(this, "Exception cancelling process {0}: {1}", process.Name, ex.Message);
                                     }
                                     throw new TimeoutException(String.Format("Cancelling process {0} as it's taken too long (max runtime = {1} seconds).", process.Name, maxRuntime.TotalSeconds));
                                 }
                             });
                        }

                        result = process.RunInstance(processorInstance, i, processingContext);
                    }
                }
                catch(Exception ex) // hard exception, or softs should have bene handled by now
                {
                    var message = ex.InnerException?.Message ?? ex.Message;
                    result.AddMessage(message);
                    result.IsError = true;

                    result.ShouldRetry |= (ex is DbException || ex is TimeoutException || ex is RFTransientSystemException || ex?.InnerException is DbException || ex?.InnerException is TimeoutException);

                    Log.Exception(this, ex, "Exception running process {0}", process.Name);
                    /*processingContext.UserLog.LogEntry(new RFUserLogEntry
                    {
                        Action = "Error",
                        Area = null,
                        Description = String.Format("Error running process {0}: {1}", process.Name, message),
                        IsUserAction = false,
                        IsWarning = true,
                        Processor = process.Name
                    });*/
                }
                completed = true;
                LogProcessingStat(process, i, sw, startTime);
                Log.Debug(this, String.Format("Engine: process {0} process took {1} ms.", process.Name, sw.ElapsedMilliseconds));
            }
            catch(Exception ex) // a really bad system exception
            {
                Log.Exception(this, ex, "Exception processing instruction {0} by process {1}", i, process);

                result.AddMessage(ex.Message);
                result.IsError = true;
                result.ShouldRetry |= (ex is DbException || ex is TimeoutException || ex is RFTransientSystemException || ex?.InnerException is DbException || ex?.InnerException is TimeoutException);
            }
            return result;
        }
    }
}
