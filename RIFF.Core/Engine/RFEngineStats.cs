// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFEngineStat
    {
        [DataMember]
        public long LastDuration { get; set; }

        [DataMember]
        public DateTimeOffset LastRun { get; set; }

        [DataMember]
        public string ProcessName { get; set; }
    }

    [DataContract]
    public class RFEngineStats
    {
        [DataMember]
        public RFGraphInstance GraphInstance { get; set; }

        [DataMember]
        public Dictionary<string, RFEngineStat> Stats { get; set; }

        public RFEngineStats()
        {
            Stats = new Dictionary<string, RFEngineStat>();
        }

        public RFEngineStat GetStat(string processName)
        {
            if (Stats.ContainsKey(processName))
            {
                return Stats[processName];
            }
            return null;
        }
    }

    [DataContract]
    public class RFEngineStatsKey : RFCatalogKey
    {
        public static RFCatalogKey Create(RFKeyDomain domain, RFGraphInstance instance)
        {
            return domain.Associate(new RFEngineStatsKey
            {
                Plane = RFPlane.System,
                StoreType = RFStoreType.Document,
                GraphInstance = instance
            });
        }

        public override string FriendlyString()
        {
            if (GraphInstance != null && !GraphInstance.IsNull())
            {
                /*if (GraphInstance.ValueDate.HasValue)
                {
                    return String.Format("{0}/{1}", GraphInstance.Name, GraphInstance.ValueDate.Value.ToDateTime().ToString("yyyy-MM-dd"));
                }
                else*/
                {
                    return String.Format("{0}", GraphInstance.Name);
                }
            }
            return String.Empty;
        }
    }
}
