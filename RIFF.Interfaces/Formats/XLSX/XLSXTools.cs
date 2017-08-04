// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace RIFF.Interfaces.Formats.XLSX
{
    public class XLSXTools
    {
        // Given a worksheet, a column name, and a row index, gets the cell at the specified column and
        public static Cell GetCell(Worksheet worksheet, string columnName, uint rowIndex)
        {
            Row row = GetRow(worksheet, rowIndex);

            if (row == null)
            {
                row = worksheet.GetFirstChild<SheetData>().AppendChild(new Row());
                row.RowIndex = rowIndex;
            }

            string cellReference = columnName + rowIndex;
            Cell existingCell = row.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, cellReference, true) == 0).FirstOrDefault();
            if (existingCell == null)
            {
                existingCell = row.AppendChild(new Cell());
                existingCell.CellReference = cellReference;
            }
            return existingCell;
        }

        public static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            if (cell.CellValue == null)
            {
                return String.Empty;
            }
            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else
            {
                return value;
            }
        }

        public static string GetExcelCol(int colNo, int offset)
        {
            int dividend = colNo + offset;
            if (dividend < 1)
            {
                return string.Empty;
            }
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

        // Given a worksheet and a row index, return the row.
        public static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().
              Elements<Row>().Where(r => r.RowIndex == rowIndex).FirstOrDefault();
        }

        public static WorksheetPart GetWorksheetPartByName(SpreadsheetDocument document, string sheetName)
        {
            IEnumerable<Sheet> sheets =
               document.WorkbookPart.Workbook.GetFirstChild<Sheets>().
               Elements<Sheet>().Where(s => s.Name == sheetName);

            if (sheets.Count() == 0)
            {
                // The specified worksheet does not exist.

                return null;
            }

            string relationshipId = sheets.First().Id.Value;
            WorksheetPart worksheetPart = (WorksheetPart)
                 document.WorkbookPart.GetPartById(relationshipId);
            return worksheetPart;
        }

        public static DataTable LoadAsDataTable(Stream stream)
        {
            DataTable dt = new DataTable();

            using (SpreadsheetDocument spreadSheetDocument = SpreadsheetDocument.Open(stream, false))
            {
                WorkbookPart workbookPart = spreadSheetDocument.WorkbookPart;
                IEnumerable<Sheet> sheets = spreadSheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>().Elements<Sheet>();
                string relationshipId = sheets.First().Id.Value;
                WorksheetPart worksheetPart = (WorksheetPart)spreadSheetDocument.WorkbookPart.GetPartById(relationshipId);
                Worksheet workSheet = worksheetPart.Worksheet;
                SheetData sheetData = workSheet.GetFirstChild<SheetData>();
                IEnumerable<Row> rows = sheetData.Descendants<Row>();

                for (int i = 0; i < 100; ++i)
                {
                    dt.Columns.Add(i.ToString());
                }

                foreach (Row row in rows)
                {
                    DataRow tempRow = dt.NewRow();

                    for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                    {
                        tempRow[i] = GetCellValue(spreadSheetDocument, row.Descendants<Cell>().ElementAt(i));
                    }

                    dt.Rows.Add(tempRow);
                }
            }
            return dt;
        }

        public static void RemoveWorksheet(SpreadsheetDocument spreadSheet, string worksheetName)
        {
            WorkbookPart wbPart = spreadSheet.WorkbookPart;
            //Get the SheetToDelete from workbook.xml
            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().Where(s => s.Name == worksheetName).FirstOrDefault();
            if (theSheet == null)
            {
                // The specified sheet doesn't exist.
            }
            //Store the SheetID for the reference
            var Sheetid = theSheet.SheetId;

            // Remove the sheet reference from the workbook.
            WorksheetPart worksheetPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            theSheet.Remove();

            // Delete the worksheet part.
            wbPart.DeletePart(worksheetPart);
        }

        public static void SetCell(WorksheetPart worksheetPart, string text, string columnName, uint rowIndex)
        {
            Cell cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            cell.CellValue = new CellValue(string.IsNullOrWhiteSpace(text) ? "" : text);
            cell.DataType = new EnumValue<CellValues>(CellValues.String);
        }

        public static void SetCellDate(WorksheetPart worksheetPart, DateTime value, string columnName, uint rowIndex)
        {
            if (value == DateTime.MinValue || value == DateTime.MinValue)
            {
                SetCell(worksheetPart, "-", columnName, rowIndex);
            }
            else
            {
                Cell cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

                cell.CellValue = new CellValue(value.ToOADate().ToString("0"));
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
            }
        }

        public static void SetCellDateTime(WorksheetPart worksheetPart, DateTime value, string columnName, uint rowIndex)
        {
            Cell cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            cell.CellValue = new CellValue(value.ToOADate().ToString("0.000#####"));
            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
        }

        public static void SetCellStyle(WorksheetPart worksheetPart, uint styleIndex, string columnName, uint rowIndex)
        {
            Cell cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            cell.StyleIndex = styleIndex;
        }

        public static void SetCellValue(WorksheetPart worksheetPart, int value, string columnName, uint rowIndex)
        {
            SetCellValue(worksheetPart, (decimal)value, columnName, rowIndex);
        }

        public static void SetCellValue(WorksheetPart worksheetPart, uint value, string columnName, uint rowIndex)
        {
            SetCellValue(worksheetPart, (decimal)value, columnName, rowIndex);
        }

        public static void SetCellValue(WorksheetPart worksheetPart, double? value, string columnName, uint rowIndex)
        {
            SetCellValue(worksheetPart, value.HasValue ? (decimal?)value.Value : null, columnName, rowIndex);
        }

        public static void SetCellValue(WorksheetPart worksheetPart, decimal? value, string columnName, uint rowIndex)
        {
            Cell cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            if (value.HasValue)
            {
                if (value.Value == 0)
                {
                    cell.CellValue = new CellValue("0");
                }
                else
                {
                    cell.CellValue = new CellValue(value.Value.ToString());
                }
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
            }
            else
            {
                cell.CellValue = new CellValue();
                cell.DataType = new EnumValue<CellValues>(CellValues.InlineString);
            }
            cell.CellFormula = null;
        }

        public static void SetInlineCell(WorksheetPart worksheetPart, string text, string columnName, uint rowIndex)
        {
            Cell cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            cell.CellValue = new CellValue(string.IsNullOrWhiteSpace(text) ? "" : text);
            cell.DataType = new EnumValue<CellValues>(CellValues.InlineString);
        }
    }
}
