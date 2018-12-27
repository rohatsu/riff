// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using RIFF.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RIFF.Interfaces.Formats.XLSX
{
    public static class DatasourceInjector
    {
        public static byte[] InjectDatasource(byte[] template, string sheetName, object datasource)
        {
            var ms = new MemoryStream();
            ms.Write(template, 0, template.Length);
            ms.Seek(0, SeekOrigin.Begin);

            using (var document = SpreadsheetDocument.Open(ms, true))
            {
                var worksheetPart = OpenXML.OpenXMLHelpers.GetWorksheetPartByName(document, sheetName);

                OpenXML.OpenXMLHelpers.ClearSheet(worksheetPart.Worksheet);

                uint rowNo = 1;
                foreach (var propertyInfo in datasource.GetType().GetProperties().OrderBy(p => p.Name))
                {
                    if (RFReflectionHelpers.IsDictionary(propertyInfo))
                    {
                        InjectDictionary(datasource, propertyInfo, worksheetPart, ref rowNo);
                    }
                    else
                    {
                        var value = propertyInfo.GetValue(datasource);
                        OpenXML.OpenXMLHelpers.SetCell(worksheetPart, propertyInfo.Name, "A", rowNo);
                        if (value != null)
                        {
                            OpenXML.OpenXMLHelpers.SetCell(worksheetPart, value.ToString(), "B", rowNo);
                        }
                        else
                        {
                            OpenXML.OpenXMLHelpers.SetCellNA(worksheetPart, "B", rowNo);
                        }
                        rowNo++;
                    }
                }

                // recalculate dimension
                try
                {
                    worksheetPart.Worksheet.SheetDimension.Reference = new StringValue("A1:B" + (rowNo + 1));
                }
                catch (Exception ex)
                {
                    RFStatic.Log.Error(typeof(DatasourceInjector), "Error generating XLSX: {0}", ex.Message);
                }

                worksheetPart.Worksheet.Save();
                OpenXML.OpenXMLHelpers.ForceCalculation(document);
                document.WorkbookPart.Workbook.Save();

                return ms.ToArray();
            }
        }

        public static void InjectDictionary(object datasource, PropertyInfo propertyInfo, WorksheetPart worksheetPart, ref uint rowNo)
        {
            Type keyType = propertyInfo.PropertyType.GetGenericArguments()[0];
            Type valueType = propertyInfo.PropertyType.GetGenericArguments()[1];

            var dict = propertyInfo.GetValue(datasource) as IDictionary;
            if (dict != null)
            {
                // map keys to strings and output sorted
                var keyMap = new Dictionary<string, object>();
                foreach (var k in dict.Keys)
                {
                    if (!keyMap.ContainsKey(k.ToString()))
                    {
                        keyMap.Add(k.ToString(), k);
                    }
                }

                var sortedKeys = new SortedSet<string>(keyMap.Keys);
                foreach (var key in sortedKeys)
                {
                    InjectDictionaryEntry(key, valueType, dict[keyMap[key]], propertyInfo, worksheetPart, ref rowNo);
                }
            }
        }

        public static void InjectDictionaryEntry(object key, Type valueType, object value, PropertyInfo propertyInfo, WorksheetPart worksheetPart, ref uint rowNo)
        {
            if (valueType.IsValueType || valueType == typeof(string))
            {
                OpenXML.OpenXMLHelpers.SetCell(worksheetPart, String.Format("{0}.{1}", propertyInfo.Name, key), "A", rowNo);
                if (value != null)
                {
                    OpenXML.OpenXMLHelpers.SetCell(worksheetPart, value.ToString(), "B", rowNo);
                }
                else
                {
                    OpenXML.OpenXMLHelpers.SetCellNA(worksheetPart, "B", rowNo);
                }
                rowNo++;
            }
            else
            {
                foreach (var memberProperty in valueType.GetProperties().OrderBy(p => p.Name))
                {
                    var member = memberProperty.GetValue(value);
                    OpenXML.OpenXMLHelpers.SetCell(worksheetPart, String.Format("{0}.{1}.{2}", propertyInfo.Name, key, memberProperty.Name), "A", rowNo);
                    if (member != null)
                    {
                        OpenXML.OpenXMLHelpers.SetCell(worksheetPart, member.ToString(), "B", rowNo);
                    }
                    else
                    {
                        OpenXML.OpenXMLHelpers.SetCellNA(worksheetPart, "B", rowNo);
                    }
                    rowNo++;
                }
            }
        }
    }
}
