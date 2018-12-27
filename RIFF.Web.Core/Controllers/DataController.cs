// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using RIFF.Core;
using RIFF.Core.Data;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    public class DataQueryOptions
    {
        public string Source { get; set; }
        public string[] Breakdown { get; set; }
        public string[] Data { get; set; }
        public bool ExcludeZero { get; set; }
        public string Filter { get; set; }

        public string SqlFilter()
        {
            if(Filter.IsBlank())
            {
                return String.Empty;
            }

            var filter = Filter;
            filter = filter.Replace("[\"", "({").Replace("\"]", "')");
            filter = filter.Replace("[", "(").Replace("]", ")"); // logical brackets
            filter = filter.Replace(",\"and\",", " AND ");
            filter = filter.Replace(",\"or\",", " OR ");
            filter = filter.Replace("\",\"=\",\"", "] = '");
            filter = filter.Replace("\",\"<=\",\"", "] <= '");
            filter = filter.Replace("\",\">=\",\"", "] >= '");
            filter = filter.Replace("\",\">\",\"", "] > '");
            filter = filter.Replace("\",\"<\",\"", "] < '");
            filter = filter.Replace("\",\"<>\",\"", "] <> '");
            filter = filter.Replace("{", "[").Replace("}", "]"); // field names

            // todo: contains, does not contain, starts with, ends with
            return $"({filter})";
        }
    }

    [RFControllerAuthorize(AccessLevel = RFAccessLevel.NotSet, Permission = null)]
    public class DataController : RIFFController
    {
        public DataController(IRFProcessingContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
        }

        protected RFDataSources GetConfiguration()
        {
            return Context.LoadDocumentContent<RFDataSources>(RFDataSources.GetKey(EngineConfig.KeyDomain));
        }

        public SourceDefinition[] GetSources()
        {
            return GetConfiguration()?.Sources ?? new SourceDefinition[0];
        }

        public ActionResult Index()
        {
            return View(new IndexModel
            {
                Sources = JsonConvert.SerializeObject(GetSources())
            });
        }

        protected (SourceDefinition source, ConnectionDefinition conn, List<string> groupers, Dictionary<string, string> aggregators) InitializeOptions(DataQueryOptions options)
        {
            var groupers = new List<string>();
            var aggregators = new Dictionary<string, string>();

            var config = GetConfiguration();

            var source = config.Sources.FirstOrDefault(x => x.Code == options.Source);
            if (source == null)
            {
                throw new RFLogicException(this, $"Unable to find source {options.Source}");
            }

            var conn = config.Connections.FirstOrDefault(x => x.Code == source.ConnectionCode);
            if (conn == null)
            {
                throw new RFLogicException(this, $"Unable to find connection {source.ConnectionCode}");
            }

            if (options == null)
            {
                return (source, conn, groupers, aggregators);
            }

            var breakdown = new SortedSet<string>((options.Breakdown ?? new string[0]).Union(source.Columns.Where(c => c.CanGroup && c.Mandatory).Select(c => c.Code)));
            var include = new SortedSet<string>(options.Data ?? new string[0]);

            foreach (var c in source.Columns)
            {
                if (c.CanGroup && breakdown.Contains(c.Code))
                {
                    groupers.Add($"[{c.Code}]");
                }
                else if (c.Aggregator.NotBlank() && include.Contains(c.Code))
                {
                    if (c.Aggregator.Equals("Sum", StringComparison.InvariantCultureIgnoreCase))
                    {
                        aggregators.Add($"[{c.Code}]", $"{c.Aggregator}(ISNULL([{c.Code}], 0))");
                    }
                    else if (c.Aggregator.Equals("Avg", StringComparison.InvariantCultureIgnoreCase) || c.Aggregator.Equals("Max", StringComparison.InvariantCultureIgnoreCase) || c.Aggregator.Equals("Min", StringComparison.InvariantCultureIgnoreCase))
                    {
                        aggregators.Add($"[{c.Code}]", $"{c.Aggregator}([{c.Code}])");
                    }
                }
            }

            return (source, conn, groupers, aggregators);
        }

        protected (string select, string from, string where, string group, string having, string order) PrepareSQL(SourceDefinition source, DataQueryOptions options, List<string> groupers, Dictionary<string, string> aggregators)
        {
            var sqlSelect = $"SELECT\n{string.Join(",\n", groupers.Union(aggregators.Select(a => $"{a.Value} AS {a.Key}")).Select(s => $"\t{s}"))},\n\tCOUNT(*) as [__RowCount]";
            var sqlFrom = $"FROM\n\t{source.Code}";
            var sqlFilter = options.SqlFilter() ?? String.Empty;
            var sqlNonZero = aggregators.Any() ? string.Join(" OR ", aggregators.Select(a => $"{a.Key} IS NOT NULL")) : String.Empty;
            var sqlWhere = sqlFilter.NotBlank() || sqlNonZero.NotBlank() ? $"WHERE\n\t({string.Join(" AND\n\t", new string[] { sqlFilter, sqlNonZero }.Where(s => s.NotBlank()).Select(s => $"({s})"))})" : String.Empty;
            var sqlGroup = groupers.Any() ? $"GROUP BY\n\t{string.Join(",", groupers)}" : String.Empty;
            var sqlHaving = options.ExcludeZero && aggregators.Any() ? $"HAVING\n\t{string.Join(" OR\n\t", aggregators.Select(a => $"{a.Value} <> 0"))}" : String.Empty;
            var sqlOrder = groupers.Any() ? $"ORDER BY\n\t{string.Join(",", groupers)}" : String.Empty;

            return (sqlSelect, sqlFrom, sqlWhere, sqlGroup, sqlHaving, sqlOrder);
        }

        internal JsonResult ExecuteSQL(ConnectionDefinition conn, string sqlText)
        {
            using (var sqlConn = new SqlConnection(conn.ConnectionString))
            {
                sqlConn.Open();
                var sw = Stopwatch.StartNew();

                using (var sqlCmd = new SqlCommand(sqlText, sqlConn))
                using (var reader = sqlCmd.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                {
                    Log.Debug(this, $"Data Browser SQL: {sqlText}");
                    var data = Serialize(reader);
                    return Json(new
                    {
                        data = data,
                        count = data.Count(),
                        sql = sqlText,
                        time = sw.ElapsedMilliseconds
                    });
                }
            }
        }

        [RFHandleJsonError]
        [HttpPost]
        public JsonResult GetAggregate(DataQueryOptions options)
        {
            (var source, var conn, var groupers, var aggregators) = InitializeOptions(options);

            if (groupers.Count == 0 && aggregators.Count == 0)
            {
                return null;
            }

            var sql = PrepareSQL(source, options, groupers, aggregators);

            var sqlText = string.Join("\n", new string[] { sql.select, sql.from, sql.where, sql.group, sql.having, sql.order }.Where(s => s.NotBlank()));

            return ExecuteSQL(conn, sqlText);
        }

        [RFHandleJsonError]
        [HttpPost]
        public JsonResult GetDetail(DataQueryOptions options, string group)
        {
            (var source, var conn, var groupers, var aggregators) = InitializeOptions(options);

            if (groupers.Count == 0 && aggregators.Count == 0)
            {
                return null;
            }

            var criteriaObject = Newtonsoft.Json.Linq.JObject.Parse(group);

            var criteria = new Dictionary<string, string>();

            foreach(var c in source.Columns.Where(c => c.CanGroup))
            {
                if(criteriaObject[c.Code] != null)
                {
                    criteria.Add(c.Code, criteriaObject[c.Code].ToString());
                }
            }

            if(!criteria.Any())
            {
                return null;
            }

            var sql = PrepareSQL(source, options, groupers, aggregators);

            var sqlSelect = $"SELECT {string.Join(",", source.Columns.Select(c => $"[{c.Code}]"))}, 1 AS [__RowCount]";

            var criteriaWhere = string.Join(" AND ", criteria.Select(c => $"[{c.Key}] = '{c.Value}'"));

            var sqlWhere = sql.where.NotBlank() ? $"{sql.where} AND ({criteriaWhere})" : $"WHERE ({criteriaWhere})";

            var sqlText = string.Join("\n", new string[] { sqlSelect, sql.from, sqlWhere });

            return ExecuteSQL(conn, sqlText);
        }

        public IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read())
                results.Add(SerializeRow(cols, reader));

            return results;
        }

        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
        {
            var result = new Dictionary<string, object>();
            foreach (var col in cols)
            {
                var v = reader[col];
                if (v is DateTime)
                {
                    result.Add(col, new RFDate((DateTime)v).ToJavascript());
                }
                else
                {
                    result.Add(col, reader[col]);
                }
            }
            return result;
        }
    }
}
