// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;

namespace RIFF.Framework
{
    public enum MonitoringTriggers
    {
        Heartbeat
    }

    public static class RFMonitors
    {
        public static void AddToEngine(RFEngineDefinition engineConfig, string connectionString, string environment)
        {
            var heartbeater = engineConfig.AddProcess(
                processName: "System Monitor",
                description: "Sends heartbeats to support dashboard",
                processor: () => new RFSystemMonitor(new RFSystemMonitor.Config
                {
                    ConnectionString = connectionString,
                    PublishToDashboard = RFSettings.GetAppSetting("RFMonitors.PublishToDashboard", false),
                    DashboardURL = RFSettings.GetAppSetting("RFMonitors.DashboardURL"),
                    Environment = environment,
                    StateKey = RFStateKey.CreateKey(engineConfig.KeyDomain, engineConfig.EngineName, "System Monitor", null)
                }));

            // scheduler to kick off monitoring
            var monitoringScheduler = engineConfig.AddProcess(
                processName: "Trigger Monitoring",
                description: "Timed trigger for system monitoring",
                processor: () => new RFSchedulerProcessor(new RFSchedulerConfig
                {
                    Schedules = new List<RFSchedulerSchedule> {
                        new RFIntervalSchedule(new TimeSpan(0, RFSettings.GetAppSetting("RFMonitors.PublishMinutes", 5), 0))
                    },
                    Range = RFWeeklyWindow.AllWeek(),
                    //IntervalKey = engineConfig.IntervalDocumentKey(),
                    TriggerKey = RFSchedulerTriggerKey.Create(engineConfig.KeyDomain, MonitoringTriggers.Heartbeat)
                }));
            engineConfig.AddIntervalTrigger(monitoringScheduler);

            engineConfig.AddCatalogUpdateTrigger<RFSchedulerTriggerKey>(t => t.TriggerName == MonitoringTriggers.Heartbeat, heartbeater);
        }
    }
}
