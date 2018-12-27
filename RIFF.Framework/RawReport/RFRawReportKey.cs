// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFRawReportKey : RFCatalogKey
    {
        [DataMember]
        public RFEnum ReportCode { get; set; }

        public static RFCatalogKey Create(RFKeyDomain domain, RFEnum reportCode, RFGraphInstance instance)
        {
            return domain.Associate(new RFRawReportKey
            {
                Plane = RFPlane.User,
                ReportCode = reportCode,
                StoreType = RFStoreType.Document,
                GraphInstance = instance
            });
        }

        public override string FriendlyString()
        {
            return string.Format("{0}", ReportCode);
        }
    }
}
