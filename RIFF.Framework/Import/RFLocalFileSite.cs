// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace RIFF.Framework
{
    [DataContract]
    public class RFLocalFileSite : RFFileSite
    {
        // read full directory list once - for slow shared drives
        [DataMember]
        public bool CacheDirectoryList { get; set; }

        [DataMember]
        public bool PreserveModifiedDate { get; set; }

        [DataMember]
        public string RootDirectory { get; set; }

        [IgnoreDataMember]
        protected static readonly string CONFIG_SECTION = "File Sites";

        public RFLocalFileSite(RFEnum siteKey, IRFUserConfig userConfig, string rootDirectory) : base(siteKey, CONFIG_SECTION, userConfig)
        {
            SiteKey = siteKey;
            RootDirectory = rootDirectory;
        }

        public static RFLocalFileSite ReadFromConfig(RFEnum siteKey, IRFUserConfig userConfig)
        {
            return new RFLocalFileSite(siteKey, userConfig, userConfig.GetString(CONFIG_SECTION, siteKey, true, "RootDirectory"))
            {
                SiteKey = siteKey,
                PreserveModifiedDate = userConfig.GetBool(CONFIG_SECTION, siteKey, false, true, "PreserveModifiedDate"),
                CacheDirectoryList = userConfig.GetBool(CONFIG_SECTION, siteKey, false, false, "CacheDirectoryList")
            };
        }

        public override void Cancel()
        {
        }

        public override string CombinePath(string directoryName, string fileName)
        {
            return System.IO.Path.Combine(directoryName, fileName);
        }

        public override void MoveFile(string sourcePath, string destinationPath)
        {
            if(File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            File.Move(sourcePath, destinationPath);
        }

        public override List<RFFileAvailableEvent> CheckSite(List<RFMonitoredFile> monitoredFiles)
        {
            var foundFiles = new List<RFFileAvailableEvent>();
            List<FileInfo> cachedFiles = null;

            if (CacheDirectoryList)
            {
                // pre-read directory list once, rather than for each file (for slow network drivers
                // and recursive search)
                var directoryInfo = new DirectoryInfo(Path.Combine(RootDirectory, String.Empty));
                cachedFiles = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).ToList();
            }

            foreach (var file in monitoredFiles)
            {
                var directoryInfo = new DirectoryInfo(Path.Combine(RootDirectory, file.GetSubDirectory ?? String.Empty));
                var fileInfos = cachedFiles != null ? FilterForFile(cachedFiles, file) : directoryInfo.GetFiles(file.FileNameWildcard, file.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                foreach (var fileInfo in fileInfos.OrderBy(f => f.LastWriteTime))
                {
                    try
                    {
                        if (IsFileReadable(fileInfo))
                        {
                            ProcessCandidate(file, new RFFileTrackedAttributes
                            {
                                FileName = fileInfo.Name,
                                FullPath = fileInfo.FullName,
                                // Excel modifies files on opening, so for those track creation time
                                ModifiedDate = (fileInfo.Name.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) || fileInfo.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) ? fileInfo.CreationTimeUtc : fileInfo.LastWriteTimeUtc,
                                FileSize = fileInfo.Length
                            }, ref foundFiles);
                        }
                        else
                        {
                            RFStatic.Log.Info(this, "Skipping file {0} as it's used by another process", fileInfo.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        RFStatic.Log.Warning(this, "Skipping file {0}: {1}", fileInfo.FullName, ex.Message);
                    }
                }
            }
            return foundFiles;
        }

        public override void Close()
        {
        }

        public override void DeleteFile(RFFileAvailableEvent file)
        {
            File.Delete(file.FileAttributes.FullPath);
        }

        public override byte[] GetFile(RFFileAvailableEvent availableFile)
        {
            return File.ReadAllBytes(availableFile.FileAttributes.FullPath);
        }

        public override void Open(IRFProcessingContext context)
        {
        }

        protected void InternalPutFile(string path, RFFileAvailableEvent file, byte[] data)
        {
            File.WriteAllBytes(path, data);
            if (PreserveModifiedDate)
            {
                try
                {
                    File.SetLastWriteTimeUtc(path, file.FileAttributes.ModifiedDate);
                    File.SetCreationTimeUtc(path, file.FileAttributes.ModifiedDate);
                }
                catch (Exception ex)
                {
                    RFStatic.Log.Warning(this, "Unable to set modified date on file {0}: {1}", path, ex.Message);
                }
            }
        }

        public override void PutFile(RFFileAvailableEvent file, RFMonitoredFile fileConfig, byte[] data)
        {
            var directory = Path.Combine(RootDirectory, fileConfig.PutSubDirectory ?? String.Empty);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, file.FileAttributes.FileName); // file name could have subdirectories
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            InternalPutFile(path, file, data);
        }

        protected bool IsFileReadable(FileInfo file)
        {
            try
            {
                File.ReadAllBytes(file.FullName);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            //file is not locked
            return true;
        }

        private static string GetRelativePath(string rootPath, string subPath)
        {
            if (!string.IsNullOrWhiteSpace(rootPath) && !rootPath.EndsWith("\\", StringComparison.Ordinal))
            {
                rootPath = rootPath + "\\";
            }
            if (!string.IsNullOrWhiteSpace(subPath) && !subPath.EndsWith("\\", StringComparison.Ordinal))
            {
                subPath = subPath + "\\";
            }
            if (rootPath == subPath)
            {
                return String.Empty;
            }
            var path1 = new Uri(rootPath);
            Uri path2 = new Uri(subPath);
            Uri diff = path1.MakeRelativeUri(path2);
            return Uri.UnescapeDataString(diff.OriginalString).Replace('/', '\\');
        }

        protected virtual string TrimRelativePath(string relativePath)
        {
            return relativePath.Trim('/', '\\', '.');
        }

        private IEnumerable<FileInfo> FilterForFile(IEnumerable<FileInfo> allFiles, RFMonitoredFile file)
        {
            var expectedDirectory = (file.GetSubDirectory ?? "").ToLower().Trim('/', '\\', '.');

            var candidates = new List<FileInfo>();
            foreach (var candidateFile in allFiles)
            {
                if (FitsMask(candidateFile.Name, file.FileNameWildcard))
                {
                    // is it in required subdirectory?
                    var relativePath = TrimRelativePath(GetRelativePath(RootDirectory, candidateFile.DirectoryName).ToLower());

                    if (relativePath == expectedDirectory || (file.Recursive && relativePath.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase)))
                    {
                        candidates.Add(candidateFile);
                    }
                }
            }
            return candidates;
        }
    }
}
