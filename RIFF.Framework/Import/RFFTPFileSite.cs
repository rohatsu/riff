// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Protocols;
using RIFF.Interfaces.Protocols.FTP;
using RIFF.Interfaces.Protocols.SFTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFFTPFileSite : RFFileSite, IDisposable
    {
        [DataMember]
        public string ConnectionKeyPassword { get; set; }

        [DataMember]
        public string ConnectionKeyPath { get; set; }

        [DataMember]
        public string Hostname { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public int? Port { get; set; }

        [DataMember]
        public string RootDirectory { get; set; }

        [DataMember]
        public RFFTPSiteType SiteType { get; set; }

        [DataMember]
        public string Username { get; set; }

        [IgnoreDataMember]
        protected static readonly string CONFIG_SECTION = "FTP Sites";

        [IgnoreDataMember]
        protected IFTPConnection _connection;

        public RFFTPFileSite(RFEnum siteKey, IRFUserConfig userConfig) : base(siteKey, CONFIG_SECTION, userConfig)
        { }

        public RFFTPFileSite OverrideRoot(string root = "")
        {
            RootDirectory = root;
            return this;
        }

        public static RFFTPFileSite ReadFromConfig(RFEnum siteKey, IRFUserConfig userConfig)
        {
            return new RFFTPFileSite(siteKey, userConfig)
            {
                SiteKey = siteKey,
                SiteType = (RFFTPSiteType)Enum.Parse(typeof(RFFTPSiteType), userConfig.GetString(CONFIG_SECTION, siteKey, true, "SiteType"), true),
                Hostname = userConfig.GetString(CONFIG_SECTION, siteKey, true, "Hostname"),
                Port = userConfig.GetInt(CONFIG_SECTION, siteKey, false, null, "Port"),
                Username = userConfig.GetString(CONFIG_SECTION, siteKey, true, "Username"),
                Password = userConfig.GetString(CONFIG_SECTION, siteKey, false, "Password"),
                ConnectionKeyPath = userConfig.GetString(CONFIG_SECTION, siteKey, false, "ConnectionKeyPath"),
                ConnectionKeyPassword = userConfig.GetString(CONFIG_SECTION, siteKey, false, "ConnectionKeyPassword"),
                RootDirectory = userConfig.GetString(CONFIG_SECTION, siteKey, false, "RootDirectory")
            };
        }

        public override void Cancel()
        {
            if (_connection != null)
            {
                _connection.Cancel();
            }
        }

        public override string CombinePath(string directoryName, string fileName)
        {
            return GetUnixDirectory(directoryName) + "/" + fileName;
        }

        public override void MoveFile(string sourcePath, string destinationPath)
        {
            _connection.MoveFile(sourcePath, destinationPath);
        }

        public override List<RFFileAvailableEvent> CheckSite(List<RFMonitoredFile> monitoredFiles)
        {
            var foundFiles = new List<RFFileAvailableEvent>();
            foreach (var file in monitoredFiles)
            {
                var directory = GetUnixDirectory(file.GetSubDirectory);
                if (!string.IsNullOrWhiteSpace(file.FileNameRegex))
                {
                    foreach (var candidate in _connection.ListFiles(directory, file.FileNameRegex, file.Recursive).OrderBy(f => f.ModifiedDate))
                    {
                        ProcessCandidate(file, candidate, ref foundFiles);
                    }
                }
                if (!string.IsNullOrWhiteSpace(file.FileNameWildcard))
                {
                    var regex = RFRegexHelpers.WildcardToRegex(file.FileNameWildcard);
                    foreach (var candidate in _connection.ListFiles(directory, regex, file.Recursive).OrderBy(f => f.ModifiedDate))
                    {
                        ProcessCandidate(file, candidate, ref foundFiles);
                    }
                }
            }
            return foundFiles;
        }

        public override void Close()
        {
            if (_connection != null)
            {
                _connection.Disconnect();
            }
        }

        public override void DeleteFile(RFFileAvailableEvent file)
        {
            _connection.DeleteFile(file.FileAttributes.FullPath);
        }

        public override byte[] GetFile(RFFileAvailableEvent availableFile)
        {
            var sw = Stopwatch.StartNew();
            var data = _connection.RetrieveFile(availableFile.FileAttributes.FullPath);
            if (data != null && sw.Elapsed.TotalSeconds > 0)
            {
                var kBytesPerSec = (data.Length / 1024.0) / sw.Elapsed.TotalSeconds;
                RFStatic.Log.Info(this, "Retrieved file {0} @ {1} kB/s", availableFile.FileAttributes.FileName, kBytesPerSec);
            }
            return data;
        }

        public override void Open(IRFProcessingContext context)
        {
            switch (SiteType)
            {
                case RFFTPSiteType.SFTP:
                    if (!string.IsNullOrWhiteSpace(ConnectionKeyPath))
                    {
                        if (Password.IsBlank())
                        {
                            _connection = new SFTPConnection(Hostname, Port, Username, ConnectionKeyPath, ConnectionKeyPassword);
                        }
                        else
                        {
                            _connection = new SFTPConnection(Hostname, Port, Username, Password, ConnectionKeyPath, ConnectionKeyPassword);
                        }
                    }
                    else
                    {
                        _connection = new SFTPConnection(Hostname, Port, Username, Password);
                    }
                    break;

                case RFFTPSiteType.FTP:
                    _connection = new FTPConnection(Hostname, Port, Username, Password);
                    break;

                default:
                    throw new RFSystemException(this, "Unknown FTP site type {0}", SiteType);
            }
        }

        public override void PutFile(RFFileAvailableEvent file, RFMonitoredFile fileConfig, byte[] data)
        {
            var directory = GetUnixDirectory(fileConfig.PutSubDirectory);
            if (UseTemporaryName)
            {
                var tmpFileName = file.FileAttributes.FileName + ".tmp";
                _connection.PutFile(directory, tmpFileName, data);
                _connection.MoveFile(
                    RFFileHelpers.GetUnixPath(directory, tmpFileName),
                    RFFileHelpers.GetUnixPath(directory, file.FileAttributes.FileName));
            }
            else
            {
                _connection.PutFile(directory, file.FileAttributes.FileName, data);
            }
        }

        protected string GetUnixDirectory(string subDirectory)
        {
            var directory = RootDirectory ?? "";
            if (!string.IsNullOrWhiteSpace(subDirectory))
            {
                directory += "/" + subDirectory; // Unix
            }
            return directory;
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
                    if (_connection != null)
                    {
                        if (_connection is FTPConnection)
                        {
                            ((FTPConnection)_connection).Dispose();
                        }
                        else if (_connection is SFTPConnection)
                        {
                            ((SFTPConnection)_connection).Dispose();
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~RFFTPFileSite() { // Do not change this code. Put cleanup code
        // in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}
