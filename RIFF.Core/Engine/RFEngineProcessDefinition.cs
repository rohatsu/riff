// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    public class RFEngineProcessDefinition
    {
        public string Description { get; set; }

        public Func<RFInstruction, RFEngineProcessorParam> InstanceParams { get; set; }

        public string Name { get; set; }

        public Func<IRFEngineProcessor> Processor { get; set; }

        public bool IsExclusive { get; set; }
    }

    public class RFEngineProcessDefinition<P> : RFEngineProcessDefinition
    {
    }
}
