// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFRawReport
    {
        [DataMember]
        public RFEnum ReportCode { get; set; }

        [DataMember]
        public List<RFRawReportSection> Sections { get; set; }

        [DataMember]
        public string SourceFilename { get; set; }

        [DataMember]
        public string SourceUniqueKey { get; set; }

        [DataMember]
        public DateTimeOffset UpdateTime { get; set; }

        [DataMember]
        public RFDate ValueDate { get; set; }

        public RFRawReport()
        {
            Sections = new List<RFRawReportSection>();
        }

        public RFRawReportSection GetFirstSection()
        {
            if (Sections.Count == 0)
            {
                return null;
            }
            return Sections.First();
        }

        public RFRawReportSection GetSection(string name)
        {
            if (name != null)
            {
                name = name.Trim(' ', '\r', '\n');
            }
            return Sections.SingleOrDefault(s => s.Name == name);
        }

        [OnDeserialized]
        private void PostDeserialize(StreamingContext ctx)
        {
            PostDeserialize();
        }

        public void PostDeserialize()
        {
            foreach (var section in Sections)
            {
                var columns = new Dictionary<string, int>();
                int i = 0;
                foreach (var column in section.Columns)
                {
                    var columnName = column;
                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        columnName = "#" + i;
                    }
                    if (columns.ContainsKey(columnName))
                    {
                        RFStatic.Log.Info(this, "Duplicate column in raw report: {0}", columnName);
                    }
                    else
                    {
                        columns.Add(columnName, i);
                    }
                    i++;
                }
                foreach (var row in section.Rows)
                {
                    row.SetColumns(columns);
                }
            }
        }
    }
}
