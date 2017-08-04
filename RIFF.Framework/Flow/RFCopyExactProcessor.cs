// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    public class RFCopyExactProcessor : RFGraphProcessor<RFCopyExactProcessor.Domain>
    {
        public class Domain : RFGraphProcessorDomain
        {
            [DataMember]
            [RFIOBehaviour(RFIOBehaviour.Input, true)]
            //[RFDateBehaviour(RFDateBehaviour.Exact)]
            public object Input { get; set; }

            [DataMember]
            [RFIOBehaviour(RFIOBehaviour.Output)]
            //[RFDateBehaviour(RFDateBehaviour.Exact)]
            public object Output { get; set; }
        }

        public override bool HasInstance(RFGraphInstance instance)
        {
            return true;
        }

        public override void Process(Domain domain)
        {
            domain.Output = domain.Input;
        }
    }
}
