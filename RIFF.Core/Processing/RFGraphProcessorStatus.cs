// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFGraphProcessorStatus
    {
        [DataMember]
        public bool CalculationOK { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTimeOffset Updated { get; set; }

        public RFGraphProcessorStatus()
        {
            CalculationOK = true;
            Message = null;
            Updated = DateTimeOffset.Now;
        }

        public void SetError(string message, params object[] param)
        {
            CalculationOK = false;
            Message = String.Format(message, param);
            Updated = DateTimeOffset.Now;
        }
    }
}
