// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFDataSetBuilderConfig : IRFGraphProcessorConfig
    {
        [DataMember]
        public RFEnum DataSetCode { get; set; }
    }

    public class RFDataSetBuilderDomain<T> : RFGraphProcessorDomain where T : IRFDataSet
    {
        [RFIOBehaviour(RFIOBehaviour.Output)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public T DataSet { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public RFRawReport SourceReport { get; set; }
    }

    public abstract class RFDataSetBuilderProcessor<T> : RFGraphProcessor<RFDataSetBuilderDomain<T>> where T : IRFDataSet
    {
        protected RFDataSetBuilderConfig _config { get; set; }

        protected RFDataSetBuilderProcessor(RFDataSetBuilderConfig config)
        {
            _config = config;
        }
    }
}
