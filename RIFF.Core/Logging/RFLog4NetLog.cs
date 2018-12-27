// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

//[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace RIFF.Core
{
    internal class RFLog4NetLog : IRFLog
    {
        protected static bool _isSetup;

        protected string _connectionString;
        protected ILog _log;

        public RFLog4NetLog(string connectionString)
        {
            _connectionString = connectionString;

            Setup();
        }

        private static Level EffectiveLevel(Level level)
        {
            return level != Level.Debug && RFStatic.IsShutdown ? Level.Info : level;
        }

        public void Critical(object caller, string message)
        {
            Log(caller, Level.Critical, message);
        }

        public void Critical(object caller, string message, params object[] args)
        {
            Log(caller, Level.Critical, ((args?.Length ?? 0) == 0) ? message : String.Format(message, args ?? new object[0]));
        }

        public void Debug(object caller, string message)
        {
            Log(caller, Level.Debug, message);
        }

        public void Debug(object caller, string message, params object[] args)
        {
            Log(caller, Level.Debug, ((args?.Length ?? 0) == 0) ? message : String.Format(message, args ?? new object[0]));
        }

        public void Error(object caller, string message)
        {
            Log(caller, Level.Error, message);
        }

        public void Error(object caller, string message, params object[] args)
        {
            Log(caller, Level.Error, ((args?.Length ?? 0) == 0) ? message : String.Format(message, args ?? new object[0]));
        }

        public void Exception(object caller, string message, Exception ex)
        {
            Log(caller, Level.Error, message, ex);
        }

        public void Exception(object caller, Exception ex, string message, params object[] args)
        {
            Log(caller, Level.Error, ((args?.Length ?? 0) == 0) ? message : String.Format(message, args ?? new object[0]), ex);
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public List<RFLogEntry> GetLogs(RFDate? date = null, long logID = 0)
        {
            var logs = new List<RFLogEntry>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var getCommandSQL = "SELECT TOP 500 [LogID], [Timestamp], [Hostname], [Level], [Source], [Message], [Exception], [Thread], [Content], [AppDomain] FROM [RIFF].[SystemLog]";

                    if (date.HasValue)
                    {
                        DateTime valueDate = date.Value;
                        getCommandSQL = getCommandSQL + String.Format(" WHERE [Timestamp] >= '{0} 00:00:00' AND [Timestamp] < '{1}'", valueDate.ToString("yyyy-MM-dd"),
                            valueDate.AddDays(1).ToString("yyyy-MM-dd"));
                    }
                    else if (logID > 0)
                    {
                        getCommandSQL = getCommandSQL + String.Format(" WHERE [LogID] = {0}", logID.ToString());
                    }
                    getCommandSQL = getCommandSQL + " ORDER BY [Timestamp] DESC";

                    using (var getCommand = new SqlCommand(getCommandSQL, connection))
                    {
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                            {
                                foreach (DataRow dataRow in dataTable.Rows)
                                {
                                    try
                                    {
                                        logs.Add(new RFLogEntry
                                        {
                                            LogID = (long)dataRow["LogID"],
                                            Timestamp = (DateTimeOffset)dataRow["Timestamp"],
                                            Exception = dataRow["Exception"] != DBNull.Value ? dataRow["Exception"].ToString() : String.Empty,
                                            Hostname = dataRow["Hostname"] != DBNull.Value ? dataRow["Hostname"].ToString() : String.Empty,
                                            Level = dataRow["Level"] != DBNull.Value ? dataRow["Level"].ToString() : String.Empty,
                                            Source = dataRow["Source"] != DBNull.Value ? dataRow["Source"].ToString() : String.Empty,
                                            Message = dataRow["Message"] != DBNull.Value ? dataRow["Message"].ToString() : String.Empty,
                                            Thread = dataRow["Thread"] != DBNull.Value ? dataRow["Thread"].ToString() : String.Empty,
                                            Content = dataRow["Content"] != DBNull.Value ? dataRow["Content"].ToString() : String.Empty,
                                            AppDomain = dataRow["AppDomain"] != DBNull.Value ? dataRow["AppDomain"].ToString() : String.Empty
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Exception(this, "Error retrieving log entry", ex);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception(this, ex, "Error retrieving System Log");
            }
            return logs;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public List<RFProcessEntry> GetProcesses(RFDate? date = null, long logID = 0)
        {
            var logs = new List<RFProcessEntry>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var getCommandSQL = "SELECT TOP 500 [LogID],[Timestamp],[Hostname],[GraphName],[ProcessName],[Instance],[ValueDate],[IOTime],[ProcessingTime],[Success],[Message],[NumUpdates] FROM [RIFF].[ProcessLog]";

                    if (date.HasValue)
                    {
                        DateTime valueDate = date.Value;
                        getCommandSQL = getCommandSQL + String.Format(" WHERE [Timestamp] >= '{0} 00:00:00' AND [Timestamp] < '{1}'", valueDate.ToString("yyyy-MM-dd"),
                            valueDate.AddDays(1).ToString("yyyy-MM-dd"));
                    }
                    else if (logID > 0)
                    {
                        getCommandSQL = getCommandSQL + String.Format(" WHERE [LogID] = {0}", logID.ToString());
                    }
                    getCommandSQL = getCommandSQL + " ORDER BY [Timestamp] DESC";

                    using (var getCommand = new SqlCommand(getCommandSQL, connection))
                    {
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                            {
                                foreach (DataRow dataRow in dataTable.Rows)
                                {
                                    try
                                    {
                                        logs.Add(new RFProcessEntry
                                        {
                                            LogID = (long)dataRow["LogID"],
                                            Timestamp = (DateTimeOffset)dataRow["Timestamp"],
                                            Message = dataRow["Message"] != DBNull.Value ? dataRow["Message"].ToString() : String.Empty,
                                            GraphName = RFStringHelpers.StringFromSQL(dataRow["GraphName"]),
                                            ProcessName = RFStringHelpers.StringFromSQL(dataRow["ProcessName"]),
                                            GraphInstance = new RFGraphInstance
                                            {
                                                Name = RFStringHelpers.StringFromSQL(dataRow["Instance"]),
                                                ValueDate = dataRow["ValueDate"] != DBNull.Value ? (RFDate?)new RFDate((DateTime)dataRow["ValueDate"]) : null
                                            },
                                            IOTime = (int)dataRow["IOTime"],
                                            ProcessingTime = (int)dataRow["ProcessingTime"],
                                            NumUpdates = (int)dataRow["NumUpdates"],
                                            Success = (bool)dataRow["Success"]
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Exception(this, "Error retrieving process entry", ex);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception(this, ex, "Error retrieving Process Log");
            }
            return logs;
        }

        public void Info(object caller, string message)
        {
            Log(caller, Level.Info, message);
        }

        public void Info(object caller, string message, params object[] args)
        {
            Log(caller, Level.Info, ((args?.Length ?? 0) == 0) ? message : String.Format(message, args ?? new object[0]));
        }

        public void LogEvent(object caller, RFEvent e)
        {/*
            if (!(e is RFCatalogUpdateEvent) && !(e is RFIntervalEvent))
            {
                LogInSystemLog(caller, "EVENT", e);
            }*/
        }

        public void LogInstruction(object caller, RFInstruction i)
        {
            /*if (!(i is RFIntervalInstruction))
            {
                LogInSystemLog(caller, "INSTR", i);
            }*/
        }

        public void LogProcess(object caller, RFProcessEntry p)
        {
            if (p != null)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        using (var insertCommand = new SqlCommand("INSERT INTO RIFF.ProcessLog( [Timestamp],[Hostname],[GraphName],[ProcessName],[Instance],[ValueDate],[IOTime],[ProcessingTime],[Success],[Message],[NumUpdates] ) VALUES " +
                            "( @Timestamp,@Hostname,@GraphName,@ProcessName,@Instance,@ValueDate,@IOTime,@ProcessingTime,@Success,@Message,@NumUpdates )", connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Timestamp", new DateTimeOffset(DateTime.Now));
                            insertCommand.Parameters.AddWithValue("@Hostname", RFStringHelpers.StringToSQL(Environment.MachineName.ToLower(), false, 50, false));
                            insertCommand.Parameters.AddWithValue("@GraphName", RFStringHelpers.StringToSQL(p.GraphName, true, 50, false));
                            insertCommand.Parameters.AddWithValue("@ProcessName", RFStringHelpers.StringToSQL(p.ProcessName, false, 100, false));
                            insertCommand.Parameters.AddWithValue("@Instance", RFStringHelpers.StringToSQL(p.GraphInstance?.Name, true, 30, false));
                            insertCommand.Parameters.AddWithValue("@ValueDate", p.GraphInstance?.ValueDate?.Date ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@IOTime", p.IOTime);
                            insertCommand.Parameters.AddWithValue("@ProcessingTime", p.ProcessingTime);
                            insertCommand.Parameters.AddWithValue("@Success", p.Success);
                            insertCommand.Parameters.AddWithValue("@Message", RFStringHelpers.StringToSQL(p.Message, true, 1014, true));
                            insertCommand.Parameters.AddWithValue("@NumUpdates", p.NumUpdates);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Exception(this, ex, "Error logging in Process Log");
                }
            }
        }

        public void Warning(object caller, string message)
        {
            Log(caller, Level.Warn, message);
        }

        public void Warning(object caller, string message, params object[] args)
        {
            Log(caller, Level.Warn, ((args?.Length ?? 0) == 0) ? message : String.Format(message, args ?? new object[0]));
        }

        protected void Log(object caller, Level level, string message, Exception ex = null)
        {
            log4net.NDC.Push(caller != null ? caller.ToString() : String.Empty);
            _log.Logger.Log(GetType(), EffectiveLevel(level), message, ex);
            log4net.NDC.Pop();
        }

        protected void LogInSystemLog(object caller, string level, object o)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var insertCommand = new SqlCommand("INSERT INTO RIFF.SystemLog( [Timestamp], [Hostname], [Level], [Source], [Message], [Content], [Thread], [AppDomain] ) VALUES " +
                        "( @Timestamp, @Hostname, @Level, @Source, @Message, @Content, @Thread, @AppDomain )", connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Timestamp", new DateTimeOffset(DateTime.Now));
                        insertCommand.Parameters.AddWithValue("@Hostname", Environment.MachineName.ToLower());
                        insertCommand.Parameters.AddWithValue("@Level", level);
                        insertCommand.Parameters.AddWithValue("@Source", caller != null ? caller.ToString() : String.Empty);
                        insertCommand.Parameters.AddWithValue("@Message", o.GetType().Name);
                        insertCommand.Parameters.AddWithValue("@Content", RFXMLSerializer.SerializeContract(o));
                        insertCommand.Parameters.AddWithValue("@Thread", System.Threading.Thread.CurrentThread.ManagedThreadId);
                        insertCommand.Parameters.AddWithValue("@AppDomain", AppDomain.CurrentDomain.FriendlyName);

                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Exception(this, ex, "Error logging in System Log");
            }
        }

        protected void Setup()
        {
            if (!_isSetup)
            {
                // add database appender for warnings

                var appender = new AdoNetAppender
                {
                    BufferSize = 1,
                    ConnectionType = "System.Data.SqlClient.SqlConnection, System.Data, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    CommandType = System.Data.CommandType.Text,
                    ConnectionString = _connectionString,
                    CommandText = "INSERT INTO RIFF.[SystemLog] ( Timestamp, Hostname, Level, Source, Message, Exception, Thread, AppDomain ) VALUES ( @timestamp, @hostname, @level, @source, @message, @exception, @thread, @appdomain )",
                    Threshold = Level.Warn
                };

                var rlc = new RawLayoutConverter();

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@timestamp",
                    DbType = System.Data.DbType.String,
                    Size = 50,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%date{yyyy-MM-ddTHH:mm:ss.fffzzz}"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@hostname",
                    DbType = System.Data.DbType.String,
                    Size = 100,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%property{log4net:HostName}"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@level",
                    DbType = System.Data.DbType.String,
                    Size = 50,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%level"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@source",
                    DbType = System.Data.DbType.String,
                    Size = 255,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%property{NDC}"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@message",
                    DbType = System.Data.DbType.String,
                    Size = 4000,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%message"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@exception",
                    DbType = System.Data.DbType.String,
                    Size = 2000,
                    Layout = (IRawLayout)rlc.ConvertFrom(new ExceptionLayout())
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@thread",
                    DbType = System.Data.DbType.String,
                    Size = 50,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%thread"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@appdomain",
                    DbType = System.Data.DbType.String,
                    Size = 50,
                    Layout = (IRawLayout)rlc.ConvertFrom(new PatternLayout("%appdomain"))
                });

                appender.ActivateOptions();

                var hierarchy = (Hierarchy)LogManager.GetRepository();
                hierarchy.Root.AddAppender(appender);
                hierarchy.Configured = true;

                _isSetup = true;
            }
            _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
    }
}
