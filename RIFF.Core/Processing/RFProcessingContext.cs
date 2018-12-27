// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RIFF.Core
{
    public class RFCatalogKeyMetadata
    {
        public string ContentType { get; set; }

        public long DataSize { get; set; }

        public RFGraphInstance Instance { get; set; }

        public bool IsValid { get; set; }

        public RFCatalogKey Key { get; set; }

        public long KeyReference { get; set; }

        public string KeyType { get; set; }

        public RFMetadata Metadata { get; set; }

        public DateTimeOffset UpdateTime { get; set; }
    }

    public class RFCatalogOptions
    {
        public RFDateBehaviour DateBehaviour { get; set; }

        public bool IgnoreContent { get; set; }

        public int Version { get; set; }

        public RFCatalogOptions()
        {
            DateBehaviour = RFDateBehaviour.NotSet;
            Version = 0;
            IgnoreContent = false;
        }
    }

    /// <summary>
    /// Helper object for accessing framework object during the calculation.
    /// </summary>
    internal class RFProcessingContext : IRFSystemContext
    {
        public IRFDispatchStore DispatchStore { get { return _dispatchStore; } }
        public string Environment { get; set; }
        public string ProcessingKey { get; set; } // specific to this calculation

        public IRFLog SystemLog { get { return RFStatic.Log; } }
        public IRFUserConfig UserConfig { get; set; }
        public IRFUserLog UserLog { get; set; }

        public IRFUserRole UserRole { get; set; }

        public RFDate Today => RFDate.Today(); // for regression testing

        protected IRFCatalog _catalog;
        protected IRFDispatchStore _dispatchStore;
        protected IRFEventSink _events;
        protected IRFInstructionSink _instructions;
        protected Dictionary<string, RFCatalogEntry> _memoryStore;
        protected RFDispatchQueueMonitorBase _workQueue;

        protected RFProcessingContext()
        { }

        public static RFProcessingContext Create(RFComponentContext component, string processingKey, IRFInstructionSink instructionManager, IRFEventSink eventManager, RFDispatchQueueMonitorBase workQueue)
        {
            return new RFProcessingContext
            {
                _instructions = instructionManager,
                _events = eventManager,
                _catalog = component.Catalog,
                _memoryStore = component.MemoryStore,
                UserConfig = component.UserConfig,
                Environment = component.SystemConfig.Environment,
                _workQueue = workQueue,
                UserLog = component.UserLog,
                UserRole = component.UserRole,
                ProcessingKey = processingKey,
                _dispatchStore = component.DispatchStore
            };
        }

        public Dictionary<RFGraphInstance, RFCatalogKey> GetKeyInstances(RFCatalogKey key)
        {
            return _catalog.GetKeyInstances(key);
        }

        public RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key)
        {
            return _catalog.GetKeyMetadata(key);
        }

        public Dictionary<long, T> GetKeysByType<T>() where T : RFCatalogKey
        {
            return _catalog.GetKeysByType(typeof(T)).ToDictionary(k => k.Key, k => k.Value as T);
        }

        public Dictionary<long, RFCatalogKey> GetKeysByType(Type t)
        {
            return _catalog.GetKeysByType(t);
        }

        public void Invalidate(RFCatalogKey key)
        {
            try
            {
                var entry = LoadEntry(key);
                if (entry != null)
                {
                    entry.SetInvalid();
                    SaveEntry(entry, false, true);
                }
            }
            catch (Exception ex)
            {
                throw new RFSystemException(this, ex, "Error invalidating {0}", key);
            }
        }

        public T LoadDocumentContent<T>(RFCatalogKey key, RFCatalogOptions options = null) where T : class
        {
            var document = (LoadEntry(key, options) as RFDocument);
            if (document != null && document.IsValid)
            {
                return document.GetContent<T>();
            }
            return null;
        }

        public RFCatalogEntry LoadEntry(RFCatalogKey key, RFCatalogOptions options = null)
        {
            DefaultOptions(ref options);
            if (key is RFCatalogKey)
            {
                lock (_memoryStore)
                {
                    if (_memoryStore.ContainsKey(key.ToString()))
                    {
                        return _memoryStore[key.ToString()];
                    }
                }

                switch (options.DateBehaviour)
                {
                    case RFDateBehaviour.NotSet:
                    case RFDateBehaviour.Exact:
                        {
                            var item = _catalog.LoadItem(key as RFCatalogKey, options.Version, options.IgnoreContent);
                            return (item != null && item.IsValid) ? item : null;
                        }

                    case RFDateBehaviour.Dateless:
                        {
                            var keyToLoad = key.CreateForInstance(new RFGraphInstance
                            {
                                Name = key.GraphInstance.Name,
                                ValueDate = null
                            });
                            var item = _catalog.LoadItem(keyToLoad, options.Version, options.IgnoreContent);
                            return (item != null && item.IsValid) ? item : null;
                        }
                    case RFDateBehaviour.Latest:
                    case RFDateBehaviour.Previous:
                        {
                            if (key.GraphInstance == null || !key.GraphInstance.ValueDate.HasValue)
                            {
                                throw new RFSystemException(this, "Unable to load latest date for key without date {0}", key);
                            }
                            var allKeys = _catalog.GetKeyInstances(key);
                            var candidateDates = new SortedSet<RFDate>();

                            foreach (var candidateKey in allKeys.Where(k => k.Key.Name == key.GraphInstance.Name))
                            {
                                if (candidateKey.Value.GraphInstance.ValueDate.Value <= key.GraphInstance.ValueDate.Value)
                                {
                                    if ((options.DateBehaviour == RFDateBehaviour.Latest) ||
                                        (options.DateBehaviour == RFDateBehaviour.Previous && candidateKey.Value.GraphInstance.ValueDate.Value < key.GraphInstance.ValueDate.Value))
                                    {
                                        candidateDates.Add(candidateKey.Value.GraphInstance.ValueDate.Value);
                                    }
                                }
                            }
                            if (candidateDates.Count == 0)
                            {
                                SystemLog.Warning(this, "No latest date instance item found for key {0}", key);
                                return null;
                            }
                            foreach (var latestDate in candidateDates.OrderByDescending(d => d))
                            {
                                var keyToLoad = key.CreateForInstance(new RFGraphInstance
                                {
                                    Name = key.GraphInstance.Name,
                                    ValueDate = latestDate
                                });
                                var item = _catalog.LoadItem(keyToLoad, options.Version, options.IgnoreContent);
                                if (item != null && item.IsValid)
                                {
                                    return item;
                                }
                            }
                            return null;
                        }
                    default:
                        throw new RFSystemException(this, "Unsupported date behaviour in LoadEntry: {0}", options.DateBehaviour);
                }
            }
            else
            {
                throw new RFSystemException(this, "Unknown store key type {0}", key.ToString());
            }
        }

        public void QueueInstruction(object raisedBy, RFInstruction instruction)
        {
            _instructions.QueueInstruction(raisedBy, instruction, ProcessingKey);
        }

        public void RaiseEvent(object raisedBy, RFEvent evt)
        {
            _events.RaiseEvent(raisedBy, evt, ProcessingKey);
        }

        public void SaveDocument(RFCatalogKey key, object content, bool raiseEvent = true)
        {
            SaveEntry(RFDocument.Create(key, content), raiseEvent);
        }

        public bool SaveEntry(RFCatalogEntry entry, bool raiseEvent = true, bool overwrite = false)
        {
            var materialUpdate = false;
            if (entry is RFDocument && !(entry as RFDocument).Type.Contains("."))
            {
                SystemLog.Warning(this, "Full type name required to save key {0}", entry.Key);
            }
            if (entry.Key.Plane == RFPlane.Ephemeral)
            {
                lock (_memoryStore)
                {
                    if (!_memoryStore.ContainsKey(entry.Key.ToString()))
                    {
                        _memoryStore.Add(entry.Key.ToString(), entry);
                    }
                    else
                    {
                        _memoryStore[entry.Key.ToString()] = entry;
                    }
                }
            }
            else
            {
                // don't raise events if not saved (i.e. same content)
                materialUpdate = _catalog.SaveItem(entry, overwrite);
                raiseEvent &= materialUpdate;
            }
            if (raiseEvent)
            {
                _events.RaiseEvent(this, new RFCatalogUpdateEvent
                {
                    Key = entry.Key
                }, ProcessingKey);
            }
            return materialUpdate;
        }

        public List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime, DateTime? endTime, int limitResults, RFDate? valueDate, bool latestOnly = false)
        {
            return _catalog.SearchKeys(t, startTime, endTime, limitResults, valueDate, latestOnly);
        }

        public RFProcessingTracker SubmitRequest(IEnumerable<RFCatalogEntry> inputs, IEnumerable<RFInstruction> instructions)
        {
            var copy = this.MemberwiseClone() as RFProcessingContext;
            copy.ProcessingKey = Guid.NewGuid().ToString();
            return copy.StartRequest(inputs, instructions);
        }

        protected static void DefaultOptions(ref RFCatalogOptions options)
        {
            if (options == null)
            {
                options = new RFCatalogOptions();
            }
        }

        protected RFProcessingTracker StartRequest(IEnumerable<RFCatalogEntry> inputs, IEnumerable<RFInstruction> instructions)
        {
            var tracker = _workQueue.PrepareRequest(ProcessingKey);
            foreach (var i in instructions ?? new List<RFInstruction>())
            {
                _instructions.QueueInstruction(this, i, ProcessingKey);
            }
            foreach (var input in inputs ?? new List<RFCatalogEntry>())
            {
                SaveEntry(input);
            }
            // TODO: find a better way to ensure request events are processed
            Thread.Sleep(1000); // we need to ensure events have been processed into instructions as otherwise it'll appear no work don
            _workQueue.BeginRequest(ProcessingKey);
            return tracker;
        }
    }
}
