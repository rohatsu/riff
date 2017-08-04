// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    public enum RFFTPSiteType
    {
        FTP = 1,
        SFTP = 2
    };

    [DataContract]
    public class RFFileAvailableEvent
    {
        [DataMember]
        public RFFileTrackedAttributes FileAttributes { get; set; }

        [DataMember]
        public RFEnum FileKey { get; set; }
    }

    [DataContract]
    public abstract class RFFileSite
    {
        public bool Enabled { get; set; }

        public decimal? MaxAge { get; set; }

        public string PGPKeyPassword { get; set; }

        public string PGPKeyPath { get; set; }

        public string PGPSuffixes { get; set; }

        public bool ScanArchives { get; set; }

        public RFEnum SiteKey { get; set; }

        public int? WriteCooldown { get; set; }

        public string ArchivePath { get; set; }

        protected RFFileSite(RFEnum siteKey, string configSection, IRFUserConfig userConfig)
        {
            SiteKey = siteKey;
            Enabled = true;
            MaxAge = null;

            if (userConfig != null)
            {
                MaxAge = userConfig.GetDecimal(configSection, siteKey, false, null, "MaxAge");
                Enabled = userConfig.GetBool(configSection, siteKey, false, true, "Enabled");
                PGPSuffixes = userConfig.GetString(configSection, siteKey, false, "PGPSuffixes");
                PGPKeyPath = userConfig.GetString(configSection, siteKey, false, "PGPKeyPath");
                PGPKeyPassword = userConfig.GetString(configSection, siteKey, false, "PGPKeyPassword");
                WriteCooldown = userConfig.GetInt(configSection, siteKey, false, null, "WriteCooldown");
                ScanArchives = userConfig.GetBool(configSection, siteKey, false, false, "ScanArchives");
                ArchivePath = userConfig.GetString(configSection, siteKey, false, "ArchivePath");
            }
        }

        public abstract void Cancel();

        public abstract List<RFFileAvailableEvent> CheckSite(List<RFMonitoredFile> monitoredFiles);

        public abstract void Close();

        public abstract void DeleteFile(RFFileAvailableEvent file);

        public abstract byte[] GetFile(RFFileAvailableEvent availableFile);

        public abstract void Open(IRFProcessingContext context);

        public abstract void PutFile(RFFileAvailableEvent file, RFMonitoredFile fileConfig, byte[] data);

        public abstract void MoveFile(string sourcePath, string destinationPath);

        public abstract string CombinePath(string directoryName, string fileName);

        public virtual void ArchiveFile(RFFileAvailableEvent availableFile)
        {
            if(ArchivePath.NotBlank())
            {
                var destinationPath = CombinePath(ArchivePath, availableFile.FileAttributes.FileName);
                MoveFile(availableFile.FileAttributes.FullPath, destinationPath);
            }
        }

        public override string ToString()
        {
            return SiteKey.ToString();
        }

        protected bool IsStillWrittenTo(RFFileTrackedAttributes candidate)
        {
            if (WriteCooldown.HasValue)
            {
                var now = DateTime.Now;
                var writtenAgo = (now - candidate.ModifiedDate).TotalSeconds;
                if (writtenAgo < WriteCooldown.Value)
                {
                    RFStatic.Log.Warning(this, "Ignoring file {0} as it's been written to {1} seconds ago.", candidate.FileName, writtenAgo);
                    return true;
                }
            }
            return false;
        }

        protected void ProcessCandidate(RFMonitoredFile file, RFFileTrackedAttributes candidate, ref List<RFFileAvailableEvent> foundFiles)
        {
            if (!IsStillWrittenTo(candidate))
            {
                foundFiles.Add(new RFFileAvailableEvent
                {
                    FileKey = file.FileKey,
                    FileAttributes = candidate
                });
            }
        }
    }
}
