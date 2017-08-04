// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    internal abstract class RFStore<K, I> : RFPassiveComponent
            where K : RFCatalogKey
        where I : RFCatalogEntry
    {
        protected RFStore(RFComponentContext context)
            : base(context)
        {
        }

        public abstract Dictionary<RFGraphInstance, K> GetKeyInstances(RFCatalogKey key);

        public abstract RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key);

        public abstract Dictionary<long, K> GetKeysByType(Type t);

        public abstract I LoadItem(K itemKey, int version = 0, bool ignoreContent = false);

        public abstract bool SaveItem(I item, bool overwrite = false);

        public abstract List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime = null, DateTime? endTime = null, int limitResults = 0, RFDate? valueDate = null, bool latestOnly = false);
    }
}
