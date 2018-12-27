// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Data.SqlClient;

namespace RIFF.Framework
{
    /// <summary>
    /// Removes older versions of documents from SQL catalog
    /// </summary>
    public class RFSQLCatalogMaintainer : RFEngineProcessorWithConfig<RFEngineProcessorParam, RFSQLCatalogMaintainer.Config>
    {
        public class Config : IRFEngineProcessorConfig
        {
            public string ConnectionString { get; set; }

            public bool MaintainCatalog { get; set; }
        }

        public RFSQLCatalogMaintainer(Config config) : base(config)
        {
        }

        public override TimeSpan MaxRuntime()
        {
            return TimeSpan.FromHours(1);
        }

        public override RFProcessingResult Process()
        {
            var result = new RFProcessingResult();
            if (!string.IsNullOrWhiteSpace(_config.ConnectionString) && _config.MaintainCatalog)
            {
                try
                {
                    using (var connection = new SqlConnection(_config.ConnectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            int rows1 = 0, rows2 = 0, rows3 = 0;
                            using (var command1 = new SqlCommand("DELETE FROM RIFF.CatalogDocument WHERE CatalogEntryID NOT IN ( SELECT CatalogEntryID FROM RIFF.DocumentLatestView WHERE IsValid = 1 )", connection, transaction))
                            {
                                rows1 = command1.ExecuteNonQuery();
                            }
                            using (var command2 = new SqlCommand("DELETE FROM RIFF.CatalogEntry WHERE CatalogEntryID NOT IN ( SELECT CatalogEntryID FROM RIFF.DocumentLatestView WHERE IsValid = 1 )", connection, transaction))
                            {
                                rows2 = command2.ExecuteNonQuery();
                            }
                            using (var command3 = new SqlCommand("DELETE FROM RIFF.CatalogKey WHERE CatalogKeyID NOT IN ( SELECT CatalogKeyID FROM RIFF.DocumentLatestView )", connection, transaction))
                            {
                                rows3 = command3.ExecuteNonQuery();
                            }
                            transaction.Commit();
                            Log.Info("Cleaned up {0} rows from CatalogDocument, {1} rows from CatalogEntry and {2} rows from CatalogKey.", rows1, rows2, rows3);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "Error maintaining SQL catalog.", ex);
                    result.IsError = true;
                    result.AddMessage(ex.Message);
                }
                result.WorkDone = true;
            }
            return result;
        }
    }
}
