// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace RIFF.Interfaces.Formats.XLSX
{
    public class XLSXLoader : IRFFormatLoader
    {
        private static object _sync = new object();
        private string _password = null;

        // library not thread-safe

        public XLSXLoader(string password = null)
        {
            _password = password;
        }

        public static DataTable ConvertToDataTable(ISheet sheet)
        {
            var rows = sheet.GetRowEnumerator();

            var dt = new DataTable(sheet.SheetName);

            while (rows.MoveNext())
            {
                IRow row = (XSSFRow)rows.Current;
                var dr = dt.NewRow();
                while (dt.Columns.Count < row.LastCellNum)
                {
                    dt.Columns.Add(dt.Columns.Count.ToString(), typeof(object));
                }

                for (int i = 0; i < row.LastCellNum; i++)
                {
                    var cell = row.GetCell(i);
                    if (cell == null)
                    {
                        dr[i] = null;
                    }
                    else
                    {
                        try
                        {
                            var cellType = cell.CellType;
                            if (cellType == CellType.Formula)
                            {
                                cellType = cell.CachedFormulaResultType;
                            }

                            if (cellType == CellType.Numeric)
                            {
                                if (DateUtil.IsCellDateFormatted(cell))
                                {
                                    dr[i] = cell.DateCellValue.ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    dr[i] = cell.NumericCellValue;
                                }
                            }
                            else if (cell.StringCellValue.NotBlank())
                            {
                                dr[i] = cell.StringCellValue;
                            }
                            else
                            {
                                dr[i] = cell.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (cell != null)
                            {
                                RFStatic.Log.Exception(Caller(), ex, "Error parsing cell ({0},{1})", cell.RowIndex, cell.ColumnIndex);
                            }
                            else
                            {
                                RFStatic.Log.Exception(Caller(), ex, "Error parsing cell - null");
                            }
                        }
                    }
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public List<DataTable> Load(MemoryStream stream)
        {
            lock (_sync)
            {
                var tables = new List<DataTable>();
                XSSFWorkbook workbook = null;
                Stream rawStream = stream;
                if (!string.IsNullOrWhiteSpace(_password))
                {
                    try
                    {
                        throw new RFLogicException(this, "OOXML Crypto required");
                        //var array = new OoXmlAgileCrypto().DecryptToArray(stream, _password);
                        //rawStream = new MemoryStream(array);
                    }
                    catch (Exception ex)
                    {
                        throw new RFSystemException(Caller(), ex, "Error decrypting XLSX file - incorrect password?");
                    }
                }
                workbook = new XSSFWorkbook(rawStream);
                if (workbook == null)
                {
                    throw new RFSystemException(this, "Unable to load XLSX");
                }
                for (int i = 0; i < workbook.NumberOfSheets; ++i)
                {
                    try
                    {
                        tables.Add(XLSXLoader.ConvertToDataTable(workbook.GetSheetAt(i)));
                    }
                    catch (Exception ex)
                    {
                        throw new RFSystemException(Caller(), ex, "Error extracting XLSX sheet number {0}", i);
                    }
                }
                return tables;
            }
        }

        private static object Caller()
        {
            return typeof(XLSXLoader);
        }
    }
}
