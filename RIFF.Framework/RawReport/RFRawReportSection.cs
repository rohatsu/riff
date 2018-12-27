// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFRawReportSection : IEnumerable<RFRawReportRow>
    {
        [DataMember]
        public List<string> Columns { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<RFRawReportRow> Rows { get; set; }

        public RFRawReportSection()
        {
            Rows = new List<RFRawReportRow>();
        }

        public DataTable AsDataTable()
        {
            var table = new DataTable(Name);
            table.Columns.Add("RFRowNo");
            foreach (var col in Columns)
            {
                table.Columns.Add(col);
            }
            var maxColumn = Rows.Any() ? Rows.Max(r => r.Values.Count) : 0;
            int dc = 1;
            while (Columns.Count < maxColumn)
            {
                // add dummy column
                dc++;
                try
                {
                    table.Columns.Add("Col" + dc);
                    maxColumn--;
                }
                catch (DuplicateNameException)
                {
                }
            }

            if (Rows.Any())
            {
                int rowNo = 1;
                foreach (var row in Rows)
                {
                    var values = new List<object>();
                    values.Add(rowNo);
                    values.AddRange(row.Values.Select(v => (object)v));
                    table.Rows.Add(values.ToArray());
                    rowNo++;
                }
            }
            return table;
        }

        public void ThrowIfColumnMissing(string columnName, string message = null)
        {
            if (!Columns.Contains(columnName))
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new RFLogicException(this, "Missing Column in input file: {0}", columnName);
                }
                else
                {
                    throw new RFLogicException(this, message);
                }
            }
        }

        #region IEnumerable<RFRawReportRow> Members

        public IEnumerator<RFRawReportRow> GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        #endregion IEnumerable<RFRawReportRow> Members

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        #endregion IEnumerable Members
    }
}
