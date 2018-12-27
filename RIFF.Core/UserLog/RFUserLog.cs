// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Text;

namespace RIFF.Core
{
    [DataContract]
    public class RFUserLogEntry
    {
        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public string Area { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public bool IsUserAction { get; set; }

        [DataMember]
        public bool IsWarning { get; set; }

        [DataMember]
        public long KeyReference { get; set; }

        [DataMember]
        public string KeyType { get; set; }

        [DataMember]
        public string Processor { get; set; }

        [DataMember]
        public DateTimeOffset Timestamp { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public RFDate ValueDate { get; set; }
    }

    internal class RFSQLUserLog : IRFUserLog
    {
        protected string _connectionString;
        protected RFComponentContext _context;

        public RFSQLUserLog(RFComponentContext context)
        {
            _connectionString = context.SystemConfig.DocumentStoreConnectionString;
            _context = context;
        }

        public List<RFUserLogEntry> GetEntries(RFDate eventDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var entries = new List<RFUserLogEntry>();
                    connection.Open();
                    try
                    {
                        string getEntriesSQL = "SELECT * FROM [RIFF].[UserLog] WHERE [Timestamp] >= @EventDate AND [Timestamp] < DATEADD(d, 1, @EventDate) ORDER BY [Timestamp] DESC";
                        using (var getEntriesCommand = new SqlCommand(getEntriesSQL, connection))
                        {
                            getEntriesCommand.Parameters.AddWithValue("@EventDate", new DateTimeOffset(eventDate.Date));

                            using (var reader = getEntriesCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                            {
                                var dataTable = new DataTable();
                                dataTable.Load(reader);
                                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                                {
                                    foreach (DataRow dataRow in dataTable.Rows)
                                    {
                                        try
                                        {
                                            entries.Add(ExtractLogEntry(dataRow));
                                        }
                                        catch (Exception ex)
                                        {
                                            _context.Log.Exception(this, "Error reading user log", ex);
                                        }
                                    }
                                }
                            }
                        }
                        return entries;
                    }
                    catch (Exception ex)
                    {
                        _context.Log.Exception(this, "Error retrieving user log (inner)", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log.Exception(this, "Error retrieving user log (outer)", ex);
            }
            return null;
        }

        public List<RFUserLogEntry> GetEntriesForArea(string area, RFDate? valueDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var entries = new List<RFUserLogEntry>();
                    connection.Open();
                    try
                    {
                        string getEntriesSQL = "SELECT * FROM [RIFF].[UserLog] WHERE [Area] = @Area AND (([ValueDate] = @ValueDate AND [ValueDate] IS NOT NULL) OR @ValueDate IS NULL) ORDER BY [Timestamp] DESC";
                        using (var getEntriesCommand = new SqlCommand(getEntriesSQL, connection))
                        {
                            getEntriesCommand.Parameters.AddWithValue("@Area", area);
                            if (valueDate.HasValue)
                            {
                                getEntriesCommand.Parameters.AddWithValue("@ValueDate", valueDate.Value.Date);
                            }
                            else
                            {
                                getEntriesCommand.Parameters.AddWithValue("@ValueDate", DBNull.Value);
                            }

                            using (var reader = getEntriesCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                            {
                                var dataTable = new DataTable();
                                dataTable.Load(reader);
                                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                                {
                                    foreach (DataRow dataRow in dataTable.Rows)
                                    {
                                        try
                                        {
                                            entries.Add(ExtractLogEntry(dataRow));
                                        }
                                        catch (Exception ex)
                                        {
                                            _context.Log.Exception(this, "Error reading user log", ex);
                                        }
                                    }
                                }
                            }
                        }
                        return entries;
                    }
                    catch (Exception ex)
                    {
                        _context.Log.Exception(this, "Error retrieving user log (inner)", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log.Exception(this, "Error retrieving user log (outer)", ex);
            }
            return null;
        }

        public void LogEntry(RFUserLogEntry entry)
        {
            if (entry == null)
            {
                return;
            }
            if (!entry.Description.EndsWith(".", StringComparison.Ordinal))
            {
                entry.Description += "."; // settle once and for all
            }
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    try
                    {
                        string insertUserLogSQL = "INSERT INTO [RIFF].[UserLog] ( [Area], [Action], [Description], [Username], [Processor], [Timestamp], [KeyType], [KeyReference], [IsUserAction], [IsWarning], [ValueDate] ) VALUES ( @Area, @Action, @Description, @Username, @Processor, @Timestamp, @KeyType, @KeyReference, @IsUserAction, @IsWarning, @ValueDate )";
                        using (var insertUserLogCommand = new SqlCommand(insertUserLogSQL, connection))
                        {
                            insertUserLogCommand.Parameters.AddWithValue("@Area", RFStringHelpers.StringToSQL(entry.Area, false, 30, false));
                            insertUserLogCommand.Parameters.AddWithValue("@Action", RFStringHelpers.StringToSQL(entry.Action, false, 50, false));
                            insertUserLogCommand.Parameters.AddWithValue("@Description", RFStringHelpers.StringToSQL(entry.Description, false, 200, true));
                            insertUserLogCommand.Parameters.AddWithValue("@Username", RFStringHelpers.StringToSQL(entry.Username, true, 40, true));
                            insertUserLogCommand.Parameters.AddWithValue("@Processor", RFStringHelpers.StringToSQL(entry.Processor, true, 50, false));
                            insertUserLogCommand.Parameters.AddWithValue("@Timestamp", DateTimeOffset.Now);
                            insertUserLogCommand.Parameters.AddWithValue("@KeyType", RFStringHelpers.StringToSQL(entry.KeyType, true, 50, false));
                            if (entry.KeyReference > 0)
                            {
                                insertUserLogCommand.Parameters.AddWithValue("@KeyReference", entry.KeyReference);
                            }
                            else
                            {
                                insertUserLogCommand.Parameters.AddWithValue("@KeyReference", DBNull.Value);
                            }
                            if (entry.ValueDate != RFDate.NullDate)
                            {
                                insertUserLogCommand.Parameters.AddWithValue("@ValueDate", entry.ValueDate.Date);
                            }
                            else
                            {
                                insertUserLogCommand.Parameters.AddWithValue("@ValueDate", DBNull.Value);
                            }
                            insertUserLogCommand.Parameters.AddWithValue("@IsUserAction", entry.IsUserAction);
                            insertUserLogCommand.Parameters.AddWithValue("@IsWarning", entry.IsWarning);

                            var affected = insertUserLogCommand.ExecuteNonQuery();
                            if (affected != 1)
                            {
                                throw new RFSystemException(this, "Unable to log event in user log.");
                            }

                            if (entry.IsWarning)
                            {
                                try
                                {
                                    var emailTo = RFSettings.GetAppSetting("UserLogWarningsTo", null);
                                    if (!string.IsNullOrWhiteSpace(emailTo))
                                    {
                                        var emailFrom = RFSettings.GetAppSetting("SmtpSender", "riff@localhost");
                                        var systemName = RFSettings.GetAppSetting("SystemName", "RIFF System");

                                        var message = new MailMessage();
                                        message.From = new MailAddress(emailFrom, systemName);
                                        foreach (var toAddress in emailTo.Split(',', ';').Select(s => s.Trim())
                                            .Where(s => !string.IsNullOrWhiteSpace(s)).
                                            Select(s => new MailAddress(s)))
                                        {
                                            message.To.Add(toAddress);
                                        }

                                        var bodyBuilder = new StringBuilder();
                                        bodyBuilder.Append("<html><body style=\"font-family: 'Helvetica Neue', 'Segoe UI', Helvetica, Verdana, sans-serif; font-size: 10pt;\">");
                                        bodyBuilder.AppendFormat("<p>A Warning was raised in {0}:</p>", systemName);
                                        bodyBuilder.Append("<blockquote><table border=\"0\" cellpadding=\"2\" cellspacing=\"0\" style=\"font-family: 'Helvetica Neue', 'Segoe UI', Helvetica, Verdana, sans-serif; font-size: 10pt;\">");
                                        bodyBuilder.AppendFormat("<tr><td style=\"width: 100px;\">Message:</td><td style=\"font-weight: bold;\">{0}</td></tr>", System.Net.WebUtility.HtmlEncode(entry.Description));
                                        bodyBuilder.AppendFormat("<tr><td>Processor:</td><td>{0}</td></tr>", System.Net.WebUtility.HtmlEncode(entry.Processor ?? String.Empty));
                                        bodyBuilder.AppendFormat("<tr><td>Area:</td><td>{0}</td></tr>", System.Net.WebUtility.HtmlEncode(entry.Area));
                                        bodyBuilder.AppendFormat("<tr><td>Action:</td><td>{0}</td></tr>", System.Net.WebUtility.HtmlEncode(entry.Action));
                                        bodyBuilder.AppendFormat("<tr><td>Value Date:</td><td>{0}</td></tr>", entry.ValueDate == RFDate.NullDate ? String.Empty : entry.ValueDate.ToString("d MMM yyyy"));
                                        bodyBuilder.AppendFormat("<tr><td>Username:</td><td>{0}</td></tr>", System.Net.WebUtility.HtmlEncode(entry.Username ?? String.Empty));
                                        bodyBuilder.AppendFormat("<tr><td>Server:</td><td>{0}</td></tr>", Environment.MachineName);
                                        bodyBuilder.AppendFormat("<tr><td>Timestamp:</td><td>{0}</td></tr>", DateTimeOffset.Now.ToString("d MMM yyyy HH:mm:ss (zzz)"));
                                        bodyBuilder.Append("</table></blockquote></body></html>");

                                        var subject = String.Format("Warning: {0}", RFStringHelpers.Limit(entry.Description, 80)).Trim('.', '\r', '\n', ' ');
                                        if (subject.Contains('\r'))
                                        {
                                            subject = subject.Substring(0, subject.IndexOf('\r')).Trim('.', '\r', '\n', ' ') + " (+)";
                                        }
                                        if (subject.Contains('\n'))
                                        {
                                            subject = subject.Substring(0, subject.IndexOf('\n')).Trim('.', '\r', '\n', ' ') + " (+)";
                                        }
                                        message.Subject = subject;
                                        message.Body = bodyBuilder.ToString();
                                        message.IsBodyHtml = true;

                                        var smtp = new SmtpClient();
                                        smtp.Send(message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _context.Log.Warning(this, "Unable to send UserLog warning via email: {0}", ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.Log.Exception(this, "Error writing to user log (inner)", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log.Exception(this, "Error writing to user log (outer)", ex);
            }
        }

        protected static RFUserLogEntry ExtractLogEntry(DataRow dataRow)
        {
            return new RFUserLogEntry
            {
                Area = RFStringHelpers.StringFromSQL(dataRow["Area"]),
                Action = RFStringHelpers.StringFromSQL(dataRow["Action"]),
                Description = RFStringHelpers.StringFromSQL(dataRow["Description"]),
                Username = RFStringHelpers.StringFromSQL(dataRow["Username"]),
                Processor = RFStringHelpers.StringFromSQL(dataRow["Processor"]),
                Timestamp = (DateTimeOffset)dataRow["Timestamp"],
                ValueDate = dataRow["ValueDate"] == DBNull.Value ? RFDate.NullDate : new RFDate((DateTime)dataRow["ValueDate"]),
                KeyType = RFStringHelpers.StringFromSQL(dataRow["KeyType"]),
                KeyReference = dataRow["KeyReference"] == DBNull.Value ? 0 : (int)dataRow["KeyReference"],
                IsUserAction = (bool)dataRow["IsUserAction"],
                IsWarning = (bool)dataRow["IsWarning"]
            };
        }
    }

    public interface IRFUserLog
    {
        List<RFUserLogEntry> GetEntries(RFDate eventDate);

        List<RFUserLogEntry> GetEntriesForArea(string area, RFDate? valueDate);

        void LogEntry(RFUserLogEntry entry);
    }
}
