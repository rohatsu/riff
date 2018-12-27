// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Threading;

namespace RIFF.Core
{
    internal class RFWebEnvironment : RFEnvironmentBase
    {
        public RFWebEnvironment(string dbConnection, string environment)
        {
            _context = new RFComponentContext
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Catalog = null,
                UserConfig = new RFCachedUserConfig(connectionString: dbConnection, environmentName: environment),
                UserRole = new RFSQLUserRole(dbConnection),
                SystemConfig = new RFSystemConfig
                {
                    ProcessingMode = RFProcessingMode.RFSinglePass,
                    Environment = environment,
                    IntervalLength = 60000,
                    DocumentStoreConnectionString = dbConnection
                }
            };

            RFStatic.Log = new RFLog4NetLog(dbConnection);

            // to do - replace these managers with remote triggers
            _context.Engine = null;
            _context.Catalog = new RFSQLCatalog(_context);
            _context.UserLog = new RFSQLUserLog(_context);
            _context.DispatchStore = new RFDispatchStoreSQL(_context);
        }

        public override IRFSystemContext Start()
        {
            return _context.GetProcessingContext(null, null, null, null); // web env not allowed to process
        }

        public override void Stop()
        {
            RFStatic.SetShutdown();
        }
    }
}
