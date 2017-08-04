// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RIFF.Core
{
    internal class RFGraphProcess : RFEngineProcessor<RFEngineProcessorParam>
    {
        public string GraphName
        {
            get
            {
                return Config.GraphName;
            }
        }

        protected RFGraphProcessDefinition Config { get; set; }

        protected RFGraphInstance GraphInstance { get; set; }

        public RFGraphProcess(RFGraphProcessDefinition config)
        {
            Config = config;
        }

        public override void Initialize(RFEngineProcessorParam p, IRFProcessingContext context, RFKeyDomain keyDomain, string processName)
        {
            base.Initialize(p, context, keyDomain, processName);

            if (InstanceParams is RFEngineProcessorGraphInstanceParam)
            {
                GraphInstance = (InstanceParams as RFEngineProcessorGraphInstanceParam).Instance;
            }
            else
            {
                throw new RFSystemException(this, "Unable to extract GraphInstance from params");
            }
        }

        public override RFProcessingResult Process()
        {
            var result = new RFProcessingResult();
            var processor = Config.Processor();
            processor.Initialize(Context);
            if (!processor.HasInstance(GraphInstance))
            {
                // do not process if not existent
                Context.SystemLog.Debug(this, "Not running graph process {0} as flagged no instance for {1}", ProcessName, InstanceParams.ToString());
                return result;
            }
            var missingInputs = new SortedSet<string>();

            var dsw = Stopwatch.StartNew();
            var domain = LoadDomain(processor, Context, ref missingInputs);
            var domainLoadTime = dsw.ElapsedMilliseconds;
            long domainSaveTime = 0;
            var pd = Stopwatch.StartNew();
            try
            {
                if (domain != null)
                {
                    result.WorkDone = processor.ProcessDomain(domain);
                    result.AddMessages(new SortedSet<string>(processor.Log.GetErrors()));
                    var processingTime = pd.ElapsedMilliseconds;

                    if (processor.ProcessorStatus.CalculationOK)
                    {
                        var sd = Stopwatch.StartNew();
                        result.UpdatedKeys = SaveDomain(domain, Context);
                        result.WorkDone |= result.UpdatedKeys.Count > 0;
                        domainSaveTime = sd.ElapsedMilliseconds;
                    }
                    else // soft error - RFLogicException in processing (incorrect file, config etc.)
                    {
                        Context.SystemLog.Error(this, processor.ProcessorStatus.Message);
                        Context.UserLog.LogEntry(new RFUserLogEntry
                        {
                            Action = "Warning",
                            IsWarning = true,
                            Description = String.Format("Calculation error: {0}", processor.ProcessorStatus.Message),
                            IsUserAction = false,
                            Processor = Config.Name,
                            Username = "system",
                            Area = null,
                            ValueDate = GraphInstance.ValueDate ?? RFDate.NullDate
                        });
                    }

                    var md = Stopwatch.StartNew();
                    UpdateResult(processor.ProcessorStatus, result);
                    LogGraphStat(processor.ProcessorStatus);
                    var miscTime = md.ElapsedMilliseconds;

                    Context.SystemLog.Debug(this, "## Graph Process {0}: {1} ms total = {2} (load) + {3} (process) + {4} (save) + {5} (misc)",
                        this.ProcessName,
                        domainLoadTime + processingTime + domainSaveTime + miscTime,
                        domainLoadTime, processingTime, domainSaveTime, miscTime);

                    SetProcessEntry(new RFProcessEntry
                    {
                        IOTime = domainLoadTime + domainSaveTime + miscTime,
                        ProcessingTime = processingTime,
                        Success = processor.ProcessorStatus.CalculationOK,
                        Message = processor.ProcessorStatus.Message,
                        ProcessName = Config.Name,
                        GraphName = Config.GraphName,
                        GraphInstance = GraphInstance,
                        NumUpdates = result.UpdatedKeys?.Count ?? 0
                    });
                }
                else
                {
                    /* don't spam these messages any more
                     * domain = processor.CreateDomain();
                    domain.SetError("Not yet available: {0}", String.Join(",", missingInputs));
                    LogStatus(domain, false);
                    SaveDomain(domain, processor, Context);*/
                    Context.SystemLog.Info(this, "Not running graph process {0} as unable to construct full domain.", Config.Name);
                    //LogGraphStat(domain);
                }
            }
            catch (Exception ex) // serious (system) error like database disconnection, NULL etc
            {
                processor.ProcessorStatus.SetError(ex.Message);
                //UpdateResult(processor.ProcessorStatus, result);
                //Log.Error(this, "Exception running graph process {0}: {1}", Config.Name, ex.Message);
                LogGraphStat(processor.ProcessorStatus);

                SetProcessEntry(new RFProcessEntry
                {
                    IOTime = domainLoadTime,
                    ProcessingTime = pd.ElapsedMilliseconds,
                    Success = false,
                    Message = ex.Message,
                    ProcessName = Config.Name,
                    GraphName = Config.GraphName,
                    GraphInstance = GraphInstance,
                    NumUpdates = 0
                });

                throw; // report exception higher up
            }

            return result;
        }

        protected RFGraphProcessorDomain LoadDomain(IRFGraphProcessorInstance processor, IRFProcessingContext context, ref SortedSet<string> missingInputs)
        {
            var domain = processor.CreateDomain();
            domain.Instance = GraphInstance;
            var domainType = domain.GetType();

            var missingNames = new ConcurrentBag<string>();
            try
            {
                foreach (var propertyInfo in domainType.GetProperties())
                {
                    var ioBehaviour = propertyInfo.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute;
                    if (ioBehaviour != null && (ioBehaviour.IOBehaviour == RFIOBehaviour.Input || ioBehaviour.IOBehaviour == RFIOBehaviour.State))
                    {
                        var inputMapping = Config.IOMappings.SingleOrDefault(m => m.Property.Name == propertyInfo.Name);
                        if (inputMapping != null)
                        {
                            var options = RFGraphInstance.ImplyOptions(inputMapping);
                            if (options.DateBehaviour == RFDateBehaviour.Range)
                            {
                                if (inputMapping.RangeRequestFunc == null)
                                {
                                    if (ioBehaviour.IsMandatory)
                                    {
                                        Context.SystemLog.Warning(this, "No range specified for mandatory ranged input {0} on process {1}", propertyInfo.FullName(), this.ProcessName);
                                    }
                                    continue;
                                }
                                var dateRange = inputMapping.RangeRequestFunc(GraphInstance);
                                options.DateBehaviour = RFDateBehaviour.Exact; // override to load one-by-one

                                var rangeInput = Activator.CreateInstance(propertyInfo.PropertyType) as IRFRangeInput;
                                foreach (var vd in dateRange)
                                {
                                    var inputKey = inputMapping.Key.CreateForInstance(GraphInstance.WithDate(vd));

                                    var item = context.LoadEntry(inputKey, options);
                                    if (item != null && item is RFDocument)
                                    {
                                        var content = (item as RFDocument).Content;
                                        if (content != null)
                                        {
                                            rangeInput.Add(vd, content);
                                        }
                                    }
                                }

                                try
                                {
                                    if (propertyInfo.PropertyType.IsAssignableFrom(rangeInput.GetType()))
                                    {
                                        propertyInfo.SetValue(domain, rangeInput);
                                    }
                                    else
                                    {
                                        Context.SystemLog.Warning(this, "Not assigning value of type {0} to property {1} of type {2} due to type mismatch [{3}]", rangeInput.GetType().FullName,
                                            propertyInfo.FullName(), propertyInfo.PropertyType.FullName, domain.Instance?.ValueDate);
                                    }
                                }
                                catch (Exception)
                                {
                                    Context.SystemLog.Info(this, "Domain mismatch on property {0} for vd {1} - stale data?", propertyInfo.FullName(), domain.Instance.ValueDate);
                                }
                            }
                            else
                            {
                                var inputKey = inputMapping.Key.CreateForInstance(GraphInstance);
                                var item = context.LoadEntry(inputKey, options);
                                if (item != null && item is RFDocument)
                                {
                                    try
                                    {
                                        var content = (item as RFDocument).Content;
                                        if (content != null)
                                        {
                                            if (propertyInfo.PropertyType.IsAssignableFrom(content.GetType()))
                                            {
                                                propertyInfo.SetValue(domain, content);
                                            }
                                            else
                                            {
                                                Context.SystemLog.Warning(this, "Not assigning value of type {0} to property {1} of type {2} due to type mismatch [{3}]", content.GetType().FullName,
                                                    propertyInfo.FullName(), propertyInfo.PropertyType.FullName, domain.Instance?.ValueDate);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Context.SystemLog.Info(this, "Domain mismatch on property {0} for vd {1} - stale data?", propertyInfo.FullName(), domain.Instance.ValueDate);
                                    }
                                }
                                else if (ioBehaviour.IsMandatory)
                                {
                                    missingNames.Add(propertyInfo.Name);
                                    // return null;// performance short circuit - only report one missing
                                }
                            }
                        }
                    }
                }
                missingInputs.UnionWith(missingNames);
                if (missingInputs.Count > 0)
                {
                    return null;
                }
                return domain;
            }
            catch (Exception ex)
            {
                Context.SystemLog.Exception(this, "Error loading domain", ex);
                return null;
            }
        }

        protected void LogGraphStat(RFGraphProcessorStatus status)
        {
            try
            {
                var statsKey = RFGraphStatsKey.Create(KeyDomain, Config.GraphName, GraphInstance);
                var statsDocument = Context.LoadEntry(statsKey) as RFDocument;
                if (statsDocument == null)
                {
                    statsDocument = RFDocument.Create(statsKey,
                        new RFGraphStats
                        {
                            GraphInstance = GraphInstance,
                            GraphName = Config.GraphName,
                            Stats = new Dictionary<string, RFGraphStat>()
                        });
                }
                var stat = new RFGraphStat
                {
                    ProcessName = Config.Name,
                    LastRun = status?.Updated ?? DateTimeOffset.MinValue,
                    CalculationOK = status?.CalculationOK ?? false,
                    Message = status?.Message ?? "n/a"
                };
                var statsItem = statsDocument.GetContent<RFGraphStats>();
                if (!statsItem.Stats.ContainsKey(Config.Name))
                {
                    statsItem.Stats.Add(Config.Name, stat);
                }
                else
                {
                    statsItem.Stats[Config.Name] = stat;
                }
                statsDocument.UpdateTime = DateTimeOffset.Now;
                Context.SaveEntry(statsDocument, false, true); // don't keep versions
            }
            catch (Exception ex)
            {
                Context.SystemLog.Exception(this, ex, "Error saving graph stats for process {0}", Config.Name);
            }
        }

        protected List<RFCatalogKey> SaveDomain(RFGraphProcessorDomain domain, IRFProcessingContext context)
        {
            var updates = new List<RFCatalogKey>();
            var domainType = domain.GetType();
            int numUpdates = 0;
            foreach (var propertyInfo in domainType.GetProperties())
            {
                var ioBehaviour = propertyInfo.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute;
                if (ioBehaviour != null && (ioBehaviour.IOBehaviour == RFIOBehaviour.Output || ioBehaviour.IOBehaviour == RFIOBehaviour.State))
                {
                    var value = propertyInfo.GetValue(domain);
                    if (value != null)
                    {
                        foreach (var outputMapping in Config.IOMappings.Where(m => m.Property.Name == propertyInfo.Name))
                        {
                            var outputKey = outputMapping.Key.CreateForInstance(this.GraphInstance);
                            var options = RFGraphInstance.ImplyOptions(outputMapping);
                            // by default date will be set from the graph instance
                            if (options.DateBehaviour == RFDateBehaviour.Dateless)
                            {
                                outputKey.GraphInstance.ValueDate = null;
                            }
                            bool isState = (ioBehaviour.IOBehaviour == RFIOBehaviour.State);
                            var hasUpdated = context.SaveEntry(new RFDocument
                            {
                                Content = value,
                                Key = outputKey,
                                Type = value.GetType().FullName
                            }, !isState);
                            if (hasUpdated)
                            {
                                updates.Add(outputKey);
                                numUpdates++;
                            }
                        }
                    }
                }
            }
            return updates;
        }

        protected void UpdateResult(RFGraphProcessorStatus status, RFProcessingResult result)
        {
            if (status != null && result != null)
            {
                if (status.CalculationOK)
                {
                    var date = InstanceParams != null && GraphInstance.ValueDate.HasValue ? GraphInstance.ValueDate.Value.ToString() : "n/a";
                    if (!result.WorkDone)
                    {
                        Context.SystemLog.Info(this, "Run process {0} but no outputs have changed.", String.Format("{0} [{1}]", ProcessName, date));
                    }
                }
                else
                {
                    var date = InstanceParams != null && GraphInstance.ValueDate.HasValue ? GraphInstance.ValueDate.Value.ToString() : "n/a";
                    result.AddMessage(status.Message);
                    result.IsError = true;
                }
            }
        }
    }
}
