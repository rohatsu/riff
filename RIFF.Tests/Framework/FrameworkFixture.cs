// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using log4net.Config;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Xunit;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace RIFF.Tests
{
    public class FrameworkFixture : IDisposable
    {
        public RFEngineDefinition Config { get; }
        public string ConnString { get; }
        public IRFProcessingContext Context { get; }
        public EngineConfigElement Engine { get; }
        public IRFEnvironment Env { get; }
        public RFKeyDomain KeyDomain { get; }

        public void SeedDatabase()
        {
            Log("Connecting to database {0}", ConnString);
            var sql = new SqlConnection(ConnString);
            sql.Open();
            new SqlCommand("use master", sql).ExecuteNonQuery();
            new SqlCommand("ALTER DATABASE [RIFF_Tests] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", sql).ExecuteNonQuery();
            new SqlCommand("DROP DATABASE [RIFF_Tests]", sql).ExecuteNonQuery();
            new SqlCommand("CREATE DATABASE [RIFF_Tests]", sql).ExecuteNonQuery();
            new SqlCommand("use [RIFF_Tests]", sql).ExecuteNonQuery();

            var server = new Server(new ServerConnection(sql));

            foreach (var s in Directory.GetFiles(@"..\..\Database", "*.sql").OrderBy(f => f))
            {
                Log("Executing script {0}", Path.GetFileName(s));
                var script = File.ReadAllText(s);
                server.ConnectionContext.ExecuteNonQuery(script);
            }

            sql.Close();
            Log("Database initialized.");
        }

        public FrameworkFixture()
        {
            Log("Initializing unit tests.");

            log4net.GlobalContext.Properties["LogName"] = "Test";
            XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.config"));
            Engine = RIFFSection.GetDefaultEngine();
            Config = Engine.BuildEngineConfiguration();
            ConnString = Engine.Database;
            KeyDomain = Config.KeyDomain;

            SeedDatabase();

            Env = RFEnvironments.StartLocal("TEST", Config, Engine.Database, new List<string> { "RIFF.Tests.dll" });
            Context = Env.Start();
        }

        public static void Log(string text, params object[] formats)
        {
            System.Diagnostics.Trace.WriteLine(string.Format(text, formats));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Log("Shutting down.");
                    Env.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FrameworkFixture() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    [CollectionDefinition("Framework")]
    public class FrameworkCollection : ICollectionFixture<FrameworkFixture>
    {
    }
}
