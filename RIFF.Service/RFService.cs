// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;

namespace RIFF.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class RFService : IRFService
    {
        protected IRFLog Log { get { return _context.SystemLog; } }

        protected static DateTime _lastRequestTime;
        protected static long _requestsServed;
        protected static object _sync = new object();
        protected static Dictionary<string, RFProcessingTracker> _trackers;
        protected IRFSystemContext _context;
        protected string _database;
        protected RFEngineDefinition _engineConfig;

        public RFService(IRFSystemContext context, RFEngineDefinition engineConfig, string database)
        {
            _context = context;
            _engineConfig = engineConfig;
            _database = database;
            _trackers = new Dictionary<string, RFProcessingTracker>();
        }

        public RFProcessingTracker GetProcessStatus(RFProcessingTrackerHandle trackerHandle)
        {
            try
            {
                Log.Debug(this, "GetProcessStatus {0}", trackerHandle.TrackerCode);
                LogRequest();
                RFProcessingTracker tracker = null;
                lock (_sync)
                {
                    _trackers.TryGetValue(trackerHandle.TrackerCode, out tracker);
                    if (tracker == null)
                    {
                        Log.Warning(this, "Unable to find tracker for {0}; current cache size {1}", trackerHandle.TrackerCode, _trackers.Count);
                    }
                }
                return tracker;
            }
            catch (Exception ex)
            {
                Log.Exception(this, "GetProcessStatus", ex);
                var tracker = new RFProcessingTracker(trackerHandle.TrackerCode);
                tracker.CycleFinished("dummy", RFProcessingResult.Error(new string[] { ex.Message }, false));
                tracker.SetComplete();
                return tracker;
            }
        }

        public RFProcessingTrackerHandle RetryError(string dispatchKey, RFUserLogEntry userLogEntry)
        {
            try
            {
                Log.Info(this, "RetryError {0}", dispatchKey);
                LogRequest();
                var activity = new RFRequestActivity(_context, _engineConfig);

                var qi = _context.DispatchStore.GetInstruction(dispatchKey);
                if (qi != null)
                {
                    return RegisterTracker(activity.Submit(null, new List<RFInstruction> { qi }, userLogEntry));
                }
                return new RFProcessingTrackerHandle
                {
                    TrackerCode = "error"
                };
            }
            catch (Exception ex)
            {
                Log.Exception(this, "RetryError", ex);
                return new RFProcessingTrackerHandle
                {
                    TrackerCode = "error"
                };
            }
        }

        public RFProcessingTrackerHandle RunProcess(bool isGraph, string processName, RFGraphInstance instance, RFUserLogEntry userLogEntry)
        {
            try
            {
                Log.Info(this, "RunProcess {0} / ({1},{2})", processName, instance != null ? instance.Name : null, instance != null ? instance.ValueDate : null);
                LogRequest();
                var activity = new RFRequestActivity(_context, _engineConfig);
                // TODO: how should we treat non-graph?
                return RegisterTracker(activity.Run(isGraph, processName, new RFEngineProcessorGraphInstanceParam(instance), userLogEntry));
            }
            catch (Exception ex)
            {
                Log.Exception(this, "RunProcess", ex);
                return new RFProcessingTrackerHandle
                {
                    TrackerCode = "error"
                };
            }
        }

        public RFServiceStatus Status()
        {
            var process = Process.GetCurrentProcess();
            lock (_sync)
            {
                return new RFServiceStatus
                {
                    Running = true,
                    WorkingSet = Environment.WorkingSet,
                    StartTime = process.StartTime,
                    NumThreads = process.Threads.Count,
                    RequestsServed = _requestsServed,
                    LastRequestTime = _lastRequestTime
                };
            }
        }

        public RFProcessingTrackerHandle SubmitAndProcess(IEnumerable<RFCatalogEntryDTO> inputs, RFUserLogEntry userLogEntry)
        {
            try
            {
                Log.Info(this, "SubmitAndProcess {0}", inputs.Count());
                LogRequest();
                var activity = new RFRequestActivity(_context, _engineConfig);
                return RegisterTracker(activity.Submit(inputs.Select(e => e.Deserialize()), userLogEntry));
            }
            catch (Exception ex)
            {
                Log.Exception(this, "SubmitAndProcess", ex);
                return new RFProcessingTrackerHandle
                {
                    TrackerCode = "error"
                };
            }
        }

        protected void LogRequest()
        {
            lock (_sync)
            {
                _requestsServed++;
                _lastRequestTime = DateTime.Now;
            }
        }

        protected RFProcessingTrackerHandle RegisterTracker(RFProcessingTracker tracker)
        {
            var guid = Guid.NewGuid().ToString();
            lock (_sync)
            {
                _trackers.Add(guid, tracker);
            }
            return new RFProcessingTrackerHandle
            {
                TrackerCode = guid
            };
        }
    }
}
