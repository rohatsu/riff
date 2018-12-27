// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    [Serializable]
    public class RFSystemException : Exception
    {
        public RFSystemException(object caller, string message) : base(String.Format("[{0}] {1}", (caller as Type)?.Name ?? caller.ToString(), message))
        {
            //RFStatic.Log.Error(caller ?? this, message);
        }

        public RFSystemException(object caller, string message, params object[] formats) : this(caller, String.Format(message, formats ?? new object[0]))
        {
            //RFStatic.Log.Error(caller ?? this, message, formats);
        }

        public RFSystemException(object caller, Exception innerException, string message) : base(String.Format("[{0}] {1}", (caller as Type)?.Name ?? caller.ToString(), message), innerException)
        {
            //RFStatic.Log.Exception(caller ?? this, message, innerException);
        }

        public RFSystemException(object caller, Exception innerException, string message, params object[] formats) : this(caller, String.Format(message, formats ?? new object[0]), innerException)
        {
            //RFStatic.Log.Exception(caller ?? this, innerException, message, formats);
        }
    }

    [Serializable]
    public class RFTransientSystemException : RFSystemException
    {
        public RFTransientSystemException(object caller, string message) : base(caller, message)
        {
            //RFStatic.Log.Error(caller ?? this, message);
        }

        public RFTransientSystemException(object caller, string message, params object[] formats) : this(caller, String.Format(message, formats ?? new object[0]))
        {
            //RFStatic.Log.Error(caller ?? this, message, formats);
        }

        public RFTransientSystemException(object caller, Exception innerException, string message) : base(caller, innerException, message)
        {
            //RFStatic.Log.Exception(caller ?? this, message, innerException);
        }

        public RFTransientSystemException(object caller, Exception innerException, string message, params object[] formats) : this(caller, String.Format(message, formats ?? new object[0]), innerException)
        {
            //RFStatic.Log.Exception(caller ?? this, innerException, message, formats);
        }
    }
}
