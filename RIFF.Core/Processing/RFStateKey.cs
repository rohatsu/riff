// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFStateKey : RFCatalogKey
    {
        [DataMember]
        public string GraphName { get; set; }

        [DataMember]
        public string ProcessName { get; set; }

        public static RFStateKey CreateKey(RFKeyDomain keyDomain, string graphName, string processName, RFGraphInstance instance)
        {
            return keyDomain.Associate<RFStateKey>(new RFStateKey
            {
                StoreType = RFStoreType.Document,
                Plane = RFPlane.User,
                GraphInstance = instance,
                GraphName = graphName,
                ProcessName = processName
            });
        }

        public override string FriendlyString()
        {
            return RFGraphDefinition.GetFullName(GraphName, ProcessName);
        }
    }
}
