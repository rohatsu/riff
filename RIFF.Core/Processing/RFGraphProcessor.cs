// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Dummy class when no configuration is required
    /// </summary>
    [DataContract]
    public class RFConfigless : IRFGraphProcessorConfig, IRFEngineProcessorConfig
    {
    }

    /// <summary>
    /// Base class for implementing all graph processes.
    /// </summary>
    /// <typeparam name="D">Type of the domain object this processor will operate on.</typeparam>
    [DataContract]
    public abstract class RFGraphProcessor<D> : RFGraphProcessorBase, IRFGraphProcessorInstance
        where D : RFGraphProcessorDomain, new()
    {
        [IgnoreDataMember]
        public RFProcessLog Log { get; private set; }

        [DataMember]
        public RFGraphProcessorStatus ProcessorStatus { get; private set; }

        [IgnoreDataMember]
        private bool _externalWorkDone;

        [DataMember]
        private string _processorID;

        protected RFGraphProcessor()
        {
            _processorID = RFComponentCounter.GetNextComponentID();
        }

        public RFGraphProcessorDomain CreateDomain()
        {
            return new D();
        }

        /// <summary>
        /// If returns false the calculation will not be called for this instance - use to limit
        /// instances to once per month etc.
        /// </summary>
        /// <returns></returns>
        public virtual bool HasInstance(RFGraphInstance instance)
        {
            return instance.ValueDate.Value.IsWeekday();
        }

        /// <summary>
        /// Use this method to intialize instance of a processor within another processor's body
        /// </summary>
        /// <param name="parent"></param>
        public void Initialize(RFGraphProcessorBase parent)
        {
            Initialize(parent._context);
        }

        public void Initialize(IRFProcessingContext context)
        {
            _context = context;
            Log = new RFProcessLog(_context?.SystemLog ?? RFStatic.Log, _context?.UserLog, this);
        }

        /// <summary>
        /// Returns the latest instance that should be attempted to run.
        /// </summary>
        /// <returns></returns>
        public virtual RFDate MaxInstance(RFDate today)
        {
            return today;
        }

        /// <summary>
        /// Main processing function - all inputs and state in the domain would have been loaded by
        /// the engine, and after calculation is finished state and outputs will be automatically
        /// saved to their mapped catalog entries.
        /// </summary>
        /// <param name="domain">Data domain object to work on.</param>
        public abstract void Process(D domain);

        public bool ProcessDomain(RFGraphProcessorDomain domain)
        {
            try
            {
                ProcessorStatus = new RFGraphProcessorStatus();
                Log.SetValueDate(domain.ValueDate);
                Process(domain as D);
            }
            catch (RFLogicException ex)
            {
                // partial results will NOT be saved
                ProcessorStatus.SetError(ex.Message);
            }
            catch
            {
                throw;
            }
            return _externalWorkDone;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", _processorID, GetType().Name);
        }

        protected void ExternalWorkDone()
        {
            _externalWorkDone = true;
        }
    }

    [DataContract]
    public abstract class RFGraphProcessorBase
    {
        [IgnoreDataMember]
        internal IRFProcessingContext _context;
    }

    public interface IRFGraphProcessorConfig
    {
    }

    [DataContract]
    public abstract class RFGraphProcessorState
    {
    }

    [DataContract]
    public abstract class RFGraphProcessorWithConfig<D, C> : RFGraphProcessor<D> where D : RFGraphProcessorDomain, new() where C : IRFGraphProcessorConfig
    {
        protected C _config;

        protected RFGraphProcessorWithConfig(C config)
        {
            _config = config;
        }
    }

    /// <summary>
    /// Interface for graph processors.
    /// </summary>
    public interface IRFGraphProcessorInstance
    {
        RFProcessLog Log { get; }

        RFGraphProcessorStatus ProcessorStatus { get; }

        RFGraphProcessorDomain CreateDomain();

        bool HasInstance(RFGraphInstance instance);

        void Initialize(IRFProcessingContext context);

        RFDate MaxInstance(RFDate today);

        bool ProcessDomain(RFGraphProcessorDomain domain);
    }
}
