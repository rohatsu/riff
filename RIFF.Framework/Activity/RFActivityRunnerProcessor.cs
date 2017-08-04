// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class ActionTracker
    {
        [DataMember]
        public bool AlreadyRun { get; set; }

        [DataMember]
        public string LastRunBy { get; set; }

        [DataMember]
        public DateTimeOffset LastRunTime { get; set; }
    }

    /// <summary>
    /// Processor that runs a specific IRFActivity's action with optional ability to track/limit past
    /// executions (i.e. once a day)
    /// </summary>
    /// <typeparam name="A">Activity type</typeparam>
    /// <typeparam name="P">Parameter type</typeparam>
    public class RFActivityRunnerProcessor<A, P> : RFEngineProcessorWithConfig<P, RFActivityRunnerProcessor<A, P>.Config> where A : IRFActivity where P : RFEngineProcessorParam
    {
        public class Config : IRFEngineProcessorConfig
        {
            public Func<A, P, bool> Action { get; set; } // return true if success, otherwise false (will be re-run)
            public Func<IRFProcessingContext, A> Activity { get; set; }
            public Func<IRFProcessingContext, P, bool> ShouldRun { get; set; }
            public Func<P, RFCatalogKey> TrackerKey { get; set; }
        }

        public RFActivityRunnerProcessor(Config config) : base(config)
        {
        }

        public override RFProcessingResult Process()
        {
            var result = new RFProcessingResult();
            using (var activity = _config.Activity(Context))
            {
                if (_config.ShouldRun != null && !_config.ShouldRun(Context, InstanceParams))
                {
                    Log.Info("Not running activity {0} as marked for not running in this instance.", typeof(A).ToString());
                    return result;
                }

                var trackerKey = _config.TrackerKey != null ? _config.TrackerKey(InstanceParams) : null;
                if (trackerKey != null)
                {
                    var tracker = Context.LoadDocumentContent<ActionTracker>(trackerKey);
                    if (tracker != null)
                    {
                        if (tracker.AlreadyRun)
                        {
                            Log.Info("Not running activity {0} as already run.", typeof(A).ToString());
                            return result;
                        }
                    }
                }

                if (_config.Action(activity, InstanceParams)) // return true to indicate it has run
                {
                    if (trackerKey != null)
                    {
                        Context.SaveDocument(trackerKey, new ActionTracker
                        {
                            AlreadyRun = true,
                            LastRunBy = "system",
                            LastRunTime = DateTimeOffset.Now
                        });
                    }

                    result.WorkDone = true;
                    return result;
                }
            }
            return result; // no work done
        }
    }
}
