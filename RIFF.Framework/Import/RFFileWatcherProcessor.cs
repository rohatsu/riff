// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Encryption.PGP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace RIFF.Framework
{
    public enum RFInternalFileKey
    {
        ZIPArchive = 1
    };

    [DataContract]
    public class RFFileWatcherProcessor : RFGenericSingleInstanceProcessor
    {
        protected RFFileWatcherProcessorConfig mConfig;

        private const long ERROR_LOCK_VIOLATION = 0x21;
        private const long ERROR_SHARING_VIOLATION = 0x20;

        public RFFileWatcherProcessor(RFFileWatcherProcessorConfig config)
        {
            mConfig = config;

            if (mConfig.SourceSite.ScanArchives)
            {
                mConfig.MonitoredFiles.Add(new RFMonitoredFile
                {
                    FileKey = RFInternalFileKey.ZIPArchive,
                    FileNameWildcard = "*.zip",
                    RemoveExpired = false
                });
            }
        }

        public override void CancelProcessing()
        {
            mConfig.SourceSite.Cancel();
            mConfig.DestinationSite.Cancel();
        }

        public override RFProcessingResult Process()
        {
            var result = new RFProcessingResult();
            if (!mConfig.SourceSite.Enabled)
            {
                Log.Debug("Not checking site {0} as it's disabled.", mConfig.SourceSite.SiteKey);
                return result;
            }
            if (!mConfig.DestinationSite.Enabled)
            {
                Log.Debug("Not checking site {0} as it's disabled.", mConfig.DestinationSite.SiteKey);
                return result;
            }
            var stateKey = GenerateStateKey("SeenFiles");

            var seenFiles = new RFSeenFiles();
            var seenFilesEntry = Context.LoadEntry(stateKey);
            if (seenFilesEntry != null)
            {
                seenFiles = (seenFilesEntry as RFDocument).GetContent<RFSeenFiles>();
            }

            int newFiles = 0;
            var destinationOpen = false;
            Log.Info("Checking for files to move from {0} to {1}", mConfig.SourceSite, mConfig.DestinationSite);
            try
            {
                mConfig.SourceSite.Open(Context);                

                var availableFiles = mConfig.SourceSite.CheckSite(mConfig.MonitoredFiles);
                var utcNow = DateTime.UtcNow;
                var filesToRemove = new List<RFFileAvailableEvent>();
                foreach (var availableFile in availableFiles)
                {
                    if (RFSeenFiles.IsExpired(availableFile.FileAttributes, utcNow, mConfig.SourceSite.MaxAge))
                    {
                        var monitoredFile = mConfig.MonitoredFiles.FirstOrDefault(m => m.FileKey == availableFile.FileKey);
                        if (monitoredFile.RemoveExpired)
                        {
                            filesToRemove.Add(availableFile);
                        }
                        continue;
                    }
                    if (!HaveSeenFile(seenFiles, availableFile))
                    {
                        try
                        {
                            if (!IsCancelling)
                            {
                                Log.Info("Retrieving new file {0} from {1}", availableFile.FileAttributes.FileName, mConfig.SourceSite);

                                var data = mConfig.SourceSite.GetFile(availableFile);
                                var fileAttributes = availableFile.FileAttributes;
                                var monitoredFile = mConfig.MonitoredFiles.FirstOrDefault(m => m.FileKey == availableFile.FileKey);

                                if (monitoredFile.FileKey == RFInternalFileKey.ZIPArchive)
                                {
                                    // unpack and process each file if it matches monitored files
                                    Log.Info("Attempting to download and unpack archive {0}", fileAttributes.FileName);
                                    using (var ms = new MemoryStream(data))
                                    {
                                        foreach (var file in RIFF.Interfaces.Compression.ZIP.ZIPUtils.UnzipArchive(ms))
                                        {
                                            foreach (var candidateFile in mConfig.MonitoredFiles)
                                            {
                                                var isMatch = false;
                                                if (!string.IsNullOrWhiteSpace(candidateFile.FileNameRegex))
                                                {
                                                    var regex = new Regex(candidateFile.FileNameRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                                    isMatch |= regex.IsMatch(file.Item1.FileName);
                                                }
                                                if (!isMatch && !string.IsNullOrWhiteSpace(candidateFile.FileNameWildcard))
                                                {
                                                    var regex = new Regex(RFRegexHelpers.WildcardToRegex(candidateFile.FileNameWildcard), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                                    isMatch |= regex.IsMatch(file.Item1.FileName);
                                                }
                                                if (isMatch)
                                                {
                                                    try
                                                    {
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
                                                        if (!destinationOpen)
                                                        {
                                                            mConfig.DestinationSite.Open(Context);
                                                            destinationOpen = true;
                                                        }
                                                        if (ProcessFile(new RFFileAvailableEvent
                                                        {
                                                            FileKey = candidateFile.FileKey,
                                                            FileAttributes = newAttrs,
                                                            SourceSite = availableFile.SourceSite
                                                        }, file.Item2, candidateFile, seenFiles))
                                                        {
                                                            newFiles++;
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.UserError("Error extracting file {0} from archive {1}: {2}", file.Item1.FileName, availableFile.FileAttributes.FileName, ex.Message);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // add archive to seen files
                                    seenFiles.MarkSeenFile(monitoredFile.FileKey, fileAttributes);
                                }
                                else
                                {
                                    if (!destinationOpen)
                                    {
                                        mConfig.DestinationSite.Open(Context);
                                        destinationOpen = true;
                                    }

                                    if (ProcessFile(availableFile, data, monitoredFile, seenFiles))
                                    {
                                        newFiles++;
                                    }
                                }

                                // archive
                                try
                                {
                                    mConfig.SourceSite.ArchiveFile(availableFile);
                                } catch (Exception ex)
                                {
                                    Log.Warning("Unable to archive file {0}: {1}", availableFile.FileAttributes.FileName, ex.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorCode = ex.HResult & 0xFFFF;

                            if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                            {
                                Log.Info("Unable to download file {0}: {1}", availableFile.FileAttributes.FileName, ex.Message);
                            }
                            else
                            {
                                Log.UserError("Error downloading file {0}: {1}", availableFile.FileAttributes.FileName, ex.Message);
                            }
                        }
                    }
                }
                if (!IsCancelling)
                {
                    seenFiles.CleanUpMaxAge(utcNow, mConfig.SourceSite.MaxAge);
                }
                Context.SaveEntry(RFDocument.Create(stateKey, seenFiles));

                // try to remove
                if (!IsCancelling)
                {
                    foreach (var fileToRemove in filesToRemove)
                    {
                        try
                        {
                            mConfig.SourceSite.DeleteFile(fileToRemove);
                        }
                        catch (Exception ex)
                        {
                            Log.UserError("Error deleting file {0}: {1}", fileToRemove.FileAttributes.FullPath, ex.Message);
                        }
                    }
                    Log.Info("Finished checking for files on {0} - {1} new files of {2} total", mConfig.SourceSite, newFiles, availableFiles.Count);
                }
                else
                {
                    Log.Info("Interrupted when checking for files on {0} - {1} new files of {2} total", mConfig.SourceSite, newFiles, availableFiles.Count);
                }
            }
            catch (RFTransientSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.UserError("Error checking file site {0}: {1}", mConfig.SourceSite.SiteKey, ex.Message);
            }
            finally
            {
                try
                {
                    if (mConfig.SourceSite != null)
                    {
                        mConfig.SourceSite.Close();
                    }
                    if (mConfig.DestinationSite != null && destinationOpen)
                    {
                        mConfig.DestinationSite.Close();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Error closing site {0}: {1}", mConfig.SourceSite.SiteKey, ex.Message);
                }
            }
            result.WorkDone = newFiles > 0;
            return result;
        }

        protected static bool HaveSeenFile(RFSeenFiles seenFiles, RFFileAvailableEvent e)
        {
            return seenFiles.HaveSeenFile(e.FileKey, e.FileAttributes);
        }

        protected static void MarkSeenFile(RFSeenFiles seenFiles, RFFileAvailableEvent e)
        {
            seenFiles.MarkSeenFile(e.FileKey, e.FileAttributes);
        }

        protected byte[] DecryptFile(byte[] data, RFFileTrackedAttributes attributes, out string finalName)
        {
            finalName = attributes.FileName;
            if (!string.IsNullOrWhiteSpace(mConfig.SourceSite.PGPSuffixes))
            {
                try
                {
                    var pgpExtensions = mConfig.SourceSite.PGPSuffixes.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var matchedExtension = pgpExtensions.FirstOrDefault(e => attributes.FileName.ToLower().EndsWith(e, StringComparison.OrdinalIgnoreCase));
                    if (matchedExtension != null)
                    {
                        Log.Info("Attempting to PGP decrypt file {0} as it matched suffix {1}", finalName, matchedExtension);
                        var inputStream = new MemoryStream(data);
                        var outputStream = new MemoryStream();
                        PGPUtils.Decrypt(inputStream, mConfig.SourceSite.PGPKeyPath, mConfig.SourceSite.PGPKeyPassword).CopyTo(outputStream);
                        data = outputStream.ToArray();

                        finalName = finalName.Substring(0, finalName.Length - matchedExtension.Length);
                        finalName = finalName.TrimEnd('.');
                    }
                }
                catch (Exception ex)
                {
                    Log.UserError("Error PGP decrypting file {0}: {1}", attributes.FileName, ex.Message);
                }
            }
            return data;
        }

        private bool ProcessFile(RFFileAvailableEvent availableFile, byte[] data, RFMonitoredFile monitoredFile, RFSeenFiles seenFiles)
        {
            string finalName = null;
            data = DecryptFile(data, availableFile.FileAttributes, out finalName);
            finalName = monitoredFile.TransformName(finalName);

            var decryptedFile = new RFFileAvailableEvent
            {
                FileKey = availableFile.FileKey,
                FileAttributes = new RFFileTrackedAttributes
                {
                    FileName = finalName,
                    FileSize = data.Length,
                    FullPath = availableFile.FileAttributes.FullPath,
                    ModifiedDate = availableFile.FileAttributes.ModifiedDate
                },
                SourceSite = availableFile.SourceSite
            };
            Log.Info("Storing new file {0} to {1}", finalName, mConfig.DestinationSite);
            mConfig.DestinationSite.PutFile(decryptedFile, monitoredFile, data);

            // mark original encrypted version as seen
            MarkSeenFile(seenFiles, availableFile); // only if successful on both fronts

            if (mConfig.DestinationSite is RFDocumentFileSite || mConfig.DestinationSite is RFLocalFileSite)
            {
                Log.Action("Download", "File Transfer", RFDate.NullDate, "Downloaded file {0} from {1}", availableFile.FileAttributes.FileName, mConfig.SourceSite);
            }
            else if (mConfig.SourceSite is RFDocumentFileSite || mConfig.SourceSite is RFLocalFileSite)
            {
                Log.Action("Upload", "File Transfer", RFDate.NullDate, "Uploaded file {0} to {1}", availableFile.FileAttributes.FileName, mConfig.DestinationSite);
            }
            else
            {
                Log.Action("Copy", "File Transfer", RFDate.NullDate, "Copied file {0} from {1} to {2}", availableFile.FileAttributes.FileName, mConfig.SourceSite, mConfig.DestinationSite);
            }

            return true;
        }
    }

    [DataContract]
    public class RFFileWatcherProcessorConfig : IRFGraphProcessorConfig
    {
        public RFFileSite DestinationSite { get; set; }

        public List<RFMonitoredFile> MonitoredFiles { get; set; }

        public RFFileSite SourceSite { get; set; }
    }
}
