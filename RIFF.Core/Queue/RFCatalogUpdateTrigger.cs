// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFCatalogUpdateTrigger : RFSingleCommandTrigger
    {
        public RFCatalogUpdateTrigger(Func<RFCatalogKey, bool> evaluatorFunc, RFEngineProcessDefinition processConfig) : base(
            e => ((e is RFCatalogUpdateEvent) && evaluatorFunc((e as RFCatalogUpdateEvent).Key)) ? new RFParamProcessInstruction(processConfig.Name, new RFEngineProcessorKeyParam((e as RFCatalogUpdateEvent).Key)) : null)
        {
        }
    }
}
