// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFGraphProcessorTrigger
    {
        [DataMember]
        public bool TriggerStatus { get; set; }

        [DataMember]
        public DateTimeOffset TriggerTime { get; set; }

        public static RFGraphProcessorTrigger CreateNew(bool status = true)
        {
            return new RFGraphProcessorTrigger
            {
                TriggerStatus = status,
                TriggerTime = DateTimeOffset.Now
            };
        }
    }
}
