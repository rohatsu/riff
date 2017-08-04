// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    /// <summary>
    /// First input is default, second input is override. If override is present, it will be chosen.
    /// </summary>
    public class RFInputOverrideProcessor<T> : RFInputSelectorProcessor<T> where T : class
    {
        public RFInputOverrideProcessor() : base(new Config
        {
            SelectOutput = (i1, i2) => i2 ?? i1
        })
        {
        }
    }

    public class RFInputSelectorProcessor<T> : RFGraphProcessorWithConfig<RFInputSelectorProcessor<T>.Domain, RFInputSelectorProcessor<T>.Config>
        where T : class
    {
        public class Config : IRFGraphProcessorConfig
        {
            [DataMember]
            public Func<T, T, T> SelectOutput { get; set; }
        }

        public class Domain : RFGraphProcessorDomain
        {
            [DataMember]
            [RFIOBehaviour(RFIOBehaviour.Input, false)]
            [RFDateBehaviour(RFDateBehaviour.Exact)]
            public T DefaultInput { get; set; }

            [DataMember]
            [RFIOBehaviour(RFIOBehaviour.Output)]
            [RFDateBehaviour(RFDateBehaviour.Exact)]
            public T Output { get; set; }

            [DataMember]
            [RFIOBehaviour(RFIOBehaviour.Input, false)]
            [RFDateBehaviour(RFDateBehaviour.Exact)]
            public T OverrideInput { get; set; }
        }

        public RFInputSelectorProcessor(Config config) : base(config)
        {
        }

        public override bool HasInstance(RFGraphInstance instance)
        {
            return true;
        }

        public override void Process(Domain domain)
        {
            domain.Output = _config.SelectOutput(domain.DefaultInput, domain.OverrideInput);
        }
    }
}
