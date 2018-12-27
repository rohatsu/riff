// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
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

            int rowNo = 0;

            if (!options.mSkipHeaders)
            {
                var lineData = new StringBuilder();
                foreach (DataColumn col in dataTable.Columns)
                {
                    string caption = col.Caption;
                    if (col == null || string.IsNullOrWhiteSpace(caption))
                        lineData.Append(",");
                    else
                    {
                        if (options.mEscapeText)
                        {
                            string normalizedName = "\"" + caption.Replace("\"", "\"\"") + "\",";
                            if (normalizedName.StartsWith("-", StringComparison.Ordinal) || normalizedName.StartsWith("+", StringComparison.Ordinal))
                            {
                                normalizedName = "'" + normalizedName;
                            }
                            lineData.Append(normalizedName);
                        }
                        else
                        {
                            lineData.Append(caption + ",");
                        }
                    }
                }
                lineData.Replace(",", "", lineData.Length - 1, 1);
                sbData.AppendLine(lineData.ToString());
                rowNo++;
            }

            foreach (DataRow dr in dataTable.Rows)
            {
                var lineData = new StringBuilder();
                foreach (DataColumn col in dataTable.Columns)
                {
                    var column = dr[col];
                    if (column == null)
                        lineData.Append(",");
                    else
                    {
                        if (column is DateTime)
                        {
                            var value = (DateTime)column;
                            if (options.mEscapeText)
                            {
                                lineData.Append("\"" + value.ToString("yyyy-MM-dd") + "\",");
                            }
                            else
                            {
                                lineData.Append(value.ToString("yyyy-MM-dd") + ",");
                            }
                        }
                        else
                        {
                            if (options.mEscapeText)
                            {
                                lineData.Append("\"" + column.ToString().Replace("\"", "\"\"") + "\",");
                            }
                            else
                            {
                                lineData.Append(column + ",");
                            }
                        }
                    }
                }
                lineData.Replace(",", "", lineData.Length - 1, 1);
                if (options.mTrimRows > rowNo)
                {
                    sbData.AppendLine(lineData.ToString().TrimEnd(','));
                }
                else
                {
                    sbData.AppendLine(lineData.ToString());
                }
                rowNo++;
            }

            return sbData.ToString();
        }
    }

    public class CSVOptions
    {
        public bool mEscapeText = true;
        public bool mSkipHeaders = false;
        public int mTrimRows = 0;
    }
}
