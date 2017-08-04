// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Formats.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace RIFF.Framework
{
    public class RFSQLReportRunner : RFEngineProcessorWithConfig<RFEngineProcessorParam, RFSQLReportRunner.Config>
    {
        public class Config : IRFEngineProcessorConfig
        {
            public string ConnectionString { get; set; }

            public string DestinationPath { get; set; }

            public bool Enabled { get; set; }

            public Func<string> FileNameFunc { get; set; }

            public Func<string> QueryFunc { get; set; }
        }

        public RFSQLReportRunner(Config config) : base(config)
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public override RFProcessingResult Process()
        {
            var result = new RFProcessingResult();
            if (!_config.Enabled)
            {
                return result;
            }
            try
            {
                int numRows = 0;
                var sqlQuery = _config.QueryFunc();
                var fileName = _config.FileNameFunc();
                var path = System.IO.Path.Combine(_config.DestinationPath, fileName);
                using (var connection = new SqlConnection(_config.ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        var knownColumns = new Dictionary<string, int>();
                        using (var queryCommand = new SqlCommand(sqlQuery, connection))
                        {
                            using (var reader = queryCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                            {
                                var dataTable = new DataTable();
                                dataTable.Load(reader);
                                if (dataTable != null && dataTable.Rows != null)
                                {
                                    System.IO.File.WriteAllText(
                                        path,
                                        CSVBuilder.FromDataTable(dataTable));
                                    numRows = dataTable.Rows.Count;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new RFSystemException(this, ex, "Error running SQL report for file {0}", fileName);
                    }
                }
                Context.SystemLog.Info(this, "Saved {0} rows from an SQL query into {1}", numRows, fileName);
            }
            catch (Exception ex)
            {
                throw new RFSystemException(this, ex, "Error running SQL report for path {0}", _config.DestinationPath);
            }
            result.WorkDone = true;
            return result;
        }
    }
}
