// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RIFF.Framework
{
    public class RFInputReportProperties
    {
        public RFCatalogKey Key { get; set; }

        public long KeyReference { get; set; }

        public int NumRows { get; set; }

        public string ReportCode { get; set; }

        public string ReportDescription { get; set; }

        public string SourceUniqueKey { get; set; }

        public DateTimeOffset UpdateTime { get; set; }

        public RFDate ValueDate { get; set; }
    }

    public class RFInputReportsActivity : RFActivity
    {
        public RFInputReportsActivity(IRFProcessingContext context) : base(context, null)
        {
        }

        public RFRawReport GetInputReport(long keyReference)
        {
            var document = GetInputReportDocument(keyReference);
            if (document != null)
            {
                return document.GetContent<RFRawReport>();
            }
            return null;
        }

        public RFDocument GetInputReportDocument(long keyReference)
        {
            var inputFileKey = Context.GetKeysByType<RFRawReportKey>().FirstOrDefault(f => f.Key == keyReference).Value;
            if (inputFileKey != null)
            {
                var fileEntry = Context.LoadEntry(inputFileKey);
                return fileEntry as RFDocument;
            }
            return null;
        }

        public List<RFInputReportProperties> GetInputReportsList(RFDate valueDate)
        {
            var reports = new List<RFInputReportProperties>();
            foreach (var reportKey in Context.GetKeysByType<RFRawReportKey>().Where(k => k.Value.GraphInstance != null && k.Value.GraphInstance.ValueDate.Value == valueDate))
            {
                try
                {
                    var fileEntry = Context.LoadEntry(reportKey.Value) as RFDocument;
                    if (fileEntry != null && fileEntry.Content is RFRawReport)
                    {
                        var fileContent = (fileEntry as RFDocument).GetContent<RFRawReport>();
                        reports.Add(new RFInputReportProperties
                        {
                            ReportCode = fileContent.ReportCode,
                            NumRows = fileContent.Sections != null ? fileContent.Sections.Sum(s => s.Rows.Count()) : 0,
                            ReportDescription = String.Empty,//"description?",
                            ValueDate = fileContent.ValueDate,
                            SourceUniqueKey = fileContent.SourceUniqueKey,
                            UpdateTime = fileEntry.UpdateTime,
                            Key = reportKey.Value,
                            KeyReference = reportKey.Key
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Exception loading RawReport {0}", reportKey);
                }
            }
            return reports;
        }
    }
}
