// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    public class RFLogEntry
    {
        public string AppDomain { get; set; }

        public string Content { get; set; }

        public string Exception { get; set; }

        public string Hostname { get; set; }

        public string Level { get; set; }

        public long LogID { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public string Thread { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }

    public class RFProcessEntry
    {
        public RFGraphInstance GraphInstance { get; set; }
        public string GraphName { get; set; }

        public long IOTime { get; set; }
        public long LogID { get; set; }
        public string Message { get; set; }
        public int NumUpdates { get; set; }
        public long ProcessingTime { get; set; }
        public string ProcessName { get; set; }
        public bool Success { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public interface IRFLog
    {
        void Critical(object caller, string message);

        void Critical(object caller, string message, params object[] args);

        void Debug(object caller, string message);

        void Debug(object caller, string message, params object[] args);

        void Error(object caller, string message);

        void Error(object caller, string message, params object[] args);

        void Exception(object caller, string message, Exception ex);

        void Exception(object caller, Exception ex, string message, params object[] args);

        List<RFLogEntry> GetLogs(RFDate? date = null, long logID = 0);

        List<RFProcessEntry> GetProcesses(RFDate? date = null, long logID = 0);

        void Info(object caller, string message);

        void Info(object caller, string message, params object[] args);

        void LogEvent(object caller, RFEvent e);

        void LogInstruction(object caller, RFInstruction i);

        void LogProcess(object caller, RFProcessEntry p);

        void Warning(object caller, string message);

        void Warning(object caller, string message, params object[] args);
    }
}
