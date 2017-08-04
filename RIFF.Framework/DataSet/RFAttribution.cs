// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    public class RFAttributionBuilder<SR, AK, AR, MD> : RFGraphProcessorWithConfig<RFAttributionBuilderDomain<SR, AK, AR, MD>, RFAttributionConfig<SR, AK>>
        where SR : RFDataRow
        where AK : RFMappingKey
        where AR : RFMappingDataRow<AK>, new()
        where MD : RFMappingDataSet<AK, AR>, new()
    {
        public RFAttributionBuilder(RFAttributionConfig<SR, AK> config) : base(config)
        {
        }

        public override void Process(RFAttributionBuilderDomain<SR, AK, AR, MD> domain)
        {
            domain.AttributionDataSet = domain.AttributionDataSet ?? new MD { DataSetCode = _config.MappingDataSetCode };

            foreach (var requiredMapping in domain.SourceDataSet.Rows.GroupBy(r => _config.KeyExtractorFunc(r)))
            {
                domain.AttributionDataSet.GetOrCreateMapping(requiredMapping.Key);
            }
        }
    }

    public class RFAttributionBuilderDomain<SR, AK, AR, MD> : RFGraphProcessorDomain
            where SR : RFDataRow
        where AK : RFMappingKey
        where AR : RFMappingDataRow<AK>, new()
        where MD : RFMappingDataSet<AK, AR>, new()
    {
        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.State, false)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public RFMappingDataSet<AK, AR> AttributionDataSet { get; set; }

        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public RFDataSet<SR> SourceDataSet { get; set; }
    }

    public class RFAttributionConfig<SR, AK> : IRFGraphProcessorConfig
        where SR : RFDataRow
        where AK : RFMappingKey
    {
        [DataMember]
        public Func<SR, AK> KeyExtractorFunc { get; set; }

        [DataMember]
        public RFEnum MappingDataSetCode { get; set; }
    }
}
