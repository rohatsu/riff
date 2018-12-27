// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    /// <summary>
    /// Checks for running services and sends monitoring information to support dashboard
    /// </summary>
    public class RFSystemMonitor : RFEngineProcessorWithConfig<RFEngineProcessorParam, RFSystemMonitor.Config>
    {
        public class Config : IRFEngineProcessorConfig
        {
            public string AlertEmail { get; set; }

            public bool CheckServices { get; set; }

            public string ConnectionString { get; set; }

            public string DashboardURL { get; set; }

            public string Environment { get; set; }

            public string[] MonitoredServices { get; set; }

            public bool PublishToDashboard { get; set; }
            public RFCatalogKey StateKey { get; set; }
        }

        [DataContract]
        public class State
        {
            [DataMember]
            public DateTimeOffset LastReportTime { get; set; }
        }

        public RFSystemMonitor(Config config) : base(config)
        {
        }

        public override RFProcessingResult Process()
        {
            CheckServices();
            return new RFProcessingResult { WorkDone = PublishToDashboard() };
        }

        protected void CheckServices()
        {
            if (_config.CheckServices)
            {
                //
            }
        }

        protected bool PublishToDashboard()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_config.ConnectionString) && _config.PublishToDashboard)
                {
                    var stateItem = Context.LoadDocumentContent<State>(_config.StateKey);

                    var currentReportTime = DateTimeOffset.Now;
                    var previousReportTime = stateItem == null ? currentReportTime.AddMinutes(-5) : stateItem.LastReportTime;

                    // min 4 minutes, max 1 hour lookback
                    if (previousReportTime.AddMinutes(4) > currentReportTime)
                    {
                        return false;
                    }
                    if (previousReportTime.AddHours(1) < currentReportTime)
                    {
                        previousReportTime = currentReportTime.AddHours(-1);
                    }

                    var userLog = new List<object>();
                    var systemLog = new List<object>();

                    using (var connection = new SqlConnection(_config.ConnectionString))
                    {
                        connection.Open();

                        using (var getUserLogCommand = new SqlCommand("SELECT * FROM [RIFF].[UserLog] WITH(NOLOCK) WHERE [Timestamp] > @Timestamp", connection))
                        {
                            getUserLogCommand.Parameters.AddWithValue("@Timestamp", previousReportTime);
                            using (var reader = getUserLogCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                            {
                                var dataTable = new DataTable();
                                dataTable.Load(reader);
                                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                                {
                                    foreach (DataRow dataRow in dataTable.Rows)
                                    {
                                        try
                                        {
                                            if ((bool)dataRow["IsWarning"]) // only warnings
                                            {
                                                userLog.Add(new
                                                {
                                                    LogID = (long)dataRow["LogID"],
                                                    Timestamp = (DateTimeOffset)dataRow["Timestamp"],
                                                    Area = dataRow["Area"] == DBNull.Value ? String.Empty : dataRow["Area"].ToString(),
                                                    Action = dataRow["Action"] == DBNull.Value ? String.Empty : dataRow["Action"].ToString(),
                                                    Description = dataRow["Description"] == DBNull.Value ? String.Empty : dataRow["Description"].ToString(),
                                                    Username = dataRow["Username"] == DBNull.Value ? String.Empty : dataRow["Username"].ToString(),
                                                    Processor = dataRow["Processor"] == DBNull.Value ? String.Empty : dataRow["Processor"].ToString(),
                                                    ValueDate = dataRow["ValueDate"] == DBNull.Value ? null : (DateTime?)dataRow["ValueDate"],
                                                    IsUserAction = (bool)dataRow["IsUserAction"],
                                                    IsWarning = (bool)dataRow["IsWarning"]
                                                });
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.SystemError(ex, "Error reading user log entry {0}", dataRow["LogID"].ToString());
                                        }
                                    }
                                }
                            }
                        }

                        using (var getSystemLogCommand = new SqlCommand("SELECT * FROM [RIFF].[SystemLog] WITH(NOLOCK) WHERE [Timestamp] > @Timestamp AND [Level] <> 'INSTR'", connection))
                        {
                            getSystemLogCommand.Parameters.AddWithValue("@Timestamp", previousReportTime);
                            using (var reader = getSystemLogCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                            {
                                var dataTable = new DataTable();
                                dataTable.Load(reader);
                                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                                {
                                    foreach (DataRow dataRow in dataTable.Rows)
                                    {
                                        try
                                        {
                                            systemLog.Add(new
                                            {
                                                LogID = (long)dataRow["LogID"],
                                                Timestamp = (DateTimeOffset)dataRow["Timestamp"],
                                                Hostname = dataRow["Hostname"] == DBNull.Value ? String.Empty : dataRow["Hostname"].ToString(),
                                                Level = dataRow["Level"] == DBNull.Value ? String.Empty : dataRow["Level"].ToString(),
                                                Source = dataRow["Source"] == DBNull.Value ? String.Empty : dataRow["Source"].ToString(),
                                                Message = dataRow["Message"] == DBNull.Value ? String.Empty : dataRow["Message"].ToString(),
                                                Exception = dataRow["Exception"] == DBNull.Value ? String.Empty : dataRow["Exception"].ToString(),
                                                AppDomain = dataRow["AppDomain"] == DBNull.Value ? String.Empty : dataRow["AppDomain"].ToString()
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.SystemError(ex, "Error reading system log entry {0}", dataRow["LogID"].ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var process = System.Diagnostics.Process.GetCurrentProcess();

                    var ticks = Stopwatch.GetTimestamp();
                    var uptime = ((double)ticks) / Stopwatch.Frequency;

                    var supportInfo = new
                    {
                        Status = "OK",
                        Timestamp = currentReportTime,
                        Environment = _config.Environment,
                        Hostname = Environment.MachineName,
                        WorkingSet = Environment.WorkingSet,
                        StartTime = new DateTimeOffset(process.StartTime),
                        NumThreads = process.Threads.Count,
                        UptimeDays = TimeSpan.FromSeconds(uptime).TotalDays,
                        Version = RFCore.sVersion,
                        UserLog = userLog,
                        SystemLog = systemLog
                    };

                    using (var client = new WebClient())
                    {
                        var dataString = JsonConvert.SerializeObject(supportInfo);
                        client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                        client.UploadString(new Uri(_config.DashboardURL), "POST", dataString);
                    }

                    var totalRows = userLog.Count + systemLog.Count;
                    if (totalRows > 0)
                    {
                        Log.Info("Sent {0} rows of monitoring data to support dashboard at {1}", totalRows, _config.DashboardURL);
                    }
                    else
                    {
                        Log.Debug("Sent heartbeat to support dashboard at {0}", _config.DashboardURL);
                    }

                    Context.SaveDocument(_config.StateKey, new State
                    {
                        LastReportTime = currentReportTime
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "Error gathering monitoring information.");
            }
            return false;
        }
    }
}
