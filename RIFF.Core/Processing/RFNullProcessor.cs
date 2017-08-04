// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFNullProcessor : RFGraphProcessor<RFNullProcessorDomain>
    {
        public override void Process(RFNullProcessorDomain domain)
        {
            return;
        }
    }

    [DataContract]
    public class RFNullProcessorDomain : RFGraphProcessorDomain
    {
    }
}
