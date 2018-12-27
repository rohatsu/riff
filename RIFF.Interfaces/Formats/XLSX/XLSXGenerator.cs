// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RIFF.Interfaces.Formats.XLSX
{
    public static class XLSXGenerator
    {
        public static byte[] ExportToXLSX(Dictionary<string, IRFDataSet> dataSets)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var wb = new XSSFWorkbook();

                foreach (var ds in dataSets)
                {
                    AddToWorkbook(wb, ds.Key, ds.Value.GetRows());
                }

                wb.Write(stream);
                return stream.ToArray();
            }
        }

        /*private static void AddToWorkbook(XSSFWorkbook wb, string sheetName, IRFDataSet dataSet)
        {
            AddToWorkbook(wb, sheetName, dataSet.GetRows());
        }*/

        private static void AddToWorkbook(XSSFWorkbook wb, string sheetName, IEnumerable<IRFDataRow> rows)
        {
            var sheet = wb.CreateSheet(sheetName);
            var cH = wb.GetCreationHelper();

            var cellStyles = new Dictionary<string, ICellStyle>();
            var dataFormat = wb.CreateDataFormat();
            var dateStyle = wb.CreateCellStyle();
            dateStyle.DataFormat = dataFormat.GetFormat("yyyy-MMM-dd");
            cellStyles.Add("date", dateStyle);

            if (rows.Any())
            {
                // header row
                var rowType = rows.First().GetType();
                var headerRow = sheet.CreateRow(0);
                int colNo = 0;
                foreach (var propertyInfo in rowType.GetProperties())
                {
                    if (RFReflectionHelpers.IsStruct(propertyInfo) || RFReflectionHelpers.IsMappingKey(propertyInfo))
                    {
                        foreach (var innerPropertyInfo in propertyInfo.PropertyType.GetProperties())
                        {
                            var cell = headerRow.CreateCell(colNo);
                            cell.SetCellValue(String.Format("{0}.{1}", propertyInfo.Name, innerPropertyInfo.Name));
                            colNo++;
                        }
                    }
                    else
                    {
                        var cell = headerRow.CreateCell(colNo);
                        cell.SetCellValue(propertyInfo.Name);
                        colNo++;
                    }
                }

                int rowNo = 1;
                foreach (var r in rows)
                {
                    var dataRow = sheet.CreateRow(rowNo);
                    colNo = 0;
                    foreach (var propertyInfo in rowType.GetProperties())
                    {
                        if (RFReflectionHelpers.IsStruct(propertyInfo) || RFReflectionHelpers.IsMappingKey(propertyInfo))
                        {
                            var valueStruct = propertyInfo.GetValue(r);
                            foreach (var innerPropertyInfo in propertyInfo.PropertyType.GetProperties())
                            {
                                var cell = dataRow.CreateCell(colNo);
                                if (valueStruct != null)
                                {
                                    SetValue(cell, innerPropertyInfo.GetValue(valueStruct), cellStyles);
                                }
                                colNo++;
                            }
                        }
                        else
                        {
                            var cell = dataRow.CreateCell(colNo);
                            SetValue(cell, propertyInfo.GetValue(r), cellStyles);
                            colNo++;
                        }
                    }
                    rowNo++;
                }
            }
        }

        public static byte[] ExportToXLSX(string sheetName, IEnumerable<IRFDataRow> rows)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var wb = new XSSFWorkbook();

                AddToWorkbook(wb, sheetName, rows);

                wb.Write(stream);
                return stream.ToArray();
            }
        }

        public static byte[] ExportToXLSX(string sheetName, IRFDataSet dataSet)
        {
            return ExportToXLSX(sheetName, dataSet.GetRows());
        }

        public static byte[] GenerateXLSX(Dictionary<string, object[,]> sheets)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                IWorkbook wb = new XSSFWorkbook();

                var cellStyles = new Dictionary<string, ICellStyle>();
                var dataFormat = wb.CreateDataFormat();
                var dateStyle = wb.CreateCellStyle();
                dateStyle.DataFormat = dataFormat.GetFormat("yyyy-MMM-dd");
                cellStyles.Add("date", dateStyle);

                foreach (var sheet in sheets)
                {
                    AddSheet(wb, sheet.Key, sheet.Value, cellStyles);
                }

                wb.Write(stream);
                return stream.ToArray();
            }
        }

        public static void SetValue(ICell cell, object value, Dictionary<string, ICellStyle> cellStyles)
        {
            if (value != null)
            {
                if (value is decimal)
                {
                    cell.SetCellValue((double)((decimal)value));
                }
                else if (value is decimal?)
                {
                    cell.SetCellValue((double)((decimal?)value).Value);
                }
                else if (value is double)
                {
                    cell.SetCellValue((double)value);
                }
                else if (value is double?)
                {
                    cell.SetCellValue(((double?)value).Value);
                }
                else if (value is int)
                {
                    cell.SetCellValue((int)value);
                }
                else if (value is RFDate)
                {
                    cell.SetCellValue(((RFDate)value).ToDateTime());
                    cell.CellStyle = cellStyles["date"];
                }
                else if (value is DateTime)
                {
                    cell.SetCellValue((DateTime)value);
                    cell.CellStyle = cellStyles["date"];
                }
                else
                {
                    cell.SetCellValue(value.ToString());
                }
            }
        }

        private static void AddSheet(IWorkbook wb, string sheetName, object[,] data, Dictionary<string, ICellStyle> cellStyles)
        {
            var sheet = wb.CreateSheet(sheetName);
            if (data != null)
            {
                for (int r = 0; r < data.GetLength(0); r++)
                {
                    var row = sheet.CreateRow(r);
                    for (int c = 0; c < data.GetLength(1); c++)
                    {
                        var cell = row.CreateCell(c);
                        SetValue(cell, data[r, c], cellStyles);
                    }
                }
            }
        }
    }
}
