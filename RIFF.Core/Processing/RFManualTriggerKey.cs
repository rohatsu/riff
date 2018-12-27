// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFManualTriggerKey : RFCatalogKey
    {
        [DataMember]
        public RFEnum TriggerCode { get; set; }

        public static RFManualTriggerKey CreateKey(RFKeyDomain keyDomain, RFEnum triggerCode, RFGraphInstance instance)
        {
            return keyDomain.Associate<RFManualTriggerKey>(new RFManualTriggerKey
            {
                StoreType = RFStoreType.Document,
                Plane = RFPlane.User,
                GraphInstance = instance,
                TriggerCode = triggerCode
            });
        }

        public override string FriendlyString()
        {
            return TriggerCode.ToString();
        }
    }
}
