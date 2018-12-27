// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public abstract class RFKeyDomain
    {
        public abstract T Associate<T>(T key) where T : RFCatalogKey;

        public abstract RFCatalogKey CreateDocumentKey(string name, params string[] path);

        public abstract RFCatalogKey CreateKey(RFStoreType storeType, RFPlane plane, string name, params string[] path);

        public abstract RFCatalogKey CreateSystemKey(RFPlane plane, string name, params string[] path);
    }

    [DataContract]
    public class RFSimpleKeyDomain : RFKeyDomain
    {
        [DataMember]
        protected string Root { get; private set; }

        public RFSimpleKeyDomain(string root)
        {
            Root = root;
        }

        public override T Associate<T>(T key)
        {
            key.Root = Root;
            return key;
        }

        public override RFCatalogKey CreateDocumentKey(string name, params string[] path)
        {
            return CreateKey(RFStoreType.Document, RFPlane.User, name, path);
        }

        public override RFCatalogKey CreateKey(RFStoreType storeType, RFPlane plane, string name, params string[] path)
        {
            return new RFGenericCatalogKey
            {
                StoreType = storeType,
                Plane = plane,
                Root = Root,
                Name = name,
                Path = path != null ? string.Join(RFGenericCatalogKey.PATH_SEPARATOR, path.Where(t => !string.IsNullOrWhiteSpace(t))) : null
            };
        }

        public override RFCatalogKey CreateSystemKey(RFPlane plane, string name, params string[] path)
        {
            return CreateKey(RFStoreType.Document, plane, name, path);
        }
    }
}
