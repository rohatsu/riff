// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFFile : IRFContentDrivenName
    {
        [DataMember]
        public RFFileTrackedAttributes Attributes { get; set; }

        public string ContentName
        {
            get
            {
                return UniqueKey;
            }
        }

        [DataMember]
        public byte[] Data { get; set; }

        [DataMember]
        public RFEnum FileKey { get; set; }

        [DataMember]
        public string UniqueKey { get; set; }

        [DataMember]
        public RFDate? ValueDate { get; set; }
    }

    [DataContract]
    public class RFFileKey : RFCatalogKey
    {
        [DataMember(EmitDefaultValue = false)]
        public RFEnum FileKey { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string UniqueKey { get; set; }

        public static RFCatalogKey Create(RFKeyDomain domain, RFEnum fileKey, string uniqueKey)
        {
            return domain.Associate(new RFFileKey
            {
                FileKey = fileKey,
                UniqueKey = uniqueKey,
                StoreType = RFStoreType.Document,
                Plane = RFPlane.User
            });
        }

        public override string FriendlyString()
        {
            return string.Format("{0}/{1}", FileKey, UniqueKey);
        }

        public override bool MatchesRoot(RFCatalogKey key)
        {
            var fileKey = key as RFFileKey;
            if (fileKey != null)
            {
                return fileKey.FileKey == FileKey;
            }
            return false;
        }
    }
}
