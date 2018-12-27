// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using RIFF.Core;
using System;
using System.Data;
using System.IO;

namespace RIFF.Interfaces.Formats.XLS
{
    public static class XLSGenerator
    {
        public static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public static byte[] Export(string sheetName, DataTable dataTable, bool outputHeader = true)
        {
            using (var stream = new MemoryStream())
            {
                var workbook = new HSSFWorkbook();
                var sheet = workbook.CreateSheet(sheetName);

                var dateStyle = workbook.CreateCellStyle();
                dateStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("d-mmm-yy");

                int r = 0;
                int c = 0;
                if (outputHeader)
                {
                    var headerRow = sheet.CreateRow(0);
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        var cell = headerRow.CreateCell(c, CellType.String);
                        cell.SetCellValue(col.Caption);
                        c++;
                    }
                    r++;
                }
                foreach (DataRow row in dataTable.Rows)
                {
                    var dataRow = sheet.CreateRow(r);
                    c = 0;
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        try
                        {
                            var v = row[col];
                            if (v != null && v != DBNull.Value)
                            {
                                var type = col.DataType;
                                if (type == typeof(object))
                                {
                                    type = v.GetType();
                                }

                                if (type == typeof(decimal))
                                {
                                    var cell = dataRow.CreateCell(c, CellType.Numeric);
                                    cell.SetCellValue((double)((decimal)v));
                                }
                                else if (type == typeof(int))
                                {
                                    var cell = dataRow.CreateCell(c, CellType.Numeric);
                                    cell.SetCellValue((int)v);
                                }
                                else if (type == typeof(double))
                                {
                                    var cell = dataRow.CreateCell(c, CellType.Numeric);
                                    cell.SetCellValue((double)v);
                                }
                                else if (type == typeof(RFDate))
                                {
                                    var cell = dataRow.CreateCell(c, CellType.Numeric);
                                    cell.SetCellValue(((RFDate)v).Date);
                                    cell.CellStyle = dateStyle;
                                }
                                else if (type == typeof(DateTime))
                                {
                                    var cell = dataRow.CreateCell(c, CellType.Numeric);
                                    cell.SetCellValue(((DateTime)v).Date);
                                    cell.CellStyle = dateStyle;
                                }
                                else
                                {
                                    var cell = dataRow.CreateCell(c, CellType.String);
                                    cell.SetCellValue(v.ToString());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            RFStatic.Log.Warning(typeof(XLSGenerator), "Error exporting cell to XLS: {0}", ex.Message);
                        }
                        c++;
                    }
                    r++;
                }
                for (int s = 0; s < dataTable.Columns.Count; s++)
                {
                    sheet.AutoSizeColumn(s);
                }
                workbook.Write(stream);
                return stream.ToArray();
            }
        }
    }
}
