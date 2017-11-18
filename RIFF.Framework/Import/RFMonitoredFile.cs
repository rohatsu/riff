// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFMonitoredFile
    {
        public RFEnum FileKey { get; set; }

        public string FileNameRegex { get; set; }

        public string FileNameWildcard { get; set; }

        public string GetSubDirectory { get; set; }

        public bool LatestOnly { get; set; }

        public Func<string, string> NameTransform { get; set; }

        public string PutSubDirectory { get; set; }

        public bool Recursive { get; set; }

        public bool RemoveExpired { get; set; }

        public string ContentPasswords { get; set; }

        [IgnoreDataMember]
        protected static readonly string CONFIG_SECTION = "Input Files";

        public static RFMonitoredFile ReadFromConfig(RFEnum fileKey, IRFUserConfig config, Func<string, string> nameTransform = null, bool throwIfFail = true)
        {
            try
            {
                var m = new RFMonitoredFile
                {
                    FileKey = fileKey,
                    FileNameWildcard = config.GetString(CONFIG_SECTION, fileKey, false, "FileNameWildcard"),
                    FileNameRegex = config.GetString(CONFIG_SECTION, fileKey, false, "FileNameRegex"),
                    GetSubDirectory = config.GetString(CONFIG_SECTION, fileKey, false, "GetSubDirectory") ?? config.GetString(CONFIG_SECTION, fileKey, false, "SubDirectory"),
                    PutSubDirectory = config.GetString(CONFIG_SECTION, fileKey, false, "PutSubDirectory"),
                    NameTransform = nameTransform,
                    Recursive = config.GetBool(CONFIG_SECTION, fileKey, false, false, "RecursiveSearch"),
                    RemoveExpired = config.GetBool(CONFIG_SECTION, fileKey, false, false, "RemoveExpired"),
                    LatestOnly = config.GetBool(CONFIG_SECTION, fileKey, false, false, "LatestOnly"),
                    ContentPasswords = config.GetString(CONFIG_SECTION, fileKey, false, "ContentPasswords"),
                };
                if (string.IsNullOrWhiteSpace(m.FileNameWildcard) && string.IsNullOrWhiteSpace(m.FileNameRegex) && throwIfFail)
                {
                    throw new RFSystemException(typeof(RFMonitoredFile), "Invalid monitored file {0} configuration: missing file name.", fileKey.ToString());
                }
                return m;
            }
            catch (Exception)
            {
                if (throwIfFail)
                {
                    throw;
                }
            }
            return null;
        }

        public string TransformName(string originalName)
        {
            try
            {
                if (NameTransform != null)
                {
                    return NameTransform(originalName);
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, ex, "Error transforming file name {0}", originalName);
            }
            return originalName;
        }
    }
}
