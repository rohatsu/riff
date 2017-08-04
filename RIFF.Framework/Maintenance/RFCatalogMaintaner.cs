// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Compression.ZIP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RIFF.Framework
{
    public static class RFCatalogMaintainer
    {
        public static long ExportCatalogUpdates(IRFProcessingContext context, string path, RFDate startDate, RFDate? endDate = null, string password = null)
        {
            long c = 0;

            var keysInScope = context.SearchKeys(typeof(RFCatalogKey), startDate, endDate, 99999, null, false).Where(k => k.IsValid && k.Key.Plane == RFPlane.User).ToList();

            foreach (var keyDate in keysInScope.GroupBy(k => k.UpdateTime.Date))
            {
                var fileName = Path.Combine(path, String.Format("RIFF_{0}_Updates_{1}.zip", context.Environment, keyDate.Key.ToString("yyyyMMdd")));

                context.SystemLog.Info(typeof(RFCatalogMaintainer), "Exporting {0} documents into {1}", keyDate.Count(), fileName);

                var exportableDocuments = new Dictionary<string, byte[]>();
                long cnt = 1;
                foreach (var key in keyDate)
                {
                    var doc = context.LoadEntry(key.Key) as RFDocument;
                    if (doc != null)
                    {
                        var docName = RFFileHelpers.SanitizeFileName(string.Format("{0}_{1}_{2}_{3}_{4}.xml",
                            doc.Key.GraphInstance?.ValueDate?.ToString() ?? "none",
                            doc.Key.GraphInstance?.Name ?? "none",
                            doc.Key.GetType().Name,
                            doc.Key.FriendlyString(),
                            cnt++));
                        exportableDocuments.Add(docName, Encoding.UTF8.GetBytes(RFXMLSerializer.PrettySerializeContract(doc)));
                        c++;
                    }
                }
                ZIPUtils.ZipFiles(fileName, exportableDocuments, password);
            }
            return c;
        }

        public static long ImportCatalogUpdates(IRFProcessingContext context, string path)
        {
            long c = 0;
            foreach (var f in Directory.GetFiles(path, "*.zip").OrderBy(f => f))
            {
                context.SystemLog.Info(typeof(RFCatalogMaintainer), "Importing updates from {0}", f);
                using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        foreach (var entry in ZIPUtils.UnzipArchive(fs))
                        {
                            try
                            {
                                var document = RFXMLSerializer.DeserializeContract(typeof(RFDocument).FullName, new string(Encoding.UTF8.GetChars(entry.Item2))) as RFDocument;
                                if (document != null && context.SaveEntry(document, false, false))
                                {
                                    c++;
                                }
                            }
                            catch (Exception ex)
                            {
                                context.SystemLog.Error(typeof(RFCatalogMaintainer), "Error importing entry {0}: {1}", entry.Item1, ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        context.SystemLog.Error(typeof(RFCatalogMaintainer), "Error processing .zip file {0}: {1}", f, ex.Message);
                    }
                }
            }
            return c;
        }
    }
}
