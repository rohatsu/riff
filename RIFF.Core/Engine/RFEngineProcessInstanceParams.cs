// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFEngineProcessorGraphInstanceParam : RFEngineProcessorParam
    {
        [DataMember]
        public RFGraphInstance Instance { get; set; }

        public RFEngineProcessorGraphInstanceParam(RFGraphInstance instance)
        {
            Instance = instance;
        }

        /*
        public static RFEngineProcessorParam CreateFrom(RFInstruction i)
        {
            var p = new RFEngineProcessorGraphInstanceParam();
            return p.ExtractFrom(i);
        }

        public override RFEngineProcessorParam ExtractFrom(RFInstruction i)
        {
            if (i is RFGraphProcessInstruction)
            {
                return new RFEngineProcessorGraphInstanceParam
                {
                    Instance = (i as RFGraphProcessInstruction).Instance
                };
            }

            if (i is RFParamProcessInstruction)
            {
                return (i as RFParamProcessInstruction).Param;
            }

            if (i.Event is RFCatalogUpdateEvent)
            {
                var key = (i.Event as RFCatalogUpdateEvent).Key;
                if (key != null)
                {
                    return new RFEngineProcessorGraphInstanceParam
                    {
                        Instance = key.GraphInstance
                    };
                }
            }

            throw new RFSystemException(typeof(RFEngineProcessorGraphInstanceParam), "Unable to extract Graph Instance param from instruction {0}", i);
        }*/

        public override string ToString()
        {
            return Instance?.ToString() ?? "null";
        }
    }

    [DataContract]
    public class RFEngineProcessorIntervalParam : RFEngineProcessorParam
    {
        [DataMember]
        public RFInterval Interval { get; set; }

        public RFEngineProcessorIntervalParam(RFInterval interval)
        {
            Interval = interval;
        }
        /*
        public override RFEngineProcessorParam ExtractFrom(RFInstruction i)
        {
            if (i is RFIntervalInstruction)
            {
                return new RFEngineProcessorIntervalParam
                {
                    Interval = (i as RFIntervalInstruction).Interval
                };
            }
            return null;
        }*/
    }

    [DataContract]
    public abstract class RFEngineProcessorParam : IComparable
    {
        public int CompareTo(object obj)
        {
            // this is really slow
            return string.Compare(RFXMLSerializer.SerializeContract(this), RFXMLSerializer.SerializeContract(obj), StringComparison.Ordinal);
        }

        public static bool Equals(RFEngineProcessorParam l, RFEngineProcessorParam r)
        {
            if(l == null && r == null)
            {
                return true;
            } else if (l != null && r != null)
            {
                return l.Equals(r);
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        /// <summary>
        /// Attempt to extract a different param type if possible, cast by default
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <returns></returns>
        public virtual P ConvertTo<P>() where P : RFEngineProcessorParam
        {
            return this as P;
        }

        /*
        public virtual RFEngineProcessorParam ExtractFrom(RFInstruction i)
        {
            return new RFEngineProcessorParam();
        }*/

        public override int GetHashCode()
        {
            return RFXMLSerializer.SerializeContract(this).GetHashCode();
        }
    }

    [DataContract]
    public class RFEngineProcessorKeyParam : RFEngineProcessorParam
    {
        [DataMember]
        public RFCatalogKey Key { get; set; }

        public RFEngineProcessorKeyParam(RFCatalogKey key)
        {
            Key = key;
        }

        public override P ConvertTo<P>()
        {
            if(typeof(P) == typeof(RFEngineProcessorGraphInstanceParam) && Key != null)
            {
                return new RFEngineProcessorGraphInstanceParam(Key.GraphInstance) as P;
            }
            return base.ConvertTo<P>();
        }

        /*
        public override RFEngineProcessorParam ExtractFrom(RFInstruction i)
        {
            if (i.Event is RFCatalogUpdateEvent)
            {
                return new RFEngineProcessorKeyParam { Key = (i.Event as RFCatalogUpdateEvent).Key };
            }

            throw new RFSystemException(typeof(RFEngineProcessorGraphInstanceParam), "Unable to extract Key param from instruction {0}", i);
        }*/
    }
}
