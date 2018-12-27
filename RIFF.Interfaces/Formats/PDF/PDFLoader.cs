// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace RIFF.Interfaces.Formats.PDF
{
    public class PDFLoader : IRFFormatLoader
    {
        public class Attachment
        {
            public byte[] Content { get; set; }
            public string Name { get; set; }
        }

        public static List<Attachment> GetAttachments(Stream stream, string password = null)
        {
            PdfReader reader = null;
            if (!string.IsNullOrWhiteSpace(password))
            {
                reader = new PdfReader(stream, Encoding.ASCII.GetBytes(password));
            }
            else
            {
                reader = new PdfReader(stream);
            }

            #region Variables

            PdfDictionary catalog = null;
            PdfDictionary documentNames = null;
            PdfDictionary embeddedFiles = null;
            PdfDictionary fileArray = null;
            PdfDictionary file = null;

            PRStream prstream = null;

            Attachment fContent = null;
            List<Attachment> lstAtt = null;

            #endregion Variables

            catalog = reader.Catalog;

            lstAtt = new List<Attachment>();

            documentNames = (PdfDictionary)PdfReader.GetPdfObject(catalog.Get(PdfName.NAMES));

            if (documentNames != null)
            {
                embeddedFiles = (PdfDictionary)PdfReader.GetPdfObject(documentNames.Get(PdfName.EMBEDDEDFILES));
                if (embeddedFiles != null)
                {
                    PdfArray filespecs = embeddedFiles.GetAsArray(PdfName.NAMES);

                    for (int i = 0; i < filespecs.Size; i++)
                    {
                        i++;
                        fileArray = filespecs.GetAsDict(i);

                        file = fileArray.GetAsDict(PdfName.EF);

                        foreach (PdfName key in file.Keys)
                        {
                            prstream = (PRStream)PdfReader.GetPdfObject(file.GetAsIndirectObject(key));

                            fContent = new Attachment();
                            fContent.Name = fileArray.GetAsString(key).ToString();

                            fContent.Content = PdfReader.GetStreamBytes(prstream);
                            lstAtt.Add(fContent);
                        }
                    }
                }
            }

            return lstAtt;
        }

        public List<DataTable> Load(MemoryStream stream)
        {
            var tables = new List<DataTable>();
            var sb = new StringBuilder();
            var reader = new PdfReader(stream);
            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                var cpage = reader.GetPageN(page);
                var content = cpage.Get(PdfName.CONTENTS);

                var ir = (PRIndirectReference)content;

                var value = reader.GetPdfObject(ir.Number);

                if (value.IsStream())
                {
                    PRStream prstream = (PRStream)value;

                    var streamBytes = PdfReader.GetStreamBytes(prstream);

                    var tokenizer = new PRTokeniser(new RandomAccessFileOrArray(streamBytes));

                    try
                    {
                        while (tokenizer.NextToken())
                        {
                            if (tokenizer.TokenType == PRTokeniser.TK_STRING)
                            {
                                string str = tokenizer.StringValue;
                                sb.AppendLine(str);
                            }
                        }
                    }
                    finally
                    {
                        tokenizer.Close();
                    }
                }
            }
            Console.WriteLine(sb.ToString());
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
            return typeof(PDFLoader);
        }
    }
}
