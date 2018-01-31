// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace RIFF.Core
{
    internal class RFConsoleEnvironment : RFEnvironmentBase, IDisposable
    {
        protected RFProcessingContext _localContext;
        protected RFDispatchQueueMonitorBase _queueMonitor;
        protected IRFDispatchQueue _workQueue;

        public RFConsoleEnvironment(string environment, RFEngineDefinition config, string dbConnection)
        {
            _context = new RFComponentContext
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Catalog = null,
                UserConfig = new RFUserConfig(dbConnection, environment),
                UserRole = new RFSQLUserRole(dbConnection),
                SystemConfig = new RFSystemConfig
                {
                    Environment = environment,
                    ProcessingMode = RFProcessingMode.RFSinglePass,
                    IntervalLength = config.IntervalSeconds,
                    DocumentStoreConnectionString = dbConnection
                }
            };

            if (RFStatic.Log == null)
            {
                RFStatic.Log = new RFLog4NetLog(dbConnection);
            }

            // reuse parent context for database-access, but create a new engine and work queue (for tracking)
            var engine = new RFSimpleEngine(config, _context);
            _context.Catalog = new RFSQLCatalog(_context);
            _context.ActiveComponents = new List<RFActiveComponent>();
            _context.Engine = engine;
            _context.UserLog = new RFSQLUserLog(_context);
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
            var manager = new RFDispatchQueueSink(_context, _workQueue);
            _queueMonitor = new RFDispatchQueueMonitorInProc(_context, manager, manager, _workQueue); // always use in-proc for console requests

            _localContext = _context.GetProcessingContext("console_" + Process.GetCurrentProcess().Id, manager, manager, _queueMonitor);
            _context.Engine.Initialize(_localContext);

            _queueMonitor.StartThread();

            return _localContext;
        }

        public override void Stop()
        {
            RFStatic.SetShutdown();
            _queueMonitor.Shutdown();
            _localContext.RaiseEvent(this, new RFEvent { Timestamp = DateTime.Now });
            _context.Shutdown();
        }
    }
}
