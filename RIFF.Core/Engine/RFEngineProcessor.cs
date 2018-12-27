// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Base class for writing engine processors (non-graph)
    /// </summary>
    /// <typeparam name="P">Type of parameter that processor will receive</typeparam>
    [DataContract]
    public abstract class RFEngineProcessor<P> : IRFEngineProcessor where P : RFEngineProcessorParam
    {
        /// <summary>
        /// Helper class for logging
        /// </summary>
        [IgnoreDataMember]
        public RFProcessLog Log { get; private set; }

        /// <summary>
        /// Context is used to access documents and events
        /// </summary>
        protected IRFProcessingContext Context { get; private set; }

        /// <summary>
        /// Execution parameters
        /// </summary>
        protected P InstanceParams { get; private set; }

        /// <summary>
        /// Current store's Key domain
        /// </summary>
        protected RFKeyDomain KeyDomain { get; private set; }

        /// <summary>
        /// Name of current process
        /// </summary>
        protected string ProcessName { get; private set; }

        private volatile bool _isCancelling;

        private RFProcessEntry _processEntry;

        public void Cancel()
        {
            _isCancelling = true;
            CancelProcessing();
        }

        public bool IsCancelling => _isCancelling;

        public virtual void CancelProcessing()
        {
        }

        public void SetProcessEntry(RFProcessEntry processEntry)
        {
            _processEntry = processEntry;
        }

        public RFProcessEntry GetProcessEntry()
        {
            return _processEntry;
        }

        /// <summary>
        /// Do not override or call base when you do
        /// </summary>
        public virtual void Initialize(RFEngineProcessorParam p, IRFProcessingContext context, RFKeyDomain keyDomain, string processName)
        {
            InstanceParams = p?.ConvertTo<P>();
            Context = context;
            KeyDomain = keyDomain;
            ProcessName = processName;
            _isCancelling = false;
            _processEntry = null;
            Log = new RFProcessLog(Context.SystemLog, context.UserLog, this);
        }

        /// <summary>
        /// Define the maximum time the processor can be executing before being aborted.
        /// </summary>
        /// <returns></returns>
        public virtual TimeSpan MaxRuntime()
        {
            return new TimeSpan(0);
        }

        /// <summary>
        /// Determines if the process can only run one instance at a time
        /// </summary>
        /// <returns></returns>
        public virtual bool IsExclusive => false;

        /// <summary>
        /// Main worker function
        /// </summary>
        public abstract RFProcessingResult Process();

        public override string ToString()
        {
            return String.Format("{0}:{1}", ProcessName, GetType().Name);
        }
    }

    public interface IRFEngineProcessorConfig
    {
    }

    /// <summary>
    /// Base class for writing engine processors (non-graph) with a configuration object
    /// </summary>
    /// <typeparam name="P">Type of parameter that processor will receive</typeparam>
    /// <typeparam name="C">Type of configuration object</typeparam>
    [DataContract]
    public abstract class RFEngineProcessorWithConfig<P, C> : RFEngineProcessor<P> where P : RFEngineProcessorParam where C : IRFEngineProcessorConfig
    {
        protected C _config;

        protected RFEngineProcessorWithConfig(C config)
        {
            _config = config;
        }
    }

    /// <summary>
    /// Simple class for a parameterless processor with default output and state keys
    /// </summary>
    [DataContract]
    public abstract class RFGenericSingleInstanceProcessor : RFEngineProcessor<RFEngineProcessorParam>
    {
        protected RFCatalogKey GenerateOutputKey(string name)
        {
            return KeyDomain.CreateDocumentKey(name, null, ProcessName, "output");
        }

        protected RFCatalogKey GenerateStateKey(string name)
        {
            return KeyDomain.CreateDocumentKey(name, null, ProcessName, "state");
        }
    }

    public interface IRFEngineProcessor
    {
        RFProcessLog Log { get; }

        void Cancel();

        RFProcessEntry GetProcessEntry();

        void Initialize(RFEngineProcessorParam p, IRFProcessingContext context, RFKeyDomain keyDomain, string processName);

        TimeSpan MaxRuntime();

        bool IsExclusive { get; }

        RFProcessingResult Process();
    }
}
