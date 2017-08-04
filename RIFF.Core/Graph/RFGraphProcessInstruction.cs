// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFGraphProcessInstruction : RFProcessInstruction
    {
        [DataMember]
        public RFGraphInstance Instance { get; set; }

        public RFGraphProcessInstruction(RFGraphInstance instance, string processName) : base(processName)
        {
            Instance = instance;
        }

        public override ItemType DispatchItemType()
        {
            return Core.ItemType.GraphProcessInstruction;
        }

        public override string DispatchKey()
        {
            return ToString();
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", ProcessName, Instance?.ValueDate?.ToString());
        }

        public override RFEngineProcessorParam ExtractParam()
        {
            return new RFEngineProcessorGraphInstanceParam(Instance);
        }
    }
}
