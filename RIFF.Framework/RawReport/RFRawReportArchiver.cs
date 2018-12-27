// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Formats.CSV;
using System;
using System.IO;
using System.Text;

namespace RIFF.Framework
{
    public class RFRawReportArchiverProcessor : RFEngineProcessorWithConfig<RFEngineProcessorKeyParam, RFRawReportArchiverProcessor.Config>
    {
        public class Config : IRFEngineProcessorConfig
        {
            public string ArchivePath { get; set; }

            public bool Enabled { get; set; }

            public Func<RFRawReport, string> FileNameFunc { get; set; }
        }

        public RFRawReportArchiverProcessor(Config config) : base(config)
        { }

        public override RFProcessingResult Process()
        {
            var result = new RFProcessingResult();
            if (_config.Enabled && !string.IsNullOrWhiteSpace(_config.ArchivePath))
            {
                var inputReport = Context.LoadDocumentContent<RFRawReport>(InstanceParams.Key);
                if (inputReport != null)
                {
                    var csvBuilder = new StringBuilder();
                    foreach (var section in inputReport.Sections)
                    {
                        csvBuilder.Append(CSVBuilder.FromDataTable(section.AsDataTable()));
                    }
                    var outputDirectory = Path.Combine(_config.ArchivePath, inputReport.ValueDate.ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(outputDirectory);
                    var outputFileName = _config.FileNameFunc != null ? _config.FileNameFunc(inputReport) : String.Format("{0}_{1}.csv", inputReport.SourceUniqueKey, inputReport.UpdateTime.ToLocalTime().ToString("yyyyMMdd_HHmmss"));
                    var outputPath = Path.Combine(outputDirectory, outputFileName);
                    File.WriteAllBytes(outputPath, Encoding.UTF8.GetBytes(csvBuilder.ToString()));
                }
                result.WorkDone = true;
            }
            return result;
        }
    }
}
