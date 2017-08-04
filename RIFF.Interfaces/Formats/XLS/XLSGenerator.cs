// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
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
        public static byte[] Export(string sheetName, DataTable dataTable)
        {
            using (var stream = new MemoryStream())
            {
                var workbook = new HSSFWorkbook();
                var sheet = workbook.CreateSheet(sheetName);

                var headerRow = sheet.CreateRow(0);
                int c = 0;
                foreach (DataColumn col in dataTable.Columns)
                {
                    var cell = headerRow.CreateCell(c, CellType.String);
                    cell.SetCellValue(col.Caption);
                    c++;
                }
                int r = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    var dataRow = sheet.CreateRow(r);
                    c = 0;
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        try
                        {
                            if (col.DataType == typeof(decimal))
                            {
                                var cell = dataRow.CreateCell(c, CellType.Numeric);
                                var value = (double)((decimal)row[col]);
                                cell.SetCellValue(value);
                            }
                            else
                            {
                                var cell = dataRow.CreateCell(c, CellType.String);
                                cell.SetCellValue(row[col].ToString());
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
