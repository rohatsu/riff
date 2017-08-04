// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.ServiceModel;

namespace RIFF.Framework
{
    public class RFServiceClient : IDisposable
    {
        public IRFService RFService { get; private set; }

        public RFServiceClient()
        {
            try
            {
                var binding = new System.ServiceModel.NetNamedPipeBinding("riffBinding");
                var uri = RFSettings.GetAppSetting("RFServiceUri");
                var endpoint = new System.ServiceModel.EndpointAddress(uri);
                var channelFactory = new ChannelFactory<IRFService>(binding, endpoint);
                RFService = channelFactory.CreateChannel();
            }
            catch
            {
                if (RFService != null)
                {
                    ((ICommunicationObject)RFService).Abort();
                }
                throw;
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
                    if (RFService != null)
                    {
                        ((ICommunicationObject)RFService).Close();
                        RFService = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~RFServiceClient() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}
