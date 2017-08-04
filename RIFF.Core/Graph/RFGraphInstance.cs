// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFGraphInstance : IComparable, IEquatable<RFGraphInstance>, IComparable<RFGraphInstance>
    {
        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public RFDate? ValueDate { get; set; }

        public static readonly string DEFAULT_INSTANCE = "default";

        /*public static RFGraphInstance DeriveFrom(RFInstruction i)
        {
            if (i is RFGraphProcessInstruction)
            {
                var gi = (i as RFGraphProcessInstruction).Instance;
                if (gi != null && !gi.IsNull())
                {
                    return gi;
                }
            }

            if (i is RFParamProcessInstruction)
            {
                var p = (i as RFParamProcessInstruction).Param;
                if (p is RFEngineProcessorGraphInstanceParam)
                {
                    var gi = (p as RFEngineProcessorGraphInstanceParam).Instance;
                    if (gi != null && !gi.IsNull())
                    {
                        return gi;
                    }
                }
            }

            if (i.Event != null && i.Event is RFCatalogUpdateEvent)
            {
                var gi = (i.Event as RFCatalogUpdateEvent).Key?.GraphInstance;
                if (gi != null && !gi.IsNull())
                {
                    return gi;
                }
            }

            return null;
        }*/

        public static RFCatalogOptions ImplyOptions(RFGraphIOMapping ioMapping/*, PropertyInfo propertyInfo*/)
        {
            if (ioMapping == null)
            {
                throw new RFSystemException(typeof(RFGraphInstance), "Missing IOMapping");
            }
            var options = new RFCatalogOptions();
            /*var dateBehaviour = ioMapping.DateBehaviour; propertyInfo.GetCustomAttributes(typeof(RFDateBehaviourAttribute), true).FirstOrDefault() as RFDateBehaviourAttribute;
            if (dateBehaviour != null)
            {
                options.DateBehaviour = dateBehaviour.DateBehaviour;
            }*/
            options.DateBehaviour = ioMapping.DateBehaviour;
            return options;
        }

        public RFGraphInstance Clone()
        {
            return MemberwiseClone() as RFGraphInstance;
        }

        public int CompareTo(object obj)
        {
            return ToString().CompareTo(obj?.ToString());
        }

        public int CompareTo(RFGraphInstance other)
        {
            return ToString().CompareTo(other.ToString());
        }

        public override bool Equals(object obj)
        {
            return (CompareTo(obj) == 0);
        }

        public bool Equals(RFGraphInstance other)
        {
            return ToString().Equals(other.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool IsNull()
        {
            return string.IsNullOrWhiteSpace(Name) && !ValueDate.HasValue;
        }

        public override string ToString()
        {
            return String.Format("{0} [{1}]", Name ?? string.Empty, ValueDate?.ToString("yyyy-MM-dd") ?? "*");
        }

        public RFGraphInstance WithDate(RFDate? date)
        {
            var clone = Clone();
            clone.ValueDate = date;
            return clone;
        }
    }
}
