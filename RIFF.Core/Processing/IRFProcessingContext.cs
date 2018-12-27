// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    /// <summary>
    /// Base context
    /// </summary>
    public interface IRFBaseContext
    {
        /// <summary>
        ///  Name of the environment as provided in Engine
        /// </summary>
        string Environment { get; }

        /// <summary>
        /// Log for system-level messages
        /// </summary>
        IRFLog SystemLog { get; }

        /// <summary>
        /// User configuration
        /// </summary>
        IRFUserConfig UserConfig { get; }

        /// <summary>
        /// Log for user-level messages
        /// </summary>
        IRFUserLog UserLog { get; }

        /// <summary>
        /// User access
        /// </summary>
        IRFUserRole UserRole { get; }

        /// <summary>
        /// Return current date
        /// </summary>
        RFDate Today { get; }
    }

    public interface IRFInternalsContext
    {
        IRFDispatchStore DispatchStore { get; }
    }

    /// <summary>
    /// Context available to processing logic (that might need to trigger events)
    /// </summary>
    public interface IRFProcessingContext : IRFBaseContext, IRFReadingContext, IRFWritingContext
    {
        void QueueInstruction(object raisedBy, RFInstruction instruction);

        void RaiseEvent(object raisedBy, RFEvent evt);

        /// <summary>
        /// Send data updates and instructions to the service and receive s tracker
        /// </summary>
        /// <returns></returns>
        RFProcessingTracker SubmitRequest(IEnumerable<RFCatalogEntry> inputs, IEnumerable<RFInstruction> instructions);
    }

    /// <summary>
    /// Read Only context
    /// </summary>
    public interface IRFReadingContext : IRFBaseContext
    {
        /// <summary>
        /// Retrieve all instanced for a specific key
        /// </summary>
        Dictionary<RFGraphInstance, RFCatalogKey> GetKeyInstances(RFCatalogKey key);

        /// <summary>
        /// Load free-form metadata for the key
        /// </summary>
        RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key);

        /// <summary>
        /// Retrieve all keys of a specific type
        /// </summary>
        Dictionary<long, T> GetKeysByType<T>() where T : RFCatalogKey;

        /// <summary>
        /// Retrieve all keys of a specific type
        /// </summary>
        Dictionary<long, RFCatalogKey> GetKeysByType(Type t);

        /// <summary>
        /// Load contents of a specific document from the store
        /// </summary>
        T LoadDocumentContent<T>(RFCatalogKey key, RFCatalogOptions options = null) where T : class;

        /// <summary>
        /// Load an entry from the store
        /// </summary>
        RFCatalogEntry LoadEntry(RFCatalogKey key, RFCatalogOptions options = null);

        // IRFCatalog pass-through

        /// <summary>
        /// Search for keys of specific types and dates
        /// </summary>
        List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime, DateTime? endTime, int limitResults, RFDate? valueDate, bool latestOnly);
    }

    public interface IRFSystemContext : IRFProcessingContext, IRFInternalsContext
    {
    }

    public interface IRFWritingContext : IRFBaseContext
    {
        /// <summary>
        /// Invalidates (deletes) a key
        /// </summary>
        /// <param name="key"></param>
        void Invalidate(RFCatalogKey key);

        /// <summary>
        /// Save a document in store
        /// </summary>
        void SaveDocument(RFCatalogKey key, object content, bool raiseEvent = true);

        /// <summary>
        /// Save an entry in store
        /// </summary>
        bool SaveEntry(RFCatalogEntry entry, bool raiseEvent = true, bool overwrite = false);
    }
}
