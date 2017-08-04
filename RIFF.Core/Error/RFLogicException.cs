// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    [Serializable]
    public class RFLogicException : ApplicationException
    {
        public RFLogicException(object caller, string message) : base(message)
        {
            RFStatic.Log.Warning(caller ?? this, message);
        }

        public RFLogicException(object caller, IEnumerable<string> errors) : base(String.Join(Environment.NewLine, errors))
        {
            RFStatic.Log.Warning(caller ?? this, String.Join(Environment.NewLine, errors));
        }

        public RFLogicException(object caller, string message, params object[] formats) : base(String.Format(message, formats))
        {
            RFStatic.Log.Warning(caller ?? this, message, formats);
        }

        public RFLogicException(object caller, Exception innerException, string message) : base(message, innerException)
        {
            RFStatic.Log.Warning(caller ?? this, message, innerException);
        }

        public RFLogicException(object caller, Exception innerException, string message, params object[] formats) : base(String.Format(message, formats), innerException)
        {
            RFStatic.Log.Exception(caller ?? this, innerException, message, formats);
        }
    }
}
