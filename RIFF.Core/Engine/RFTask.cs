using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public abstract class RFTaskDefinition : IRFTaskDefinition
    {
        public virtual string Description
        {
            get
            {
                return null;
            }
        }

        public virtual string GraphName
        {
            get
            {
                return null;
            }
        }

        [DataMember]
        public bool IsSystem { get; set; }

        [DataMember]
        public abstract string ProcessName { get; }

        public virtual string SchedulerRange
        {
            get
            {
                return null;
            }
        }

        public virtual string SchedulerSchedule
        {
            get
            {
                return null;
            }
        }

        [DataMember]
        public string TaskName { get; set; }

        public virtual string Trigger
        {
            get
            {
                return null;
            }
        }
    }

    public interface IRFScheduledTaskDefinition : IRFTaskDefinition
    {
        Func<RFSchedulerRange> RangeFunc { get; }
        Func<List<RFSchedulerSchedule>> SchedulesFunc { get; }
    }

    public interface IRFTaskDefinition
    {
        string Description { get; }
        string GraphName { get; }
        bool IsSystem { get; }
        string ProcessName { get; }
        string SchedulerRange { get; }
        string SchedulerSchedule { get; }
        string TaskName { get; }
        string Trigger { get; }
    }
}
