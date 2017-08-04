// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Data;
using System.Text;

namespace RIFF.Interfaces.Formats.CSV
{
    public static class CSVBuilder
    {
        public static string FromDataTable(DataTable dataTable, CSVOptions options = null)
        {
            options = options ?? new CSVOptions();

            var sbData = new StringBuilder();

            if (dataTable.Columns.Count == 0)
                return null;

            foreach (DataColumn col in dataTable.Columns)
            {
                string caption = col.Caption;
                if (col == null || string.IsNullOrWhiteSpace(caption))
                    sbData.Append(",");
                else
                {
                    if (options.mEscapeText)
                    {
                        string normalizedName = "\"" + caption.Replace("\"", "\"\"") + "\",";
                        if (normalizedName.StartsWith("-", StringComparison.Ordinal) || normalizedName.StartsWith("+", StringComparison.Ordinal))
                        {
                            normalizedName = "'" + normalizedName;
                        }
                        sbData.Append(normalizedName);
                    }
                    else
                    {
                        sbData.Append(caption + ",");
                    }
                }
            }

            sbData.Replace(",", System.Environment.NewLine, sbData.Length - 1, 1);

            foreach (DataRow dr in dataTable.Rows)
            {
                foreach (DataColumn col in dataTable.Columns)
                {
                    var column = dr[col];
                    if (column == null)
                        sbData.Append(",");
                    else
                    {
                        if (column is DateTime)
                        {
                            var value = (DateTime)column;
                            if (options.mEscapeText)
                            {
                                sbData.Append("\"" + value.ToString("yyyy-MM-dd") + "\",");
                            }
                            else
                            {
                                sbData.Append(value.ToString("yyyy-MM-dd") + ",");
                            }
                        }
                        else
                        {
                            if (options.mEscapeText)
                            {
                                sbData.Append("\"" + column.ToString().Replace("\"", "\"\"") + "\",");
                            }
                            else
                            {
                                sbData.Append(column + ",");
                            }
                        }
                    }
                }
                sbData.Replace(",", System.Environment.NewLine, sbData.Length - 1, 1);
            }

            return sbData.ToString();
        }
    }

    public class CSVOptions
    {
        public bool mEscapeText = true;
    }
}
