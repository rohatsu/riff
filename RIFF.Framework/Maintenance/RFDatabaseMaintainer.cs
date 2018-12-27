// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Compression.ZIP;
using System;
using System.Data.SqlClient;
using System.IO;

namespace RIFF.Framework
{
    /// <summary>
    /// Backs up database
    /// </summary>
    public class RFDatabaseMaintainer : RFEngineProcessorWithConfig<RFEngineProcessorParam, RFDatabaseMaintainer.Config>
    {
        public class Config : IRFEngineProcessorConfig
        {
            public string BackupDirectory { get; set; }

            public string BackupPassword { get; set; }

            public string ConnectionString { get; set; }

            public bool MaintainDatabase { get; set; }

            public bool OptimizeDatabase { get; set; }

            public bool WeeklyRotation { get; set; }

            public string WorkingDirectoryLocal { get; set; }

            public string WorkingDirectoryUNC { get; set; }
        }

        public static readonly int COMMAND_TIMEOUT = 60 * 60; // 1 hr

        public RFDatabaseMaintainer(Config config) : base(config)
        {
        }

        public override TimeSpan MaxRuntime()
        {
            return TimeSpan.FromHours(2);
        }

        public override RFProcessingResult Process()
        {
            var step1 = BackupDatabase();
            var step2 = OptimizeDatabase();
            return new RFProcessingResult { WorkDone = step1 || step2 };
        }

        protected static void BackupDatabaseTo(SqlConnection connection, string databaseName, string backupPath)
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            using (var masterCommand = new SqlCommand("USE master", connection))
            {
                masterCommand.ExecuteNonQuery();
            }
            var backupSQL = "BACKUP DATABASE @dbname TO DISK = @filename WITH FORMAT, MEDIANAME = @dbname, NAME = @dbname";
            using (var backupCommand = new SqlCommand(backupSQL, connection))
            {
                backupCommand.Parameters.AddWithValue("@dbname", databaseName);
                backupCommand.Parameters.AddWithValue("@filename", backupPath);
                backupCommand.CommandTimeout = COMMAND_TIMEOUT; // 1 hr
                var rows = backupCommand.ExecuteNonQuery();
            }
        }

        protected bool BackupDatabase()
        {
            string databaseName = "n/a";
            string workingPathLocal = null;
            string workingPathUNC = null;
            string backupPath = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(_config.ConnectionString) && _config.MaintainDatabase && !string.IsNullOrWhiteSpace(_config.BackupDirectory))
                {
                    using (var connection = new SqlConnection(_config.ConnectionString))
                    {
                        connection.Open();

                        databaseName = connection.Database;
                        var backupFileName = databaseName;
                        if (_config.WeeklyRotation)
                        {
                            backupFileName = String.Format("{0}_{1}", backupFileName, DateTime.Today.DayOfWeek);
                        }
                        backupFileName += ".bak";

                        if (!string.IsNullOrWhiteSpace(_config.BackupPassword))
                        {
                            // save to working directory and encrypt
                            var workingDirectory = _config.WorkingDirectoryLocal ?? Path.GetTempPath();
                            workingPathLocal = Path.Combine(_config.WorkingDirectoryLocal, backupFileName);
                            workingPathUNC = Path.Combine(_config.WorkingDirectoryUNC, backupFileName);
                            BackupDatabaseTo(connection, databaseName, workingPathLocal);
                            ZIPUtils.ZipFile(_config.WorkingDirectoryUNC, backupFileName, _config.BackupDirectory, _config.BackupPassword);
                            File.Delete(workingPathUNC);
                            Log.Info("Backed up and encrypted database {0} to {1}.", databaseName, backupFileName);
                        }
                        else
                        {
                            // save directly to destination
                            backupPath = Path.Combine(_config.BackupDirectory, backupFileName);
                            BackupDatabaseTo(connection, databaseName, backupPath);
                            Log.Info("Backed up database {0} to {1}.", databaseName, backupFileName);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "Error backing up database {0} into {1}", databaseName, _config.BackupDirectory);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(workingPathLocal) && File.Exists(workingPathLocal))
                {
                    try
                    {
                        File.Delete(workingPathLocal);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Unable to remove local working file {0}: {1}", workingPathLocal, ex.Message);
                    }
                }
                if (!string.IsNullOrWhiteSpace(workingPathUNC) && File.Exists(workingPathUNC))
                {
                    try
                    {
                        File.Delete(workingPathUNC);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Unable to remove UNC working file {0}: {1}", workingPathLocal, ex.Message);
                    }
                }
            }
            return false;
        }

        protected bool OptimizeDatabase()
        {
            string databaseName = "n/a";
            try
            {
                if (!string.IsNullOrWhiteSpace(_config.ConnectionString) && _config.OptimizeDatabase)
                {
                    using (var connection = new SqlConnection(_config.ConnectionString))
                    {
                        connection.Open();
                        databaseName = connection.Database;

                        using (var optimizeCommand = new SqlCommand("RIFF.OptimizeIndices", connection))
                        {
                            optimizeCommand.CommandType = System.Data.CommandType.StoredProcedure;
                            optimizeCommand.CommandTimeout = COMMAND_TIMEOUT;
                            optimizeCommand.ExecuteNonQuery();
                        }

                        using (var truncateCommand = new SqlCommand("RIFF.TruncateDatabase", connection))
                        {
                            truncateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                            truncateCommand.CommandTimeout = COMMAND_TIMEOUT;
                            truncateCommand.Parameters.AddWithValue("@dbname", databaseName);
                            truncateCommand.Parameters.AddWithValue("@logname", databaseName + "_log");
                            truncateCommand.ExecuteNonQuery();
                        }
                    }
                    Log.Info("Optimized database {0}", databaseName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "Error optimizing database {0}.", databaseName);
            }
            return false;
        }
    }
}
