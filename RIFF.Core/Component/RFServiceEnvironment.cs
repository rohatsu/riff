// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Threading;

namespace RIFF.Core
{
    internal class RFServiceEnvironment : RFEnvironmentBase, IDisposable
    {
        protected RFProcessingContext _processingContext;
        protected IRFDispatchQueue _workQueue;
        protected RFDispatchQueueMonitorBase _workQueueMonitor;

        public RFServiceEnvironment(string environment, RFEngineDefinition config, string dbConnection)
        {
            _context = new RFComponentContext
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Catalog = null,
                UserConfig = new RFUserConfig(dbConnection, environment),
                SystemConfig = new RFSystemConfig
                {
                    Environment = environment,
                    IntervalLength = config.IntervalSeconds * 1000,
                    ProcessingMode = RFProcessingMode.RFContinuous,
                    DocumentStoreConnectionString = dbConnection,
                    Downtime = RFSettings.GetDowntime()
                }
            };

            RFStatic.Log = new RFLog4NetLog(dbConnection);

            var engine = new RFSimpleEngine(config, _context);
            _context.Engine = engine;
            _context.Catalog = new RFSQLCatalog(_context);
            _context.UserLog = new RFSQLUserLog(_context);
            _context.UserRole = new RFSQLUserRole(dbConnection);
            _context.DispatchStore = new RFDispatchStoreSQL(_context);

            _workQueue = new RFGraphDispatchQueue(engine.GetWeights(), engine.GetDependencies(), _context);
            RFEnvironments.LogLicenseInfo(config);
        }

        public void Dispose()
        {
            _workQueue.Dispose();
        }

        public override IRFSystemContext Start()
        {            
            var useMSMQ = RFSettings.GetAppSetting("UseMSMQ", true);
            var manager = new RFDispatchQueueSink(_context, _workQueue);
            _workQueueMonitor = useMSMQ ? (RFDispatchQueueMonitorBase)new RFDispatchQueueMonitorMSMQ(_context, manager, manager, _workQueue) : (RFDispatchQueueMonitorBase)new RFDispatchQueueMonitorInProc(_context, manager, manager, _workQueue);
            _workQueueMonitor.StartThread();

            _processingContext = _context.GetProcessingContext(null, manager, manager, _workQueueMonitor);
            _context.Engine.Initialize(_processingContext);
            return _processingContext; // this is the root environment (time triggers) which doesn't have a tracker
        }

        public override void Stop()
        {
            RFStatic.SetShutdown();
            _workQueueMonitor.Shutdown();
            _processingContext.RaiseEvent(this, new RFEvent { Timestamp = DateTime.Now });
        }
    }
}
