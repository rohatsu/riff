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

        [DataMember]
        public string TaskName { get; set; }

        public virtual string Trigger
        {
            get
            {
                return null;
            }
        }

        public virtual RFSchedulerConfig SchedulerConfig(IRFProcessingContext context)
        {
            return new RFSchedulerConfig { IsEnabled = true };
        }
    }

    public interface IRFScheduledTaskDefinition : IRFTaskDefinition
    {
        //RFSchedulerConfig SchedulerConfig(IRFProcessingContext context);
    }

    public interface IRFTaskDefinition
    {
        string Description { get; }
        string GraphName { get; }
        bool IsSystem { get; }
        string ProcessName { get; }
        RFSchedulerConfig SchedulerConfig(IRFProcessingContext context);
        string TaskName { get; }
        string Trigger { get; }
    }
}
