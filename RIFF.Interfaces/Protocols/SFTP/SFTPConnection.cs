// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RIFF.Interfaces.Protocols.SFTP
{
    public class SFTPConnection : IFTPConnection, IDisposable
    {
        protected static readonly int DEFAULT_PORT = 22;
        protected SftpClient _client;
        protected volatile bool _isCancelling;
        protected bool _logRetries;

        public SFTPConnection(string host, int? port, string username, string password, int timeout = 120, int retries = 5, bool logRetries = true)
        {
            _client = new SftpClient(host, port ?? DEFAULT_PORT, username, password);
            _logRetries = logRetries;
            Connect(timeout, retries);
        }

        public SFTPConnection(string host, int? port, string username, string keyFile, string keyPassword, int timeout = 120, int retries = 5, bool logRetries = true)
        {
            var pkf = new PrivateKeyFile(keyFile, keyPassword);
            _client = new SftpClient(host, port ?? DEFAULT_PORT, username, new PrivateKeyFile[] { pkf });
            _logRetries = logRetries;
            Connect(timeout, retries);
        }

        public SFTPConnection(string host, int? port, string username, string password, string keyFile, string keyPassword, int timeout = 120, int retries = 5, bool logRetries = true)
        {
            var pkf = new PrivateKeyFile(keyFile, keyPassword);

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PasswordAuthenticationMethod(username, password));
            methods.Add(new PrivateKeyAuthenticationMethod(username, pkf));

            var con = new ConnectionInfo(host, port ?? DEFAULT_PORT, username, methods.ToArray());
            _client = new SftpClient(con);
            _logRetries = logRetries;
            Connect(timeout, retries);
        }

        public void Cancel()
        {
            _isCancelling = true;
        }

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

        public void MoveFile(string sourcePath, string destinationPath)
        {
            _client.RenameFile(sourcePath, destinationPath);
        }

        public List<RFFileTrackedAttributes> ListFiles(string directory, string regexString = null, bool recursive = false)
        {
            if (!_client.IsConnected)
            {
                throw new RFTransientSystemException(typeof(SFTPConnection), "Not connected to SFTP site!");
            }
            var files = new List<RFFileTrackedAttributes>();
            Regex regex = null;
            if (!string.IsNullOrWhiteSpace(regexString))
            {
                regex = new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            FindFiles(files, null, directory, regex, recursive);
            return files;
        }

        public void PutFile(string directory, string fileName, byte[] data)
        {
            var destinationPath = RFFileHelpers.GetUnixPath(directory, fileName);
            _client.UploadFile(new MemoryStream(data), destinationPath, true);
        }

        public byte[] RetrieveFile(string filePath)
        {
            var memoryStream = new MemoryStream();
            if (!_client.IsConnected)
            {
                throw new Exception("Not connected");
            }
            var result = _client.BeginDownloadFile(filePath, memoryStream);
            while (!result.IsCompleted)
            {
                Thread.Sleep(1000);
                if (_isCancelling)
                {
                    throw new RFTransientSystemException(this, "Cancelled when retrieving file {0}", filePath);
                }
            }
            _client.EndDownloadFile(result);

            return memoryStream.ToArray();
        }

        protected void Connect(int timeout, int retries)
        {
            int numTries = 0;
            _client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(timeout);
            _client.OperationTimeout = TimeSpan.FromSeconds(timeout);
            _client.KeepAliveInterval = TimeSpan.FromSeconds(60);
            while (!_client.IsConnected)
            {
                try
                {
                    _client.Connect();
                }
                catch(SshAuthenticationException ex)
                {
                    RFStatic.Log.Warning(typeof(SFTPConnection), "Unable to connect to {0}: authentication failed ({1}).", _client.ConnectionInfo.Host, ex.Message);
                    throw new RFTransientSystemException(typeof(SFTPConnection), "Unable to connect to {0}: authentication failed ({1}).", _client.ConnectionInfo.Host, ex.Message);
                }
                catch (Exception ex)
                {
                    numTries++;
                    if (numTries < retries)
                    {
                        if(_logRetries)
                            RFStatic.Log.Warning(typeof(SFTPConnection), "Unable to connect to {0}: {1}, retrying..", _client.ConnectionInfo.Host, ex.Message);
                        else
                            RFStatic.Log.Info(typeof(SFTPConnection), "Unable to connect to {0}: {1}, retrying..", _client.ConnectionInfo.Host, ex.Message);
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        throw new RFTransientSystemException(typeof(SFTPConnection), "Unable to connect to {0} after {1} retries, aborting ({2}).", _client.ConnectionInfo.Host, numTries, ex.Message);
                    }
                }
            }
            RFStatic.Log.Info(typeof(SFTPConnection), "Connected to {0} as user {1}", _client.ConnectionInfo.Host, _client.ConnectionInfo.Username);
        }

        protected void FindFiles(List<RFFileTrackedAttributes> files, string parentDirectory, string directory, Regex regex, bool recursive)
        {
            var searchedDirectory = string.IsNullOrWhiteSpace(parentDirectory) ? directory : (parentDirectory + '/' + directory);
            foreach (SftpFile file in _client.ListDirectory(searchedDirectory).ToList())
            {
                if (file.IsDirectory && recursive)
                {
                    if (file.Name != "." && file.Name != "..")
                    {
                        try
                        {
                            FindFiles(files, searchedDirectory, file.Name, regex, recursive);
                        }
                        catch (Exception ex)
                        {
                            RFStatic.Log.Exception(this, ex, "Error searching SFTP directory tree");
                        }
                    }
                }
                else if ((regex == null || regex.IsMatch(file.Name)) && !file.IsDirectory)
                {
                    files.Add(new RFFileTrackedAttributes
                    {
                        FileName = file.Name,
                        FileSize = file.Length,
                        FullPath = file.FullName,
                        ModifiedDate = file.LastWriteTimeUtc
                    });
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                    _client = null;
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
