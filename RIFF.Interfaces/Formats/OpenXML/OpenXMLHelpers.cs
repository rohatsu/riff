// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Linq;

namespace RIFF.Interfaces.Formats.OpenXML
{
    public static class OpenXMLHelpers
    {
        public static void ClearSheet(Worksheet worksheet)
        {
            var sheetData = worksheet.GetFirstChild<SheetData>();
            sheetData.RemoveAllChildren<Row>();
        }

        public static void ForceCalculation(SpreadsheetDocument document)
        {
            document.WorkbookPart.Workbook.CalculationProperties.ForceFullCalculation = true;
            document.WorkbookPart.Workbook.CalculationProperties.FullCalculationOnLoad = true;
        }

        public static Cell GetCell(Worksheet worksheet, string columnName, uint rowIndex)
        {
            var row = GetRow(worksheet, rowIndex);

            if (row == null)
            {
                row = worksheet.GetFirstChild<SheetData>().AppendChild(new Row());
                row.RowIndex = rowIndex;
            }

            string cellReference = columnName + rowIndex;
            var existingCell = row.Elements<Cell>().FirstOrDefault(c => string.Compare(c.CellReference.Value, cellReference, StringComparison.OrdinalIgnoreCase) == 0);
            if (existingCell == null)
            {
                existingCell = row.AppendChild(new Cell());
                existingCell.CellReference = cellReference;
            }
            return existingCell;
        }

        public static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
        }

        public static WorksheetPart GetWorksheetPartByName(SpreadsheetDocument document, string sheetName)
        {
            var sheets =
               document.WorkbookPart.Workbook.GetFirstChild<Sheets>().
               Elements<Sheet>().Where(s => s.Name == sheetName);

            if (sheets.Count() == 0)
            {
                return null;
            }

            string relationshipId = sheets.First().Id.Value;
            var worksheetPart = (WorksheetPart)
                 document.WorkbookPart.GetPartById(relationshipId);
            return worksheetPart;
        }

        public static void SetCell(WorksheetPart worksheetPart, string text, string columnName, uint rowIndex)
        {
            var cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            cell.CellValue = new CellValue(string.IsNullOrWhiteSpace(text) ? "" : text);
            cell.DataType = new EnumValue<CellValues>(CellValues.String);
        }

        public static void SetCellNA(WorksheetPart worksheetPart, string columnName, uint rowIndex)
        {
            var cell = GetCell(worksheetPart.Worksheet, columnName, rowIndex);

            cell.CellValue = new CellValue("#N/A");
            cell.DataType = new EnumValue<CellValues>(CellValues.Error);
        }
    }
}
