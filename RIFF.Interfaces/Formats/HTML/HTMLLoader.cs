// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using HtmlAgilityPack;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace RIFF.Interfaces.Formats.HTML
{
    public class HTMLLoader : IRFFormatLoader
    {
        public HTMLLoader()
        {
        }

        public static DataTable ConvertToDataTable(HtmlNode tableNode)
        {
            if (tableNode.Name == "table")
            {
                var table = new DataTable();

                foreach (var row in tableNode.SelectNodes(".//tr") ?? new HtmlNodeCollection(tableNode))
                {
                    var cells = row.SelectNodes("td|th");
                    if (cells != null && cells.Any())
                    {
                        while (table.Columns.Count < cells.Count)
                        {
                            table.Columns.Add(table.Columns.Count.ToString(), typeof(object));
                        }

                        table.Rows.Add(cells.Select(c => c.InnerText?.Replace(Convert.ToChar(160), ' ').Replace("&nbsp;", " ").Trim('\r', '\n', ' ').Replace("&amp;", "&").Replace("&lt", "<").Replace("&gt", ">")).ToArray<object>());
                    }
                }
                return table;
            }
            return null;
        }

        public List<DataTable> Load(MemoryStream stream)
        {
            var tables = new List<DataTable>();

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(stream);

            int i = 1;
            foreach (var table in doc.DocumentNode.SelectNodes("//table") ?? new HtmlNodeCollection(doc.DocumentNode))
            {
                try
                {
                    tables.Add(HTMLLoader.ConvertToDataTable(table));
                }
                catch (Exception ex)
                {
                    throw new RFSystemException(Caller(), ex, "Error extracting HTML table number {0}", i);
                }
                i++;
            }

            return tables;
        }

        private static object Caller()
        {
            return typeof(HTMLLoader);
        }
    }
}
