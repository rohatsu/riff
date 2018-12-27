// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace RIFF.Framework
{
    /// <summary>
    /// Compresses log files and purges from the database
    /// </summary>
    public class RFLogMaintainer : RFEngineProcessorWithConfig<RFEngineProcessorParam, RFLogMaintainer.Config>
    {
        public class Config : IRFEngineProcessorConfig
        {
            public string ConnectionString { get; set; }

            public string LogArchiveDirectory { get; set; }

            public string[] LogDirectories { get; set; }

            public bool MaintainDispatchQueue { get; set; }
            public bool MaintainLogFiles { get; set; }

            public bool MaintainSystemLog { get; set; }
            public int SystemLogRetentionDays { get; set; }
        }

        public RFLogMaintainer(Config config) : base(config)
        {
        }

        public override TimeSpan MaxRuntime()
        {
            return TimeSpan.FromHours(1);
        }

        public override RFProcessingResult Process()
        {
            var step1 = MaintainSystemLog();
            var step2 = MaintainLogFiles();
            var step3 = MaintainDispatchQueue();
            return new RFProcessingResult { WorkDone = step1 || step2 || step3 };
        }

        protected bool MaintainDispatchQueue()
        {
            if (!string.IsNullOrWhiteSpace(_config.ConnectionString) && _config.MaintainDispatchQueue)
            {
                try
                {
                    using (var connection = new SqlConnection(_config.ConnectionString))
                    {
                        connection.Open();
                        int rows = 0;
                        using (var command = new SqlCommand("DELETE FROM [RIFF].[DispatchQueue] WHERE [Environment] = @Environment AND [DispatchState] IN ( @FinishedState, @IgnoredState, @SkippedState )", connection))
                        {
                            command.Parameters.AddWithValue("@Environment", Context.Environment);
                            command.Parameters.AddWithValue("@FinishedState", (int)DispatchState.Finished);
                            command.Parameters.AddWithValue("@IgnoredState", (int)DispatchState.Ignored);
                            command.Parameters.AddWithValue("@SkippedState", (int)DispatchState.Skipped);
                            rows = command.ExecuteNonQuery();
                        }
                        Log.Info("Cleaned up {0} completed entries from DispatchQueue.", rows);
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "Error maintaining DispatchQueue.");
                }
            }
            return false;
        }

        protected bool MaintainLogFiles()
        {
            if (_config.LogDirectories != null && _config.LogDirectories.Count() > 0 && _config.MaintainLogFiles)
            {
                int filesArchived = 0;
                int filesDeleted = 0;
                var archiveFile = Path.Combine(_config.LogArchiveDirectory, String.Format("RIFFLogs_{0}_{1}.zip", DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"), Environment.MachineName));
                try
                {
                    var filesToRemove = new SortedSet<string>();

                    ZipArchive archive = null;
                    var memoryStream = new MemoryStream();
                    bool isNew = false;
                    if (File.Exists(archiveFile))
                    {
                        var existingFile = File.ReadAllBytes(archiveFile);
                        memoryStream.Write(existingFile, 0, existingFile.Length);
                        archive = new ZipArchive(memoryStream, ZipArchiveMode.Update, true);
                        isNew = false;
                    }
                    else
                    {
                        archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
                        isNew = true;
                    }
                    using (archive)
                    {
                        foreach (var logDirectory in _config.LogDirectories)
                        {
                            try
                            {
                                foreach (var file in Directory.GetFiles(logDirectory, "*.*", SearchOption.AllDirectories))
                                {
                                    try
                                    {
                                        // don't archive files written to today, one should use daily
                                        // log rotation
                                        if (File.GetLastWriteTime(file) >= DateTime.Today)
                                        {
                                            continue;
                                        }
                                        var originalFileName = Path.GetFileName(file);
                                        var archivedFileName = originalFileName;
                                        if (!isNew)
                                        {
                                            int n = 1;
                                            while (archive.Entries.Any(e => e.Name == archivedFileName))
                                            {
                                                archivedFileName = Path.GetFileNameWithoutExtension(originalFileName) + "." + n + Path.GetExtension(originalFileName);
                                                n++;
                                            }
                                        }
                                        archive.CreateEntryFromFile(file, archivedFileName);
                                        Log.Info("Archiving log file {0}", file);
                                        filesToRemove.Add(file);
                                        filesArchived++;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning("Unable to archive file {0}: {1}", file, ex.Message);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Unable to access directory {0}: {1}", logDirectory, ex.Message);
                            }
                        }
                    }

                    if (filesArchived > 0)
                    {
                        try
                        {
                            using (var fileStream = new FileStream(archiveFile, FileMode.Create))
                            {
                                memoryStream.Seek(0, SeekOrigin.Begin);
                                memoryStream.CopyTo(fileStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, "Error writing log archive {0}", archiveFile);
                        }
                    }
                    else
                    {
                        Log.Info("No log files to maintain.");
                    }

                    foreach (var fileToRemove in filesToRemove)
                    {
                        try
                        {
                            File.Delete(fileToRemove);
                            filesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Unable to delete file {0}: {1}", fileToRemove, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "Error maintaining log files.");
                }
                Log.Info("Log maintenance complete: {0} files archived into {1} and {2} files deleted.", filesArchived, archiveFile, filesDeleted);
                return true;
            }
            return false;
        }

        protected bool MaintainSystemLog()
        {
            if (!string.IsNullOrWhiteSpace(_config.ConnectionString) && _config.MaintainSystemLog)
            {
                try
                {
                    using (var connection = new SqlConnection(_config.ConnectionString))
                    {
                        connection.Open();
                        int rows = 0;
                        using (var command = new SqlCommand("DELETE FROM [RIFF].[SystemLog] WHERE [Timestamp] < getDate() - @LookBack", connection))
                        {
                            command.Parameters.AddWithValue("@LookBack", _config.SystemLogRetentionDays);
                            rows = command.ExecuteNonQuery();
                        }
                        Log.Info("Cleaned up {0} logs from SystemLog.", rows);
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "Error maintaining SystemLog.");
                }
            }
            return false;
        }
    }
}
