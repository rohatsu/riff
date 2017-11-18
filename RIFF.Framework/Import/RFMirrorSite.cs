// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
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
    public class RFLocalMirrorSite : RFLocalFileSite
    {
        private string _connectionString;
        private static readonly List<string> sArchives = new List<string> { ".zip" };

        public RFLocalMirrorSite(RFEnum siteKey, IRFUserConfig userConfig, string rootDirectory, string connectionString) : base(siteKey, userConfig, rootDirectory)
        {
            SiteKey = siteKey;
            RootDirectory = rootDirectory;
            _connectionString = connectionString;
        }

        public static string GetRootDirectory(RFEnum siteKey, IRFUserConfig userConfig)
        {
            return userConfig.GetString(CONFIG_SECTION, siteKey, true, "RootDirectory");
        }

        public static RFLocalMirrorSite ReadFromConfig(RFEnum siteKey, IRFUserConfig userConfig, string connectionString)
        {
            return new RFLocalMirrorSite(siteKey, userConfig, userConfig.GetString(CONFIG_SECTION, siteKey, true, "RootDirectory"), connectionString)
            {
                SiteKey = siteKey,
                PreserveModifiedDate = userConfig.GetBool(CONFIG_SECTION, siteKey, false, true, "PreserveModifiedDate"),
                CacheDirectoryList = true
            };
        }

        protected override string TrimRelativePath(string relativePath)
        {
            // remove the top directory (modified date) from relative path so files in specific subdirectories are found
            relativePath = base.TrimRelativePath(relativePath);
            if (relativePath.Length >= 10 && relativePath[4] == '-' && relativePath[7] == '-')
            {
                relativePath = relativePath.Substring(10);
            }
            return base.TrimRelativePath(relativePath);
        }

        public override void PutFile(RFFileAvailableEvent file, RFMonitoredFile fileConfig, byte[] data)
        {
            var fileName = file.FileAttributes.FileName;
            if (fileName.Contains('.'))
            {
                fileName = fileName.Substring(0, fileName.LastIndexOf('.')) + file.FileAttributes.ModifiedDate.ToString("_HHmmss") + fileName.Substring(fileName.LastIndexOf('.'));
            }
            else
            {
                fileName += file.FileAttributes.ModifiedDate.ToString("_HHmmsss");
            }

            // mirror path is \modified-date\source-site\relative-location\<org_filename>_HHmmss.<ext>
            var mirrorDirectory = Path.Combine(file.FileAttributes.ModifiedDate.ToString("yyyy-MM-dd"), file.SourceSite.ToString(), fileConfig.PutSubDirectory ?? String.Empty);
            var mirrorPath = Path.Combine(mirrorDirectory, fileName);

            PutMirroredFile(mirrorPath, file, data);
        }

        protected void PutMirroredFile(string mirrorPath, RFFileAvailableEvent file, byte[] data)
        {
            var physicalPath = Path.Combine(RootDirectory, mirrorPath); // file name could have subdirectories
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath));

            // if file exists and is the same, ignore
            if (File.Exists(physicalPath))
            {
                var existingFile = File.ReadAllBytes(physicalPath);
                if (Enumerable.SequenceEqual(existingFile, data))
                {
                    return;
                }
                // same name and modified date, but different content? perhaps bad transport - we will update so remove readonly flag
                File.SetAttributes(physicalPath, File.GetAttributes(physicalPath) & ~FileAttributes.ReadOnly);
            }

            InternalPutFile(physicalPath, file, data);
            File.SetAttributes(physicalPath, File.GetAttributes(physicalPath) | FileAttributes.ReadOnly);

            InternalPutMirroredFile(file.SourceSite, file.FileAttributes.FileSize, file.FileAttributes.FileName, file.FileAttributes.FullPath, file.FileAttributes.ModifiedDate,
                DateTime.Now, false, mirrorPath);
        }

        protected void InternalPutMirroredFile(string sourceSite, long fileSize, string fileName, string sourcePath, DateTime modifiedDate, DateTime receivedDate,
            bool isExtracted, string mirrorPath)
        {
            // write info to SQL
            using (var sqlConn = new SqlConnection(_connectionString))
            {
                sqlConn.Open();
                var sqlCommand = new SqlCommand("RIFF.PutMirroredFile", sqlConn);
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@SourceSite", sourceSite);
                sqlCommand.Parameters.AddWithValue("@FileSize", fileSize);
                sqlCommand.Parameters.AddWithValue("@FileName", fileName);
                sqlCommand.Parameters.AddWithValue("@SourcePath", sourcePath);
                sqlCommand.Parameters.AddWithValue("@ModifiedTime", modifiedDate);
                sqlCommand.Parameters.AddWithValue("@ReceivedTime", receivedDate);
                sqlCommand.Parameters.AddWithValue("@IsExtracted", isExtracted);
                sqlCommand.Parameters.AddWithValue("@MirrorPath", mirrorPath);

                sqlCommand.ExecuteNonQuery();
            }
        }

#if (false)
        public void Expand(RFMirrorSourceConfig config)
        {
            // get all files and check if they can be expanded
            var dataTable = new DataTable();
            using (var sqlConn = new SqlConnection(_connectionString))
            {
                sqlConn.Open();
                var getCommand = new SqlCommand("RIFF.GetMirroredFile", sqlConn);
                using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                {
                    dataTable.Load(reader);
                }
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var archiveName = row["FileName"].ToString();
                var isExtracted = (bool)row["IsExtracted"];
                if (!isExtracted && sArchives.Any(a => archiveName.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
                {
                    // it's an archive, try to extract it
                    var archivePath = row["MirrorPath"].ToString();
                    var archive = File.ReadAllBytes(Path.Combine(RootDirectory, archivePath));

                    using (var ms = new MemoryStream(archive))
                    {
                        foreach (var file in RIFF.Interfaces.Compression.ZIP.ZIPUtils.UnzipArchive(ms))
                        {
                            try
                            {
                                var mirrorDirectory = Path.Combine(Path.GetDirectoryName(archivePath), Path.GetFileNameWithoutExtension(archivePath));
                                Directory.CreateDirectory(mirrorDirectory);

                                var mirrorPath = Path.Combine(mirrorDirectory, file.Item1.FileName);

                                PutMirroredFile(
                                    row["SourceSite"].ToString(),
                                    file.Item1.FileSize,
                                    file.Item1.FileName,
                                    Path.Combine(row["SourcePath"].ToString(), file.Item1.FileName),
                                    (DateTime)row["ModifiedDate"],
                                    (DateTime)row["ReceivedDate"],
                                    false,
                                    mirrorPath);

                                // .zip does not contain timezone information
                                // so set file's update time to archive's
                                // update time
                                var newAttrs = new RFFileTrackedAttributes
                                {
                                    FileName = file.Item1.FileName,
                                    FileSize = file.Item1.FileSize,
                                    FullPath = file.Item1.FullPath,
                                    ModifiedDate = fileAttributes.ModifiedDate
                                };
                                if (ProcessFile(new RFFileAvailableEvent
                                {
                                    FileKey = candidateFile.FileKey,
                                    FileAttributes = newAttrs,
                                    SourceSite = mConfig.SourceSite.SiteKey
                                }, file.Item2, candidateFile, seenFiles))
                                {
                                    newFiles++;
                                }
                            }
                            catch (Exception ex)
                            {
                                //Log.UserError("Error extracting file {0} from archive {1}: {2}", file.Item1.FileName, availableFile.FileAttributes.FileName, ex.Message);
                            }
                        }
                    }
                }
            }
        }
#endif

    }
}
