// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFGraphStat
    {
        [DataMember]
        public bool CalculationOK { get; set; }

        [DataMember]
        public DateTimeOffset LastRun { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string ProcessName { get; set; }
    }

    [DataContract]
    public class RFGraphStats
    {
        [DataMember]
        public RFGraphInstance GraphInstance { get; set; }

        [DataMember]
        public string GraphName { get; set; }

        [DataMember]
        public Dictionary<string, RFGraphStat> Stats { get; set; }

        public RFGraphStats()
        {
            Stats = new Dictionary<string, RFGraphStat>();
        }

        public RFGraphStat GetStat(string processName)
        {
            if (Stats.ContainsKey(processName))
            {
                return Stats[processName];
            }
            return null;
        }
    }

    [DataContract]
    public class RFGraphStatsKey : RFCatalogKey
    {
        [DataMember]
        public string GraphName { get; set; }

        public static RFCatalogKey Create(RFKeyDomain domain, string graphName, RFGraphInstance instance)
        {
            return domain.Associate(new RFGraphStatsKey
            {
                Plane = RFPlane.System,
                StoreType = RFStoreType.Document,
                GraphInstance = instance,
                GraphName = graphName
            });
        }

        public override string FriendlyString()
        {
            if (GraphInstance != null && !GraphInstance.IsNull())
            {
                return String.Format("{0}/{1}", GraphName, GraphInstance.Name);
            }
            return String.Empty;
        }
    }
}
