// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Data domain for a graph processor. Use member attributes to denote input, state and output parameters.
    /// </summary>
    [DataContract]
    public abstract class RFGraphProcessorDomain
    {
        /// <summary>
        /// Instance of the graph under which the process is being executed.
        /// </summary>
        [DataMember]
        public RFGraphInstance Instance { get; set; }

        /// <summary>
        /// Optional and will be used by Graph Scheduled Tasks; override and set RFIOBehaviour to
        /// mandatory if necessary
        /// </summary>
        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.Input, false)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public virtual RFGraphProcessorTrigger Trigger { get; set; }

        [IgnoreDataMember]
        public RFDate ValueDate { get { return Instance.ValueDate.Value; } }

        protected RFGraphProcessorDomain()
        {
        }
    }
}
