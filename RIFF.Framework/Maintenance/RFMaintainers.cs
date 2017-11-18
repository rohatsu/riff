// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;

namespace RIFF.Framework
{
    public enum MaintenanceTriggers
    {
        DailyMaintenance
    }

    public static class RFMaintainers
    {
        public static void AddToEngine(RFEngineDefinition engineConfig, string connectionString)
        {
            var logMaintainer = engineConfig.AddProcess(
                processName: "Log Maintainer",
                description: "Maintains system log files",
                processor: () => new RFLogMaintainer(new RFLogMaintainer.Config
                {
                    ConnectionString = connectionString,
                    LogArchiveDirectory = RFSettings.GetAppSetting("RFMaintainers.LogArchiveDirectory", null),
                    LogDirectories = (RFSettings.GetAppSetting("RFMaintainers.LogDirectories", "")).Split(';'),
                    SystemLogRetentionDays = RFSettings.GetAppSetting("RFMaintainers.SystemLogRetentionDays", 7),
                    MaintainLogFiles = RFSettings.GetAppSetting("RFMaintainers.MaintainLogFiles", false),
                    MaintainSystemLog = RFSettings.GetAppSetting("RFMaintainers.MaintainSystemLog", false),
                    MaintainDispatchQueue = RFSettings.GetAppSetting("RFMaintainers.MaintainDispatchQueue", false)
                }));

            var catalogMaintainer = engineConfig.AddProcess(
                processName: "Catalog Maintainer",
                description: "Maintains data catalog",
                processor: () => new RFSQLCatalogMaintainer(new RFSQLCatalogMaintainer.Config
                {
                    ConnectionString = connectionString,
                    MaintainCatalog = RFSettings.GetAppSetting("RFMaintainers.MaintainCatalog", false)
                }));

            var databaseMaintainer = engineConfig.AddProcess(
                processName: "Database Maintainer",
                description: "Maintains database backups",
                processor: () => new RFDatabaseMaintainer(new RFDatabaseMaintainer.Config
                {
                    ConnectionString = connectionString,
                    MaintainDatabase = RFSettings.GetAppSetting("RFMaintainers.MaintainDatabase", false),
                    BackupDirectory = RFSettings.GetAppSetting("RFMaintainers.BackupDirectory", null),
                    WeeklyRotation = RFSettings.GetAppSetting("RFMaintainers.WeeklyRotation", false),
                    BackupPassword = RFSettings.GetAppSetting("RFMaintainers.BackupPassword", null),
                    WorkingDirectoryLocal = RFSettings.GetAppSetting("RFMaintainers.WorkingDirectoryLocal", null),
                    WorkingDirectoryUNC = RFSettings.GetAppSetting("RFMaintainers.WorkingDirectoryUNC", null),
                    OptimizeDatabase = RFSettings.GetAppSetting("RFMaintainers.OptimizeDatabase", false)
                }));

            var maintenanceTime = new TimeSpan(0, 15, 0);
            string maintenanceTimeConfig = RFSettings.GetAppSetting("RFMaintainers.MaintenanceTime", null);
            if (!string.IsNullOrWhiteSpace(maintenanceTimeConfig))
            {
                maintenanceTime = TimeSpan.ParseExact(maintenanceTimeConfig, @"hh\:mm", null);
            }

            engineConfig.AddScheduledTask("Daily Maintenance: Logs", new RFDailySchedule(maintenanceTime).Single(), RFWeeklyWindow.TueSat(), logMaintainer, true);
            engineConfig.AddChainedTask("Daily Maintenance: Catalog", logMaintainer, catalogMaintainer, true);
            engineConfig.AddChainedTask("Daily Maintenance: Backup", catalogMaintainer, databaseMaintainer, true);
        }
    }
}
