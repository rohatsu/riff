// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Framework.Data;
using RIFF.Interfaces.Protocols;
using RIFF.Interfaces.Protocols.FTP;
using RIFF.Interfaces.Protocols.SFTP;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFMirrorSourceKey : RFCatalogKey
    {
        public RFMirrorSourceKey() : base()
        {

        }

        [DataMember]
        public string SourceName { get; set; }

        public static RFMirrorSourceKey Create(RFKeyDomain keyDomain, string sourceName)
        {
            return keyDomain.Associate(new RFMirrorSourceKey
            {
                GraphInstance = null,
                SourceName = sourceName,
                Plane = RFPlane.User,
                StoreType = RFStoreType.Document
            });
        }

        public override string FriendlyString()
        {
            return SourceName;
        }
    }

    [DataContract]
    public class RFMirrorSourceConfig
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<RFEnum> SiteEnums { get; set; }

        [DataMember]
        public List<string> Passwords { get; set; }

        [DataMember]
        public byte[] PGPKeyring { get; set; }

        [DataMember]
        public List<RFNamedFileConfig> NamedFiles { get; set; }

        [DataContract]
        public class RFNamedFileConfig
        {
            [DataMember]
            public string NamedFileConfigKey { get; set; }

            [DataMember]
            public string Description { get; set; }

            [DataMember]
            public RFEnum MonitoredFileEnum { get; set; }

            [DataMember]
            public List<string> Groups { get; set; }

            [DataMember]
            public int ValueDateStart { get; set; }

            [DataMember]
            public string ValueDateFormat { get; set; }

            [DataMember]
            public TimeSpan ExpectedTime { get; set; }

            [DataMember]
            public List<string> ExpectedDays { get; set; }
        }
    }

    public class RFMirrorActivity : RFActivity
    {
        private RFFrameworkDataContext _dc;
        private string _mirrorRoot;

        public RFMirrorActivity(IRFProcessingContext context, string userName) : base(context, userName)
        {
            _dc = new RFFrameworkDataContext(RFSettings.GetAppSetting("RFFramework.DataContext"));
            _mirrorRoot = RFLocalMirrorSite.GetRootDirectory(RFEnum.FromString("Mirror"), context.UserConfig);
        }

        public List<RFMirrorSourceConfig> GetMirrorSources()
        {
            return Context.GetKeysByType<RFMirrorSourceKey>().Select(k => Context.LoadDocumentContent<RFMirrorSourceConfig>(k.Value)).Where(s => s.IsEnabled).ToList();
        }

        public IQueryable<MirroredFile> GetFilesForSite(string sourceSite = null)
        {
            return _dc.MirroredFiles.Where(m => sourceSite == null || m.SourceSite == sourceSite);
        }

        public IEnumerable<string> GetSitesForSource(string sourceName)
        {
            var source = GetMirrorSources().FirstOrDefault(s => s.Name == sourceName);
            if (source != null)
            {
                return source.SiteEnums.Select(e => e.Enum).ToList();
            }
            return new List<string>();
        }

        public IQueryable<MirroredFile> GetFiles(IEnumerable<string> sourceSites)
        {
            return sourceSites != null ? _dc.MirroredFiles.Where(m => sourceSites.Contains(m.SourceSite)) : GetFilesForSite(null);
        }

        public (MirroredFile mirroredFile, byte[] content) GetFile(int mirroredFileID)
        {
            var mirroredFile = _dc.MirroredFiles.Single(m => m.MirroredFileID == mirroredFileID);
            return (mirroredFile, File.ReadAllBytes(Path.Combine(_mirrorRoot, mirroredFile.MirrorPath)));
        }

        public (MirroredFile mirroredFile, RFRawReport report) GetPreview(int mirroredFileID)
        {
            var file = GetFile(mirroredFileID);
            RFRawReport report = null;
            if(file.content != null && file.content.Length > 0)
            {
                try
                {
                    report = RFReportParserProcessor.LoadFromStream(new MemoryStream(file.content), null, null, null, null);
                    file.mirroredFile.Processed = true;
                    file.mirroredFile.NumRows = report.Sections.Sum(s => s.Rows.Count);
                }
                catch (Exception ex)
                {
                    file.mirroredFile.Processed = false;
                    file.mirroredFile.NumRows = null;
                    file.mirroredFile.Message = ex.Message;
                }
            }
            return (file.mirroredFile, report);
        }
    }
}
