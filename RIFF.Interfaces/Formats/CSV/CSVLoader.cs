// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RIFF.Interfaces.Formats.CSV
{
    public class CSVLoader : IRFFormatLoader
    {
        protected static readonly string DEFAULT_TABLE_NAME = ":Default";
        protected string _dateFormat;
        protected Encoding _encoding;
        protected char _separator;
        protected int _skipRows;

        public CSVLoader(Encoding encoding, string dateFormat, int skipRows = 0, char separator = ',')
        {
            _encoding = encoding;
            _dateFormat = dateFormat;
            _skipRows = skipRows;
            _separator = separator;
        }

        public List<DataTable> Load(MemoryStream data)
        {
            var tables = new List<DataTable>();

            var lines = LoadRaw(data, _encoding);
            if (lines == null || lines.Count <= _skipRows)
            {
                return tables;
            }

            if (_skipRows > 0)
            {
                lines = lines.Skip(_skipRows).ToList();
            }

            var dt = new DataTable(DEFAULT_TABLE_NAME);
            var maxColumns = lines.Max(l => l.Count());
            for (int j = 0; j < maxColumns; j++)
            {
                dt.Columns.Add(j.ToString());
            }

            bool convertDates = !string.IsNullOrEmpty(_dateFormat);
            foreach (var line in lines)
            {
                var row = dt.NewRow();
                for (int i = 0; i < line.Count(); ++i)
                {
                    var cellValue = line[i];
                    if (convertDates)
                    {
                        DateTime parsedDate = DateTime.MinValue;
                        if (DateTime.TryParseExact(cellValue, _dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDate))
                        {
                            cellValue = parsedDate.ToString(RFCore.sDateFormat);
                        }
                    }
                    row[i] = cellValue;
                }
                dt.Rows.Add(row);
            }

            tables.Add(dt);
            return tables;
        }

        public List<string[]> LoadRaw(Stream data, Encoding encoding)
        {
            var lines = new List<string[]>();
            if (data == null || data.Length == 0)
            {
                return null;
            }
            foreach (var line in ReadLines(data, encoding))
            {
                lines.Add(new CSVParser(line, _separator).ToArray());
            }
            return lines;
        }

        protected static IEnumerable<string> ReadLines(Stream data, Encoding encoding)
        {
            using (var reader = new StreamReader(data, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}
