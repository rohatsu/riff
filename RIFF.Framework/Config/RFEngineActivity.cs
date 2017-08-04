// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;

namespace RIFF.Framework
{
    public class RFEngineActivity : RFActivity
    {
        protected RFEngineDefinition _engineConfig;

        public RFEngineActivity(IRFProcessingContext context, RFEngineDefinition engineConfig) : base(context, null)
        {
            _engineConfig = engineConfig;
        }

        public RFEngineDefinition GetEngineConfig()
        {
            return _engineConfig;
        }

        public RFEngineStats GetEngineStats(RFGraphInstance instance)
        {
            RFEngineStats stats = null;
            var graphlessItem = Context.LoadEntry(RFEngineStatsKey.Create(_engineConfig.KeyDomain, null)) as RFDocument;
            if (graphlessItem != null)
            {
                stats = graphlessItem.GetContent<RFEngineStats>();
            }
            else
            {
                stats = new RFEngineStats
                {
                    GraphInstance = instance,
                    Stats = new Dictionary<string, RFEngineStat>()
                };
            }
            if (instance != null)
            {
                stats.GraphInstance = instance;
                var instanceItem = Context.LoadEntry(RFEngineStatsKey.Create(_engineConfig.KeyDomain, instance)) as RFDocument;
                if (instanceItem != null)
                {
                    var instanceStats = instanceItem.GetContent<RFEngineStats>();
                    foreach (var instanceStat in instanceStats.Stats)
                    {
                        if (stats.Stats.ContainsKey(instanceStat.Key))
                        {
                            stats.Stats.Remove(instanceStat.Key);
                        }
                        stats.Stats.Add(instanceStat.Key, instanceStat.Value);
                    }
                }
            }
            return stats;
        }

        public RFGraphStats GetGraphStats(string graphName, RFGraphInstance instance)
        {
            return Context.LoadDocumentContent<RFGraphStats>(RFGraphStatsKey.Create(_engineConfig.KeyDomain, graphName, instance));
        }
    }
}
