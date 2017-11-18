using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if (false)
using Couchbase;
using Couchbase.Configuration.Client;

namespace RIFF.Core.Data
{
    internal class RFCouchbaseDocument
    {
        public string KeyString { get; set; }
        public string RootString { get; set; }
        public RFGraphInstance GraphInstance { get; set; }

        public string Type { get; set; }
        public byte[] Data { get; set; }
    }

    internal class RFCouchbaseCatalog : RFCatalog
    {
        private readonly Cluster _cluster;
        private readonly string _bucket;

        public RFCouchbaseCatalog(RFComponentContext context)
            : base(context)
        {
            _cluster = new Cluster();
            _bucket = "riff";
        }

        public override Dictionary<RFGraphInstance, RFCatalogKey> GetKeyInstances(RFCatalogKey key)
        {
            throw new NotImplementedException();
        }

        public override RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<long, RFCatalogKey> GetKeysByType(Type t)
        {
            throw new NotImplementedException();
        }

        public override RFCatalogEntry LoadItem(RFCatalogKey itemKey, int version = 0, bool ignoreContent = false)
        {
            using (var bucket = _cluster.OpenBucket(_bucket))
            {
                var keyString = itemKey.ToString();
                var doc = bucket.GetDocument<RFCouchbaseDocument>(keyString);
                throw new NotImplementedException();
            }
        }

        public override bool SaveItem(RFCatalogEntry item, bool overwrite = false)
        {
            using (var bucket = _cluster.OpenBucket(_bucket))
            {
                var keyString = item.Key.ToString();
                var result = bucket.Upsert(keyString, new RFCouchbaseDocument
                {
                    GraphInstance = item.Key.GraphInstance,
                    KeyString = keyString,
                    RootString = item.Key.RootKey().ToString(),
//                    RFCatalogEntry = item
                });
                throw new NotImplementedException();
            }
        }

        public override List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime = default(DateTime?), DateTime? endTime = default(DateTime?), int limitResults = 0, RFDate? valueDate = default(RFDate?), bool latestOnly = false)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
