// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
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
                UserConfig = new RFCachedUserConfig(dbConnection, environment),
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

            _workQueue = new RFGraphDispatchQueue(engine.GetWeights(), engine.GetDependencies(), engine.GetExclusiveProcesses(), _context);
            RFEnvironments.LogLicenseInfo(config);
        }

        public void Dispose()
        {
            _workQueue.Dispose();
        }

        public override IRFSystemContext Start()
        {
            var internalQueue = new RFDispatchQueueSink(_context, _workQueue);

            if (RFSettings.GetAppSetting("UseRabbitMQ", false))
            {
                _workQueueMonitor = (RFDispatchQueueMonitorBase)new RFDispatchQueueMonitorRabbitMQ(_context, internalQueue, internalQueue, _workQueue);
            }
            else
            {

#if !NETSTANDARD2_0
                var useMSMQ = RFSettings.GetAppSetting("UseMSMQ", true);
                _workQueueMonitor = useMSMQ ? (RFDispatchQueueMonitorBase)new RFDispatchQueueMonitorMSMQ(_context, internalQueue, internalQueue, _workQueue) : (RFDispatchQueueMonitorBase)new RFDispatchQueueMonitorInProc(_context, internalQueue, internalQueue, _workQueue);
#else
            _workQueueMonitor = (RFDispatchQueueMonitorBase)new RFDispatchQueueMonitorInProc(_context, internalQueue, internalQueue, _workQueue);
#endif
            }
            _workQueueMonitor.StartThread();

            _processingContext = _context.GetProcessingContext(null, internalQueue, internalQueue, _workQueueMonitor);
            _context.Engine.Initialize(_processingContext);
            return _processingContext; // this is the root environment (time triggers) which doesn't have a tracker
        }

        public override void Stop()
        {
            RFStatic.SetShutdown();
            _context.CancellationTokenSource.Cancel();
            _processingContext.RaiseEvent(this, new RFEvent { Timestamp = DateTime.Now });
            Thread.Sleep(1000);
            _workQueueMonitor.Shutdown();
            _processingContext.RaiseEvent(this, new RFEvent { Timestamp = DateTime.Now });
        }
    }
}
