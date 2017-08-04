using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace RIFF.Core
{
    internal class RFDispatchStoreSQL : IRFDispatchStore
    {
        private RFComponentContext _context;

        public RFDispatchStoreSQL(RFComponentContext context)
        {
            _context = context;
        }

        public void Finished(RFWorkQueueItem i, RFProcessingResult result)
        {
            if (Filter(i))
            {
                Update(i.Item, GetState(result), null, result);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public IEnumerable<RFErrorQueueItem> GetErrorQueue(int numRecentlyCompleted = 50)
        {
            var queue = new List<RFErrorQueueItem>();
            try
            {
                using (var conn = new SqlConnection(_context.SystemConfig.DocumentStoreConnectionString))
                {
                    conn.Open();
                    var sqlCommand = "SELECT * FROM (SELECT * FROM RIFF.DispatchQueue WHERE Environment = @Environment AND DispatchState NOT IN ( @FinishedState, @IgnoredState, @SkippedState ) ) AS [First]";
                    if (numRecentlyCompleted > 0)
                    {
                        sqlCommand = sqlCommand + String.Format(" UNION ALL SELECT * FROM (SELECT TOP {0} * FROM RIFF.DispatchQueue WHERE Environment = @Environment AND DispatchState IN ( @FinishedState, @IgnoredState, @SkippedState ) ORDER BY LastStart DESC) as [Second]", numRecentlyCompleted);
                    }
                    using (var getCommand = new SqlCommand(sqlCommand, conn))
                    {
                        getCommand.Parameters.AddWithValue("@Environment", _context.SystemConfig.Environment);
                        getCommand.Parameters.AddWithValue("@FinishedState", (int)DispatchState.Finished);
                        getCommand.Parameters.AddWithValue("@IgnoredState", (int)DispatchState.Ignored);
                        getCommand.Parameters.AddWithValue("@SkippedState", (int)DispatchState.Skipped);
                        var dataTable = new DataTable();
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            dataTable.Load(reader);
                            foreach (DataRow r in dataTable.Rows)
                            {
                                queue.Add(new RFErrorQueueItem
                                {
                                    DispatchState = (DispatchState)r["DispatchState"],
                                    Instance = (r["GraphInstance"] != null && r["GraphInstance"] != DBNull.Value) ? new RFGraphInstance
                                    {
                                        Name = r["GraphInstance"].ToString(),
                                        ValueDate = new RFDate((DateTime)r["ValueDate"])
                                    } : null,
                                    ItemType = ItemType.GraphProcessInstruction,
                                    LastStart = (r["LastStart"] != null && r["LastStart"] != DBNull.Value) ? (DateTimeOffset?)(DateTimeOffset)r["LastStart"] : null,
                                    Message = r["Message"]?.ToString(),
                                    ProcessName = r["ProcessName"]?.ToString(),
                                    ShouldRetry = (bool)r["ShouldRetry"],
                                    Weight = (long)r["Weight"],
                                    DispatchKey = r["DispatchKey"].ToString()
                                });
                            }
                            return queue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log.Exception(this, "Error reading Dispatch Store", ex);
                throw;
            }
        }

        public RFInstruction GetInstruction(string dispatchKey)
        {
            try
            {
                using (var conn = new SqlConnection(_context.SystemConfig.DocumentStoreConnectionString))
                {
                    conn.Open();
                    var sqlCommand = "SELECT * FROM RIFF.DispatchQueue WHERE Environment = @Environment AND DispatchKey = @DispatchKey";
                    using (var getCommand = new SqlCommand(sqlCommand, conn))
                    {
                        getCommand.Parameters.AddWithValue("@Environment", _context.SystemConfig.Environment);
                        getCommand.Parameters.AddWithValue("@DispatchKey", dispatchKey);
                        var dataTable = new DataTable();
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            dataTable.Load(reader);
                            if (dataTable.Rows != null && dataTable.Rows.Count > 0)
                            {
                                var dataRow = dataTable.Rows[0];

                                var instructionType = dataRow["InstructionType"].ToString();
                                var instructionContent = dataRow["InstructionContent"].ToString();

                                return RFXMLSerializer.DeserializeContract(instructionType, instructionContent) as RFInstruction;
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log.Exception(this, "Error reading Dispatch Store Error", ex);
                throw;
            }
        }

        public void Ignored(string dispatchKey)
        {
            Update(dispatchKey, null, null, null, DispatchState.Ignored, null, null, null);
        }

        public void Queued(RFWorkQueueItem i, long weight)
        {
            if (Filter(i))
            {
                Update(i.Item, DispatchState.Queued, weight, null);
            }
        }

        public void Started(RFWorkQueueItem i)
        {
            if (Filter(i))
            {
                Update(i.Item, DispatchState.Started, null, null);
            }
        }

        private static bool Filter(RFWorkQueueItem i)
        {
            if (i?.Item != null)
            {
                var type = i.Item.DispatchItemType();
                switch (type)
                {
                    case ItemType.NotSupported:
                        return false;

                    default:
                        return true;
                }
            }
            return false;
        }

        private static DispatchState GetState(RFProcessingResult result)
        {
            if (result.IsError)
                return DispatchState.Error;
            else if (!result.WorkDone && (result.UpdatedKeys == null || result.UpdatedKeys.Count == 0))
                return DispatchState.Skipped;
            else
                return DispatchState.Finished;
        }

        private void Update(IRFWorkQueueableItem i, DispatchState state, long? weight, RFProcessingResult result)
        {
            var processName = (i as RFProcessInstruction)?.ProcessName;
            var instanceName = (i as RFGraphProcessInstruction)?.Instance?.Name;
            var instanceDate = (i as RFGraphProcessInstruction)?.Instance?.ValueDate;

            Update(i.DispatchKey(), processName, instanceName, instanceDate, state, weight, result, i as RFInstruction);
        }

        private void Update(string dispatchKey, string processName, string instanceName, RFDate? valueDate, DispatchState state, long? weight, RFProcessingResult result, RFInstruction instruction)
        {
            try
            {
                using (var conn = new SqlConnection(_context.SystemConfig.DocumentStoreConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("RIFF.UpdateDispatchQueue", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Environment", RFStringHelpers.StringToSQL(_context.SystemConfig.Environment, false, 10, false));
                        cmd.Parameters.AddWithValue("@ItemType", (int)ItemType.GraphProcessInstruction);
                        cmd.Parameters.AddWithValue("@DispatchKey", RFStringHelpers.StringToSQL(dispatchKey, false, 140, false));
                        cmd.Parameters.AddWithValue("@ProcessName", RFStringHelpers.StringToSQL(processName, true, 100, false));
                        cmd.Parameters.AddWithValue("@GraphInstance", RFStringHelpers.StringToSQL(instanceName, true, 20, false));
                        if (valueDate != null && valueDate.Value.IsValid())
                        {
                            cmd.Parameters.AddWithValue("@ValueDate", valueDate.Value.Date);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ValueDate", DBNull.Value);
                        }
                        if (weight.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@Weight", weight.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Weight", DBNull.Value);
                        }
                        cmd.Parameters.AddWithValue("@DispatchState", (int)state);
                        if (state == DispatchState.Started)
                        {
                            cmd.Parameters.AddWithValue("@LastStart", DateTimeOffset.Now);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@LastStart", DBNull.Value);
                        }
                        if (result?.Messages != null)
                        {
                            cmd.Parameters.AddWithValue("@Message", RFStringHelpers.StringToSQL(String.Join("|", result.Messages), true, 200, false));
                        }
                        else if (state == DispatchState.Finished || state == DispatchState.Skipped || state == DispatchState.Started)
                        {
                            cmd.Parameters.AddWithValue("@Message", String.Empty); // clear past error messages
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Message", DBNull.Value);
                        }
                        if (result != null)
                        {
                            cmd.Parameters.AddWithValue("@ShouldRetry", result.ShouldRetry);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ShouldRetry", false);
                        }
                        if (state == DispatchState.Queued && instruction != null)
                        {
                            cmd.Parameters.AddWithValue("@InstructionType", RFStringHelpers.StringToSQL(instruction.GetType().FullName, false, 200, false));
                            cmd.Parameters.AddWithValue("@InstructionContent", RFXMLSerializer.SerializeContract(instruction));
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@InstructionType", DBNull.Value);
                            cmd.Parameters.AddWithValue("@InstructionContent", DBNull.Value);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log.Exception(this, "Error updating Dispatch Store", ex);
            }
        }
    }
}
