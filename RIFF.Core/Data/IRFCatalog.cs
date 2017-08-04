// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    /// <summary>
    /// Provides access to the catalog - an object store, however you should use methods on
    /// RFProcessingContext to perform I/O.
    /// </summary>
    public interface IRFCatalog
    {
        Dictionary<RFGraphInstance, RFCatalogKey> GetKeyInstances(RFCatalogKey key);

        RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key);

        Dictionary<long, RFCatalogKey> GetKeysByType(Type t);

        RFCatalogEntry LoadItem(RFCatalogKey itemKey, int version = 0, bool ignoreContent = false);

        bool SaveItem(RFCatalogEntry item, bool overwrite = false);

        List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime, DateTime? endTime, int limitResults, RFDate? valueDate, bool latestOnly);
    }
}
