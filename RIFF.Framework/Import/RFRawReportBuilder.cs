// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RIFF.Framework
{
    public class RFRawReportBuilder
    {
        protected static readonly string HEADER_SECTION_NAME = ":Header";

        protected IRFReportBuilder _builder;
        protected RFRawReportSection _currentSection;
        protected string _parentSection;
        protected RFRawReport _report;
        protected bool _expectingColumns;

        public RFRawReport BuildReport(List<DataTable> tables, IRFReportBuilder builder, RFReportParserConfig config)
        {
            _report = new RFRawReport();
            _currentSection = null;
            _parentSection = null;
            _builder = builder;

            foreach (var table in tables)
            {
                _parentSection = table.TableName;
                _currentSection = new RFRawReportSection
                {
                    Name = String.Format("{0}.{1}", _parentSection, HEADER_SECTION_NAME),
                    Columns = new List<string>()
                };
                _report.Sections.Add(_currentSection);
                bool isFirstRow = true;
                foreach (DataRow row in table.Rows)
                {
                    ProcessRow(row, isFirstRow, config.HasHeaders);
                    isFirstRow = false;
                }
            }

            return _report;
        }

        protected static bool IsEmpty(DataRow row)
        {
            if (row != null && row.ItemArray != null && row.ItemArray.Length > 0 && row.ItemArray.Any(i => i != null && i.ToString() != String.Empty))
            {
                return false;
            }
            return true;
        }

        protected static List<string> RenameDupeColumns(List<string> rawColumns)
        {
            var result = new List<string>();
            if (rawColumns != null)
            {
                rawColumns = rawColumns.Select(r => r != null ? r.Trim(' ', '\r', '\n') : r).ToList(); // trim spaces/newlines in column names
                foreach (var rawColumn in rawColumns)
                {
                    if (!string.IsNullOrWhiteSpace(rawColumn) && result.Contains(rawColumn))
                    {
                        int n = 2;
                        while (result.Contains(rawColumn + n))
                        {
                            n++;
                        }
                        result.Add(rawColumn + n);
                    }
                    else
                    {
                        result.Add(rawColumn);
                    }
                }
            }
            return result;
        }

        protected void ProcessRow(DataRow row, bool isFirstRow, bool hasHeaders)
        {
            if (IsEmpty(row))
            {
                return;
            }
            var itemList = row.ItemArray.Select(i => i == null ? String.Empty : i.ToString()).ToList();
            var newSectionStart = _builder.IsNewSectionStart(itemList.ToArray());
            var newSectionPrefix = _builder.IsNewSectionPrefix(itemList.ToArray());
            if (newSectionStart.NotBlank())
            {
                // new section - assume these are column headers
                _currentSection = new RFRawReportSection
                {
                    Name = String.Format("{0}.{1}", _parentSection, newSectionStart),
                    Columns = RenameDupeColumns(itemList)
                };
                _report.Sections.Add(_currentSection);
            }
            else if (newSectionPrefix.NotBlank())
            {
                // next line will be new section
                _currentSection = new RFRawReportSection
                {
                    Name = String.Format("{0}.{1}", _parentSection, newSectionPrefix),
                };
                _expectingColumns = true;
                _report.Sections.Add(_currentSection);
            }
            else if(_expectingColumns)
            {
                _currentSection.Columns = RenameDupeColumns(itemList);
                _expectingColumns = false;
            }
            else if (isFirstRow)
            {
                if (hasHeaders)
                {
                    _currentSection.Columns = RenameDupeColumns(itemList);
                }
                else
                {
                    // use table columns and treat as data row
                    _currentSection.Columns = RenameDupeColumns(row.Table.Columns.OfType<DataColumn>().Select(s => s.ColumnName).ToList());
                    var newRow = new RFRawReportRow();
                    newRow.Values = itemList;
                    _currentSection.Rows.Add(newRow);
                }
            }
            else
            {
                var newRow = new RFRawReportRow();
                newRow.Values = itemList;
                _currentSection.Rows.Add(newRow);
            }
        }
    }
}
