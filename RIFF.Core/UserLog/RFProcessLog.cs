using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    public class RFProcessLog
    {
        private SortedSet<string> _errors;
        private object _parent;
        private IRFLog _systemLog;
        private IRFUserLog _userLog;
        private RFDate? _valueDate;

        public RFProcessLog(IRFLog systemLog, IRFUserLog userLog, object parent)
        {
            _systemLog = systemLog;
            _userLog = userLog;
            _parent = parent;
            _errors = new SortedSet<string>();
        }

        /// <summary>
        /// Informational message saved in User Log
        /// </summary>
        public void Action(string action, string area, RFDate? valueDate, string message, params object[] param)
        {
            var fullMessage = ((param?.Length ?? 0) == 0) ? message : String.Format(message, param ?? new object[0]);
            _userLog?.LogEntry(new RFUserLogEntry
            {
                Description = fullMessage,
                IsUserAction = false,
                IsWarning = false,
                Processor = _parent.GetType().Name,
                Action = action,
                Area = area,
                ValueDate = valueDate ?? (_valueDate ?? RFDate.NullDate),
                Username = "system"
            });
            _systemLog?.Info(_parent, "Action: {0}/{1}/{2}: {3}", action, area, valueDate, fullMessage);
        }

        /// <summary>
        /// Development message saved in System Log only
        /// </summary>
        public void Debug(string message, params object[] formats)
        {
            _systemLog?.Debug(_parent, message, formats ?? new object[0]);
        }

        public IEnumerable<string> GetErrors()
        {
            return _errors;
        }

        /// <summary>
        /// Informational message saved in System Log only
        /// </summary>
        public void Info(string message, params object[] formats)
        {
            _systemLog?.Info(_parent, message, formats ?? new object[0]);
        }

        public void SetValueDate(RFDate valueDate)
        {
            _valueDate = valueDate;
        }

        /// <summary>
        /// Error e-mailed to System Support
        /// </summary>
        public void SystemError(string message, params object[] formats)
        {
            var fullMessage = ((formats?.Length ?? 0) == 0) ? message : String.Format(message, formats);
            _systemLog?.Error(_parent, fullMessage);
            _errors.Add(fullMessage);
        }

        /// <summary>
        /// Error e-mailed to System Support
        /// </summary>
        public void SystemError(Exception ex, string message, params object[] formats)
        {
            var fullMessage = ((formats?.Length ?? 0) == 0) ? message : String.Format(message, formats);
            _systemLog?.Exception(_parent, ex, fullMessage);
            _errors.Add(fullMessage);
        }

        /// <summary>
        /// Notifies System Support only (processing stops)
        /// </summary>
        public Exception SystemException(string message, params object[] formats)
        {
            var fullMessage = ((formats?.Length ?? 0) == 0) ? message : String.Format(message, formats);
            _errors.Add(fullMessage);
            throw new RFSystemException(_parent, fullMessage);
        }

        /// <summary>
        /// Notifies System Support only (processing stops)
        /// </summary>
        public Exception SystemException(Exception ex, string message, params object[] formats)
        {
            var fullMessage = ((formats?.Length ?? 0) == 0) ? message : String.Format(message, formats);
            _errors.Add(fullMessage);
            throw new RFSystemException(_parent, ex, fullMessage);
        }

        /// <summary>
        /// Warning saved in User Log and e-mailed to User Support
        /// </summary>
        public void UserError(string message, params object[] formats)
        {
            var fullMessage = ((formats?.Length ?? 0) == 0) ? message : String.Format(message, formats);
            _systemLog?.Warning(_parent, fullMessage);
            _userLog?.LogEntry(new RFUserLogEntry
            {
                Description = fullMessage,
                IsUserAction = false,
                IsWarning = true,
                Processor = _parent.GetType().Name,
                Action = "Process",
                Area = String.Empty,
                ValueDate = _valueDate ?? RFDate.NullDate,
                Username = "system"
            });
            _errors.Add(fullMessage);
        }

        /// <summary>
        /// Saved in User Log, notifies User Support and System Support (processing stops)
        /// </summary>
        public Exception UserException(string message, params object[] formats)
        {
            var fullMessage = ((formats?.Length ?? 0) == 0) ? message : String.Format(message, formats);
            _errors.Add(fullMessage);
            throw new RFLogicException(_parent, fullMessage);
        }

        /// <summary>
        /// Informational message saved in System Log only
        /// </summary>
        public void Warning(string message, params object[] formats)
        {
            _systemLog?.Warning(_parent, message, formats ?? new object[0]);
        }
    }
}
