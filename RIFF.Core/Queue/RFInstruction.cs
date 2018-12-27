// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public abstract class RFInstruction : IComparable, IRFWorkQueueableItem
    {
        /*[DataMember]
        public RFEvent Event { get; protected set; }*/
        /*
        public static RFInstruction Create(/*RFEvent e)
        {
            return new RFInstruction { Event = e };
        }*/

        public int CompareTo(object obj)
        {
            // this is really slow
            return string.Compare(RFXMLSerializer.SerializeContract(this), RFXMLSerializer.SerializeContract(obj), StringComparison.Ordinal);
        }

        public virtual ItemType DispatchItemType()
        {
            return Core.ItemType.Instruction;
        }

        public virtual string DispatchKey()
        {
            return "GenericKey";
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public abstract RFEngineProcessorParam ExtractParam();

        public virtual bool ForceProcessLocally()
        {
            return false;
        }
    }

    /// <summary>
    /// Instruction that will be process locally for quick reaction
    /// </summary>
    [DataContract]
    public class RFIntervalInstruction : RFProcessInstruction
    {
        [DataMember]
        public RFInterval Interval { get; set; }

        public RFIntervalInstruction(string processName, RFIntervalEvent e) : base(processName)
        {
            Interval = e.Interval;
        }

        public override ItemType DispatchItemType()
        {
            return Core.ItemType.NotSupported;
        }

        public override string DispatchKey()
        {
            return "Interval";
        }

        public override RFEngineProcessorParam ExtractParam()
        {
            return new RFEngineProcessorIntervalParam(Interval);
        }

        public override bool ForceProcessLocally()
        {
            return true;
        }
    }

    /// <summary>
    /// Instruction carrying parameters (key, graph instance, custom class etc.)
    /// </summary>
    [DataContract]
    public class RFParamProcessInstruction : RFProcessInstruction
    {
        [DataMember]
        public RFEngineProcessorParam Param { get; private set; }

        public RFParamProcessInstruction(string processName, RFEngineProcessorParam param) : base(processName)
        {
            Param = param;
        }

        public override RFEngineProcessorParam ExtractParam()
        {
            return Param;
        }
    }

    /// <summary>
    /// Base class for instructions to run specific processes
    /// </summary>
    [DataContract]
    public abstract class RFProcessInstruction : RFInstruction
    {
        [DataMember]
        public string ProcessName { get; private set; }

        public RFProcessInstruction(/*RFEvent e, */string processName)
        {
            ProcessName = processName;
            //Event = e;
        }

        public override ItemType DispatchItemType()
        {
            return Core.ItemType.ProcessInstruction;
        }

        public override string DispatchKey()
        {
            return ToString();
        }

        public override string ToString()
        {
            return ProcessName;
        }
    }
}
