// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RIFF.Core
{
    internal class RFEngineProcess
    {
        public RFEngineProcessDefinition Config { get; set; }

        public RFKeyDomain KeyDomain { get; set; }

        public string Name { get; private set; }

        public RFEngineProcess(string name, RFEngineProcessDefinition config, RFKeyDomain keyDomain)
        {
            Name = name;
            Config = config;
            KeyDomain = keyDomain;
        }

        public IRFEngineProcessor CreateInstance()
        {
            return Config.Processor();
        }

        public RFProcessingResult RunInstance(IRFEngineProcessor processorInstance, RFInstruction instruction, IRFProcessingContext context)
        {
            var pi = instruction as RFProcessInstruction;
            if (pi != null)
            {
                var sw = Stopwatch.StartNew();
                RFEngineProcessorParam instanceParams = null;
                try
                {
                    instanceParams = Config.InstanceParams(instruction);
                    processorInstance.Initialize(instanceParams, context, KeyDomain, Config.Name);
                    var result = processorInstance.Process();
                    result.AddMessages(processorInstance.Log.GetErrors());
                    if ((result.WorkDone || result.IsError) && !(processorInstance is RFSchedulerProcessor))
                    {
                        context.SystemLog.LogProcess(this, processorInstance.GetProcessEntry() ?? new RFProcessEntry
                        {
                            GraphInstance = (instanceParams as RFEngineProcessorGraphInstanceParam)?.Instance,
                            GraphName = (processorInstance as RFGraphProcess)?.GraphName,
                            IOTime = 0,
                            ProcessingTime = sw.ElapsedMilliseconds,
                            Message = String.Join("\r\n", result.Messages),
                            ProcessName = Config.Name,
                            Success = !result.IsError,
                            NumUpdates = 1
                        });

                        context.SystemLog.Info(this, "Run process {0}{1} in {2}ms", Config.Name, instanceParams != null ? String.Format(" ({0})", instanceParams) : String.Empty, sw.ElapsedMilliseconds);
                    }
                    return result;
                }
                catch (RFLogicException ex) // soft exception (incorrect file etc.)
                {
                    context.UserLog.LogEntry(new RFUserLogEntry
                    {
                        Action = "Warning",
                        IsWarning = true,
                        Description = String.Format("Calculation error: {0}", ex.Message),
                        IsUserAction = false,
                        Processor = Config.Name,
                        Username = "system",
                        Area = null,
                        ValueDate = RFDate.NullDate
                    });

                    context.SystemLog.Warning(this, "Logic Exception on process {0}: {1}", Config.Name, ex.Message);

                    context.SystemLog.LogProcess(this, processorInstance.GetProcessEntry() ?? new RFProcessEntry
                    {
                        GraphInstance = (instanceParams as RFEngineProcessorGraphInstanceParam)?.Instance,
                        GraphName = (processorInstance as RFGraphProcess)?.GraphName,
                        IOTime = 0,
                        ProcessingTime = sw.ElapsedMilliseconds,
                        Message = ex.Message,
                        ProcessName = Config.Name,
                        Success = false,
                        NumUpdates = 0
                    });

                    var result = new RFProcessingResult
                    {
                        IsError = true,
                        Messages = new SortedSet<string>(processorInstance?.Log?.GetErrors() ?? new string[0]),
                        WorkDone = false,
                        ShouldRetry = false,
                        UpdatedKeys = new System.Collections.Generic.List<RFCatalogKey>()
                    };
                    result.AddMessage(ex.Message);

                    return result;
                }
                catch (Exception ex) // hard exception - system, null etc.
                {
                    context.SystemLog.LogProcess(this, processorInstance?.GetProcessEntry() ?? new RFProcessEntry
                    {
                        GraphInstance = (instanceParams as RFEngineProcessorGraphInstanceParam)?.Instance,
                        GraphName = (processorInstance as RFGraphProcess)?.GraphName,
                        IOTime = 0,
                        ProcessingTime = sw.ElapsedMilliseconds,
                        Message = ex.Message,
                        ProcessName = Config.Name,
                        Success = false,
                        NumUpdates = 0
                    });

                    throw;
                }
            }
            throw new RFSystemException(this, "Cannot process empty instruction");
        }
    }
}
