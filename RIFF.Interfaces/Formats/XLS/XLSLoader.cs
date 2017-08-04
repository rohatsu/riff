// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using NPOI.HSSF;
using NPOI.HSSF.Record;
using NPOI.HSSF.Record.Crypto;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace RIFF.Interfaces.Formats.XLS
{
    public class XLSLoader : IRFFormatLoader
    {
        public enum XLSFormat
        {
            NPOI = 1,
            POI = 2
        }

        public static readonly string OUTLINE_LEVEL = "_Outline_Level_";

        protected XLSFormat _format = XLSFormat.NPOI;
        protected string _password = null;
        private static volatile object _sync = new object(); // NPOI has MT issues

        public XLSLoader(string password = null, XLSFormat format = XLSFormat.NPOI)
        {
            _password = password;
            _format = format;
        }

        public static DataTable ConvertToDataTable(ISheet sheet)
        {
            var rows = sheet.GetRowEnumerator();
            var dt = new DataTable(sheet.SheetName);

            lock (_sync)
            {
                // pre-scan columns and outline
                int maxOutlineLevel = 0;
                while (rows.MoveNext())
                {
                    IRow row = (HSSFRow)rows.Current;
                    var dr = dt.NewRow();
                    while (dt.Columns.Count < row.LastCellNum)
                    {
                        dt.Columns.Add(dt.Columns.Count.ToString());
                    }
                    maxOutlineLevel = Math.Max(row.OutlineLevel, maxOutlineLevel);
                }
                if (maxOutlineLevel > 0)
                {
                    dt.Columns.Add(OUTLINE_LEVEL);
                }

                rows = sheet.GetRowEnumerator();
                while (rows.MoveNext())
                {
                    IRow row = (HSSFRow)rows.Current;
                    var dr = dt.NewRow();
                    if (maxOutlineLevel > 0)
                    {
                        dr[OUTLINE_LEVEL] = row.OutlineLevel;
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
                                if (cell.CellType == CellType.Numeric)
                                {
                                    if (DateUtil.IsCellDateFormatted(cell))
                                    {
                                        // date or time
                                        if (cell.DateCellValue == cell.DateCellValue.Date)
                                        {
                                            dr[i] = cell.DateCellValue.ToString(RFCore.sDateFormat);
                                        }
                                        else
                                        {
                                            dr[i] = cell.DateCellValue.ToString(RFCore.sDateTimeFormat);
                                        }
                                    }
                                    else
                                    {
                                        dr[i] = cell.NumericCellValue.ToString();
                                    }
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
            }
            return dt;
        }

        public static DataTable LoadIntoDataTableExcel95(Stream stream)
        {
            var values = new Dictionary<Tuple<int, int>, object>();
            throw new RFLogicException(typeof(XLSLoader), "NPOI extensions required");
            /*
            POIFSFileSystem fs = new POIFSFileSystem(stream, true);

            DocumentNode book;

            if (fs.Root.HasEntry("BOOK"))
            {
                book = (DocumentNode)fs.Root.GetEntry("BOOK");
            }
            else if (fs.Root.HasEntry("Book"))
            {
                book = (DocumentNode)fs.Root.GetEntry("Book");
            }
            else
            {
                throw new NotSupportedException();
            }
            var doc = book.Document;
            var docStream = new DocumentInputStream(doc);
            var ris = new RecordInputStream(docStream);

            ris.NextRecord();
            var sid = ris.Sid;
            var bof = new BOFRecord(ris);
            var fileType = bof.Type;

            var buf = new byte[8192];

            SSTRecord sst = null;

            while (ris.HasNextRecord)
            {
                var nextSid = ris.GetNextSid();
                ris.NextRecord();

                var sb = new StringBuilder();

                switch (nextSid)
                {
                    case 0x204: // Label Cell
                        {
                            ris.ReadFully(buf, 0, (int)ris.Length);
                            var memoryStream = new MemoryStream(buf, 0, (int)ris.Length);
                            using (var reader = new BinaryReader(memoryStream))
                            {
                                var row = reader.ReadUInt16();
                                var col = reader.ReadUInt16();
                                var idx = reader.ReadUInt16();
                                var strLen = reader.ReadUInt16();
                                reader.ReadByte();
                                var strVal = reader.ReadBytes(strLen);
                                var strData = System.Text.Encoding.ASCII.GetString(strVal);
                                values.Add(new Tuple<int, int>(col, row), strData);
                            }
                        };
                        break;

                    case 0xFD:
                        {
                            // shared label
                            var sl = new LabelSSTRecord(ris);
                            if (sst != null)
                            {
                                var ss = sst.GetString(sl.SSTIndex);
                                values.Add(new Tuple<int, int>(sl.Column, sl.Row), ss);
                            }
                            break;
                        }
                    case 0xFC:
                        {
                            // shared string table
                            sst = new SSTRecord(ris);
                            break;
                        }
                    case 0x203: // Number cell
                        {
                            var nr = new NumberRecord(ris);
                            values.Add(new Tuple<int, int>(nr.Column, nr.Row), nr.Value);
                        }
                        break;

                    default:
                        {
                            //Console.WriteLine("Unknown SID {0:X} or length {1}", nextSid, ris.Remaining);
                            ris.ReadFully(buf, 0, (int)ris.Length);
                        }
                        break;
                }
            }
            return CreateDataTable(values);*/
        }

        public List<DataTable> Load(MemoryStream stream)
        {
            var tables = new List<DataTable>();
            var copy = new MemoryStream();
            stream.CopyTo(copy);

            try
            {
                HSSFWorkbook workbook = null;
                if (!string.IsNullOrWhiteSpace(_password))
                {
                    // only XOR/RC4 supported, not CryptoAPI
                    Biff8EncryptionKey.CurrentUserPassword = _password;
                    switch (_format)
                    {
                        case XLSFormat.NPOI:
                            {
                                var infs = new NPOIFSFileSystem(stream);
                                workbook = new HSSFWorkbook(infs.Root, true);
                            }
                            break;

                        case XLSFormat.POI:
                            {
                                var fs = new POIFSFileSystem(stream);
                                workbook = new HSSFWorkbook(fs);
                            }
                            break;

                        default:
                            throw new RFSystemException(this, "Unsupported XLS Format {0}", _format);
                    }
                }
                else
                {
                    workbook = new HSSFWorkbook(stream);
                }
                if (workbook != null)
                {
                    for (int i = 0; i < workbook.NumberOfSheets; ++i)
                    {
                        try
                        {
                            tables.Add(ConvertToDataTable(workbook.GetSheetAt(i)));
                        }
                        catch (Exception ex)
                        {
                            throw new RFSystemException(Caller(), ex, "Error extracting XLS sheet number {0}", i);
                        }
                    }
                }
            }
            catch (OldExcelFormatException)
            {
                // congrats to ppl using 1993 formats in 2016
                tables.Add(LoadIntoDataTableExcel95(copy));
            }
            return tables;
        }

        protected static DataTable CreateDataTable(Dictionary<Tuple<int, int>, object> values)
        {
            var table = new DataTable();
            var numCols = values.Max(v => v.Key.Item1) + 1;
            var numRows = values.Max(v => v.Key.Item2) + 1;
            for (int col = 0; col < numCols; ++col)
            {
                table.Columns.Add(col.ToString());
            }
            for (int row = 0; row < numRows; ++row)
            {
                DataRow dr = table.NewRow();
                foreach (var val in values.Where(v => v.Key.Item2 == row))
                {
                    dr[val.Key.Item1] = val.Value;
                }
                table.Rows.Add(dr);
            }
            return table;
        }

        private static object Caller()
        {
            return typeof(XLSLoader);
        }
    }
}
