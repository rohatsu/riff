// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.FtpClient;
using System.Text.RegularExpressions;
using System.Threading;

namespace RIFF.Interfaces.Protocols.FTP
{
    public class FTPConnection : IFTPConnection, IDisposable
    {
        protected static readonly int DEFAULT_PORT = 21;
        protected FtpClient _client;

        public FTPConnection(string host, int? port, string username, string password, int timeout = 120, int retries = 5)
        {
            _client = new FtpClient();
            _client.Host = host;
            _client.Port = port ?? DEFAULT_PORT;
            _client.Credentials = new System.Net.NetworkCredential(username, password);
            _client.ConnectTimeout = timeout * 1000;
            _client.DataConnectionConnectTimeout = timeout * 1000;
            _client.DataConnectionType = FtpDataConnectionType.PASV;
            Connect(retries);
        }

        public void Cancel()
        { }

        public void DeleteFile(string fullPath)
        {
            _client.DeleteFile(fullPath);
        }

        public void Disconnect()
        {
            if (_client.IsConnected)
            {
                _client.Disconnect();
            }
        }

        public List<RFFileTrackedAttributes> ListFiles(string directory, string regexString = null, bool recursive = false)
        {
            if (!_client.IsConnected)
            {
                throw new RFSystemException(typeof(FTPConnection), "Not connected to FTP site!");
            }
            var files = new List<RFFileTrackedAttributes>();
            Regex regex = null;
            if (!string.IsNullOrWhiteSpace(regexString))
            {
                regex = new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            FindFiles(files, directory, regex, recursive);
            return files;
        }

        public void PutFile(string directory, string fileName, byte[] data)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _client.SetWorkingDirectory(directory);
            }
            using (var fileStream = _client.OpenWrite(fileName, FtpDataType.Binary))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            _client.Rename(sourcePath, destinationPath);
        }

        public byte[] RetrieveFile(string filePath)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var fileStream = _client.OpenRead(filePath, FtpDataType.Binary))
                {
                    fileStream.CopyTo(memoryStream);
                }
                return memoryStream.ToArray();
            }
        }

        protected void Connect(int retries)
        {
            int numTries = 0;
            while (!_client.IsConnected)
            {
                try
                {
                    _client.Connect();
                }
                catch (Exception ex)
                {
                    numTries++;
                    if (numTries < retries)
                    {
                        RFStatic.Log.Warning(typeof(FTPConnection), "Unable to connect to {0}: {1}, retrying..", _client.Host, ex.Message);
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        throw new RFTransientSystemException(typeof(FTPConnection), "Unable to connect to {0} after {1} retries, aborting ({2}).", _client.Host, numTries, ex.Message);
                    }
                }
            }
            RFStatic.Log.Info(typeof(FTPConnection), "Connected to {0} as user {1}", _client.Host, _client.Credentials.UserName);
        }

        protected void FindFiles(List<RFFileTrackedAttributes> files, string directory, Regex regex, bool recursive)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _client.SetWorkingDirectory(directory);
            }
            foreach (FtpListItem file in _client.GetListing())
            {
                if (file.Type == FtpFileSystemObjectType.Directory && recursive)
                {
                    if (file.Name != "." && file.Name != "..")
                    {
                        FindFiles(files, file.Name, regex, recursive);
                    }
                }
                else if ((regex == null || regex.IsMatch(file.Name)) && file.Type == FtpFileSystemObjectType.File)
                {
                    files.Add(new RFFileTrackedAttributes
                    {
                        FileName = file.Name,
                        FileSize = file.Size,
                        FullPath = file.FullName,
                        ModifiedDate = file.Modified
                    });
                }
            }
            _client.SetWorkingDirectory("..");
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
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                    _client = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~FTPConnection() { // Do not change this code. Put cleanup code
        // in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}
