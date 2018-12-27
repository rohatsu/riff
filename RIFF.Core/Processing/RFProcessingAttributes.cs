// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    public enum RFDateBehaviour
    {
        NotSet = 0,
        Dateless = 1,
        Exact = 2,
        Latest = 3,
        Previous = 4,
        Custom = 5,
        Content = 6, // TODO: implement support
        Range = 7
    }

    public enum RFIOBehaviour
    {
        NotSet = 0,
        Input = 1,
        Output = 2,
        State = 3
    }

    /// <summary>
    /// Obsolete - specify date behaviour when mapping inputs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RFDateBehaviourAttribute : Attribute
    {
        public Func<DateTime, DateTime> CustomFunc { get; set; }

        public RFDateBehaviour DateBehaviour { get; set; }

        public RFDateBehaviourAttribute(RFDateBehaviour dateBehaviour)
        {
            DateBehaviour = dateBehaviour;
            CustomFunc = null;
        }
    }

    /// <summary>
    /// Defines the input/output mapping requirements for a graph domain member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RFIOBehaviourAttribute : Attribute
    {
        public RFIOBehaviour IOBehaviour { get; set; }

        public bool IsMandatory { get; set; }

        public RFIOBehaviourAttribute(RFIOBehaviour ioBehaviour, bool isMandatory = true)
        {
            IOBehaviour = ioBehaviour;
            IsMandatory = isMandatory;
        }
    }

    public interface IRFContentDrivenDate
    {
        DateTime? ContentDate { get; }
    }

    public interface IRFContentDrivenName
    {
        string ContentName { get; }
    }
}
