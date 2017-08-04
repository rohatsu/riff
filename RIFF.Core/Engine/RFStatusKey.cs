// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFStatusKey : RFCatalogKey
    {
        [DataMember]
        public string ProcessName { get; set; }

        public static RFStatusKey Create(RFKeyDomain domain, string processName, RFGraphInstance instance)
        {
            return domain.Associate(new RFStatusKey
            {
                ProcessName = processName,
                GraphInstance = instance,
                Plane = RFPlane.System,
                StoreType = RFStoreType.Document
            });
        }

        public override string FriendlyString()
        {
            return ProcessName;
        }
    }
}
