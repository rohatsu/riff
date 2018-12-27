// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    public abstract class RFDataSetProcessor<I, O> : RFGraphProcessor<RFDataSetProcessorDomain<I, O>> where I : IRFDataSet where O : IRFDataSet
    {
        protected RFDataSetProcessorConfig mConfig { get; set; }

        protected RFDataSetProcessor(RFDataSetProcessorConfig config)
        {
            mConfig = config;
        }
    }

    public abstract class RFDataSetProcessor<I1, I2, O> : RFGraphProcessor<RFDataSetProcessorDomain<I1, I2, O>> where I1 : IRFDataSet where I2 : IRFDataSet where O : IRFDataSet
    {
        protected RFDataSetProcessorConfig mConfig { get; set; }

        protected RFDataSetProcessor(RFDataSetProcessorConfig config)
        {
            mConfig = config;
        }
    }

    public abstract class RFDataSetProcessor<I1, I2, I3, O> : RFGraphProcessor<RFDataSetProcessorDomain<I1, I2, I3, O>>
        where I1 : IRFDataSet
        where I2 : IRFDataSet
        where I3 : IRFDataSet
        where O : IRFDataSet
    {
        protected RFDataSetProcessorConfig mConfig { get; set; }

        protected RFDataSetProcessor(RFDataSetProcessorConfig config)
        {
            mConfig = config;
        }
    }

    public abstract class RFDataSetProcessor<I1, I2, I3, I4, O> : RFGraphProcessor<RFDataSetProcessorDomain<I1, I2, I3, I4, O>>
        where I1 : IRFDataSet
        where I2 : IRFDataSet
        where I3 : IRFDataSet
        where I4 : IRFDataSet
        where O : IRFDataSet
    {
        protected RFDataSetProcessorConfig mConfig { get; set; }

        protected RFDataSetProcessor(RFDataSetProcessorConfig config)
        {
            mConfig = config;
        }
    }

    [DataContract]
    public class RFDataSetProcessorConfig : IRFGraphProcessorConfig
    {
        [DataMember]
        public RFEnum DataSetCode { get; set; }
    }

    public class RFDataSetProcessorDomain<I, O> : RFGraphProcessorDomain where I : IRFDataSet where O : IRFDataSet
    {
        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I Input { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Output)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public O Output { get; set; }
    }

    public class RFDataSetProcessorDomain<I1, I2, O> : RFGraphProcessorDomain where I1 : IRFDataSet where I2 : IRFDataSet where O : IRFDataSet
    {
        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I1 Input1 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I2 Input2 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Output)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public O Output { get; set; }
    }

    public class RFDataSetProcessorDomain<I1, I2, I3, O> : RFGraphProcessorDomain where I1 : IRFDataSet where I2 : IRFDataSet where I3 : IRFDataSet where O : IRFDataSet
    {
        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I1 Input1 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I2 Input2 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I3 Input3 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Output)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public O Output { get; set; }
    }

    public class RFDataSetProcessorDomain<I1, I2, I3, I4, O> : RFGraphProcessorDomain where I1 : IRFDataSet where I2 : IRFDataSet where I3 : IRFDataSet where I4 : IRFDataSet where O : IRFDataSet
    {
        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I1 Input1 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I2 Input2 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I3 Input3 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public I4 Input4 { get; set; }

        [RFIOBehaviour(RFIOBehaviour.Output)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        [DataMember]
        public O Output { get; set; }
    }
}
