// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RIFF.Framework
{
    public class RFActivityContext : IRFActivityContext, IDisposable
    {
        public IRFUserConfig UserConfig
        {
            get { return _context.UserConfig; }
        }

        public string Environment
        {
            get { return _context.Environment; }
        }

        public IRFLog SystemLog
        {
            get { return _context.SystemLog; }
        }

        public IRFUserLog UserLog
        {
            get { return _context.UserLog; }
        }

        public IRFUserRole UserRole
        {
            get { return _context.UserRole; }
        }

        public RFDate Today => _context.Today;

        private IRFProcessingContext _context;
        private RFServiceClient _serviceClient;

        public RFActivityContext(IRFProcessingContext context)
        {
            _context = context;
        }

        public RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key)
        {
            return _context.GetKeyMetadata(key);
        }

        public Dictionary<long, T> GetKeysByType<T>() where T : RFCatalogKey
        {
            return _context.GetKeysByType<T>();
        }

        public Dictionary<long, RFCatalogKey> GetKeysByType(Type t)
        {
            return _context.GetKeysByType(t);
        }

        public RFProcessingTracker GetStatus(RFProcessingTrackerHandle trackerHandle)
        {
            return GetService().GetProcessStatus(trackerHandle);
        }

        public void Invalidate(RFCatalogKey key)
        {
            _context.Invalidate(key);
        }

        public T LoadDocumentContent<T>(RFCatalogKey key, RFCatalogOptions options = null) where T : class
        {
            return _context.LoadDocumentContent<T>(key, options);
        }

        public RFCatalogEntry LoadEntry(RFCatalogKey key, RFCatalogOptions options = null)
        {
            return _context.LoadEntry(key, options);
        }

        public RFProcessingTracker SaveDocument(RFCatalogKey key, object content, bool raiseEvent, RFUserLogEntry userLogEntry)
        {
            if(raiseEvent)
            {
                return SaveEntry(RFDocument.Create(key, content), userLogEntry);
            }
            else
            {
                SaveEntry(RFDocument.Create(key, content), false, userLogEntry);
                return new RFProcessingTracker("dummy");
            }
        }

        public RFProcessingTrackerHandle SaveDocumentAsync(RFCatalogKey key, object content, RFUserLogEntry userLogEntry)
        {
            return SaveEntryAsync(RFDocument.Create(key, content), userLogEntry);
        }

        public RFProcessingTrackerHandle SaveEntriesAsync(List<RFCatalogEntry> entries, RFUserLogEntry userLogEntry)
        {
            return SaveEntries(entries, true, userLogEntry);
        }

        public RFProcessingTracker SaveEntry(RFCatalogEntry entry, RFUserLogEntry userLogEntry)
        {
            var trackerCode = SaveEntry(entry, true, userLogEntry);
            RFProcessingTracker tracker = null;
            var sw = Stopwatch.StartNew();
            do
            {
                Thread.Sleep(250);
                tracker = GetService().GetProcessStatus(trackerCode);
            } while (!tracker.IsComplete && sw.ElapsedMilliseconds < 10000);
            return tracker;
        }

        public RFProcessingTrackerHandle SaveEntryAsync(RFCatalogEntry entry, RFUserLogEntry userLogEntry)
        {
            return SaveEntry(entry, true, userLogEntry);
        }

        public List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime = null, DateTime? endTime = null, int limitResults = 0, RFDate? valueDate = null, bool latestOnly = false)
        {
            return _context.SearchKeys(t, startTime, endTime, limitResults, valueDate, latestOnly);
        }

        protected IRFService GetService()
        {
            if (_serviceClient == null)
            {
                _serviceClient = new RFServiceClient();
            }
            return _serviceClient.RFService;
        }

        protected RFProcessingTrackerHandle SaveEntries(List<RFCatalogEntry> entries, bool raiseEvent, RFUserLogEntry userLogEntry) // raiseEvent = true
        {
            if (raiseEvent)
            {
                return GetService().SubmitAndProcess(entries.Select(e => new RFCatalogEntryDTO(e)), userLogEntry);
            }
            else
            {
                foreach (var entry in entries)
                {
                    _context.SaveEntry(entry, raiseEvent);
                }
                _context.UserLog.LogEntry(userLogEntry);
                return null;
            }
        }

        protected RFProcessingTrackerHandle SaveEntry(RFCatalogEntry entry, bool raiseEvent, RFUserLogEntry userLogEntry) // raiseEvent = true
        {
            if (raiseEvent)
            {
                return GetService().SubmitAndProcess(new List<RFCatalogEntryDTO> { new RFCatalogEntryDTO(entry) }, userLogEntry);
            }
            else
            {
                _context.SaveEntry(entry, raiseEvent);
                _context.UserLog.LogEntry(userLogEntry);
                return null;
            }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above. GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_serviceClient != null)
                    {
                        _serviceClient.Dispose();
                    }
                    _serviceClient = null;
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        public Dictionary<RFGraphInstance, RFCatalogKey> GetKeyInstances(RFCatalogKey key)
        {
            throw new NotImplementedException();
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~RFActivityContext() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }

    public interface IRFActivityContext : IRFReadingContext
    {
        RFProcessingTracker GetStatus(RFProcessingTrackerHandle trackerHandle);

        void Invalidate(RFCatalogKey key);

        RFProcessingTracker SaveDocument(RFCatalogKey key, object content, bool raiseEvent, RFUserLogEntry userLogEntry);

        RFProcessingTrackerHandle SaveDocumentAsync(RFCatalogKey key, object content, RFUserLogEntry userLogEntry);

        RFProcessingTrackerHandle SaveEntriesAsync(List<RFCatalogEntry> entries, RFUserLogEntry userLogEntry);

        RFProcessingTracker SaveEntry(RFCatalogEntry entry, RFUserLogEntry userLogEntry);

        RFProcessingTrackerHandle SaveEntryAsync(RFCatalogEntry entry, RFUserLogEntry userLogEntry);
    }
}
