// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;

namespace RIFF.Framework
{
    public abstract class RFActivity : IRFActivity
    {
        public string LogArea { get { return GetType().Name; } }
        public string UserName { get { return _userName; } }
        protected IRFActivityContext Context { get { return _context; } }

        protected IRFLog Log { get { return Context.SystemLog; } }
        private IRFActivityContext _context;
        private string _userName;

        protected RFActivity(IRFProcessingContext context, string userName)
        {
            _context = new RFActivityContext(context);
            _userName = userName;
        }

        protected RFUserLogEntry CreateUserLogEntry(string action, string description, RFDate? valueDate)
        {
            return new RFUserLogEntry
            {
                Area = LogArea,
                Action = action,
                Description = description,
                IsUserAction = true,
                IsWarning = false,
                Username = _userName,
                ValueDate = valueDate.HasValue ? valueDate.Value : RFDate.NullDate
            };
        }

        protected DateTimeOffset? GetValidUpdateTime(RFCatalogKey key)
        {
            var stats = _context.GetKeyMetadata(key);
            return (stats != null && stats.IsValid) ? (DateTimeOffset?)stats.UpdateTime : null;
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
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~RFActivity() { // Do not change this code. Put cleanup code in
        // Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }

    public interface IRFActivity : IDisposable
    {
    }
}
