// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace RIFF.Framework
{
    /// <summary>
    /// Allows to map a RFDataSet onto an SQL table. Properties of RFDataSet are used as keys (when
    /// saving previous data for that key will be removed). Properties of RFDataRow as data columns.
    /// Property names must map 1-1 to column names. Saving will fail if there are properties without
    /// columns in the database.
    /// </summary>
    public class RFDataSetSinkSQL : RFGraphProcessor<RFDataSetSinkSQLDomain>
    {
        protected RFDataSetSinkSQLConfig _config { get; set; }

        public RFDataSetSinkSQL(RFDataSetSinkSQLConfig config)
        {
            _config = config;
        }

        public static List<Dictionary<string, object>> GenerateRows(IRFDataSet dataSet)
        {
            var result = new List<Dictionary<string, object>>();
            var rowType = dataSet.GetRowType();

            foreach (var r in dataSet.GetRows())
            {
                var row = new Dictionary<string, object>();
                foreach (var propertyInfo in rowType.GetProperties())
                {
                    if (RFReflectionHelpers.IsStruct(propertyInfo) || RFReflectionHelpers.IsMappingKey(propertyInfo))
                    {
                        var valueStruct = propertyInfo.GetValue(r);
                        foreach (var innerPropertyInfo in propertyInfo.PropertyType.GetProperties())
                        {
                            if (!row.ContainsKey(innerPropertyInfo.Name))
                            {
                                row.Add(innerPropertyInfo.Name, innerPropertyInfo.GetValue(valueStruct));
                            }
                            // also add children if no conflict, so 2 levels of drill-down
                            if (RFReflectionHelpers.IsStruct(innerPropertyInfo) || RFReflectionHelpers.IsMappingKey(innerPropertyInfo))
                            {
                                var innerStruct = innerPropertyInfo.GetValue(valueStruct);
                                foreach (var inner2PropertyInfo in innerPropertyInfo.PropertyType.GetProperties())
                                {
                                    if (!row.ContainsKey(inner2PropertyInfo.Name))
                                    {
                                        row.Add(inner2PropertyInfo.Name, inner2PropertyInfo.GetValue(innerStruct));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!row.ContainsKey(propertyInfo.Name))
                        {
                            row.Add(propertyInfo.Name, propertyInfo.GetValue(r));
                        }
                    }
                }
                result.Add(row);
            }

            return result;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public override void Process(RFDataSetSinkSQLDomain domain)
        {
            try
            {
                var keys = new List<PropertyInfo>();
                var data = new List<PropertyInfo>();
                foreach (var propertyInfo in domain.DataSet.GetType().GetProperties())
                {
                    if (!propertyInfo.PropertyType.IsGenericType)
                    {
                        keys.Add(propertyInfo);
                    }
                }

                foreach (var propertyInfo in domain.DataSet.GetRowType().GetProperties())
                {
                    if (!propertyInfo.PropertyType.IsGenericType && !keys.Any(p => p.Name == propertyInfo.Name))
                    {
                        data.Add(propertyInfo);
                    }
                }

                int insertedRows = 0;
                int deletedRows = 0;
                using (var connection = new SqlConnection(_config.DBConnection))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var knownColumns = new Dictionary<string, int>();
                            var selectSQLBuilder = String.Format("SELECT * FROM [{0}].[{1}]", _config.SchemaName, _config.TableName);
                            using (var selectCommand = new SqlCommand(selectSQLBuilder, connection, transaction))
                            {
                                using (var reader = selectCommand.ExecuteReader(System.Data.CommandBehavior.SchemaOnly))
                                {
                                    var schema = reader.GetSchemaTable();
                                    foreach (DataRow row in schema.Rows)
                                    {
                                        if (!(bool)row["IsIdentity"])
                                        {
                                            knownColumns.Add(row["ColumnName"].ToString(), (int)row["ColumnSize"]);
                                        }
                                    }
                                }
                            }

                            // remove keys on unknown columns (UpdateTime etc.)
                            keys.RemoveAll(k => !knownColumns.ContainsKey(k.Name));

                            var deleteSQLBuilder = new StringBuilder(String.Format("DELETE FROM [{0}].[{1}] WHERE ", _config.SchemaName, _config.TableName));
                            deleteSQLBuilder.Append(String.Join(" AND ", keys.Select(k => String.Format("[{0}] = @{0}", k.Name))));

                            var insertSQLBuilder = new StringBuilder(String.Format("INSERT INTO [{0}].[{1}] (", _config.SchemaName, _config.TableName));
                            insertSQLBuilder.Append(String.Join(", ",
                                knownColumns.Select(k => String.Format("[{0}]", k.Key))));
                            insertSQLBuilder.Append(") VALUES (");
                            insertSQLBuilder.Append(String.Join(", ",
                                knownColumns.Select(k => String.Format("@{0}", k.Key))));
                            insertSQLBuilder.Append(")");

                            using (var deleteCommand = new SqlCommand(deleteSQLBuilder.ToString(), connection, transaction))
                            {
                                deleteCommand.CommandTimeout = 120;
                                foreach (var key in keys)
                                {
                                    deleteCommand.Parameters.AddWithValue(String.Format("@{0}", key.Name), ConvertValue(key.GetValue(domain.DataSet)));
                                }
                                deletedRows = deleteCommand.ExecuteNonQuery();
                            }
                            foreach (var row in GenerateRows(domain.DataSet))
                            {
                                using (var insertCommand = new SqlCommand(insertSQLBuilder.ToString(), connection, transaction))
                                {
                                    insertCommand.CommandTimeout = 120;
                                    var suppliedColumns = new SortedSet<string>();
                                    foreach (var key in keys)
                                    {
                                        insertCommand.Parameters.AddWithValue(String.Format("@{0}", key.Name), ConvertValue(key.GetValue(domain.DataSet)));
                                        suppliedColumns.Add(key.Name);
                                    }
                                    foreach (var col in row)
                                    {
                                        if (knownColumns.ContainsKey(col.Key))
                                        {
                                            insertCommand.Parameters.AddWithValue(String.Format("@{0}", col.Key), ConvertValue(col.Value, knownColumns[col.Key]));
                                            suppliedColumns.Add(col.Key);
                                        }
                                    }
                                    foreach (var nullColumn in knownColumns.Keys.Except(suppliedColumns))
                                    {
                                        insertCommand.Parameters.AddWithValue(String.Format("@{0}", nullColumn), DBNull.Value);
                                    }
                                    insertedRows += insertCommand.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            throw new RFSystemException(this, ex, "Error sinking SQL dataset into table {0}", _config.TableName);
                        }
                    }
                }
                Log.Info("SQL sink deleted {0} rows, added {1} rows into Table {2}", deletedRows, insertedRows, _config.TableName);
            }
            catch (Exception ex)
            {
                throw new RFSystemException(this, ex, "Error sinking SQL dataset into table {0}", _config.TableName);
            }
        }

        protected static object ConvertValue(object val, int columnSize = 0)
        {
            if (val == null)
            {
                return DBNull.Value;
            }
            else if (val is RFDate)
            {
                val = ((RFDate)val).ToDateTime();
            }
            else if (val is RFEnum || val is Enum || val is string)
            {
                var stringVal = val.ToString();
                if (columnSize > 0 && stringVal.Length > columnSize)
                {
                    stringVal = stringVal.Substring(0, columnSize);
                }
                val = stringVal;
            }
            return val;
        }
    }

    [DataContract]
    public class RFDataSetSinkSQLConfig : IRFGraphProcessorConfig
    {
        [DataMember]
        public string DBConnection { get; set; }

        [DataMember]
        public string SchemaName { get; set; }

        [DataMember]
        public string TableName { get; set; }
    }

    public class RFDataSetSinkSQLDomain : RFGraphProcessorDomain
    {
        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.Input)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public IRFDataSet DataSet { get; set; }
    }
}
