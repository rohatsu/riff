// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace RIFF.Framework
{
    [DataContract]
    public enum RFReportParserFormat
    {
        [EnumMember]
        AutoDetect = 0,

        [EnumMember]
        CSV = 1,

        [EnumMember]
        ExcelXLS = 2,

        [EnumMember]
        ExcelXLSX = 3,

        [EnumMember]
        XML = 4
    }

    [DataContract]
    public class RFReportParserConfig : IRFGraphProcessorConfig
    {
        [DataMember]
        public IRFFormatLoader CustomLoader { get; set; }

        [DataMember]
        public string DateFormat { get; set; }

        [DataMember]
        public Encoding Encoding { get; set; }

        [DataMember]
        public RFReportParserFormat Format { get; set; }

        [DataMember]
        public string GraphInstance { get; set; }

        [DataMember]
        public bool HasHeaders { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public RFEnum ReportCode { get; set; }

        [DataMember]
        public IEnumerable<string> RequiredColumns { get; set; }

        [DataMember]
        public char Separator { get; set; }

        [DataMember]
        public int SkipRows { get; set; }

        [IgnoreDataMember]
        public Func<RFRawReport, string> ValidatorFunc { get; set; }

        public RFReportParserConfig()
        {
            // defaults
            Encoding = Encoding.ASCII;
            Format = RFReportParserFormat.AutoDetect;
            DateFormat = null;
            HasHeaders = true;
            ValidatorFunc = null;
            RequiredColumns = null;
            Separator = ',';
        }
    }

    public class RFReportParserProcessor : RFEngineProcessor<RFEngineProcessorKeyParam>
    {
        public static readonly Dictionary<string, RFReportParserFormat> sExtensionFormats = new Dictionary<string, RFReportParserFormat>
        {
            { ".csv", RFReportParserFormat.CSV },
            { ".txt", RFReportParserFormat.CSV },
            { ".xls", RFReportParserFormat.ExcelXLS },
            { ".xlsx", RFReportParserFormat.ExcelXLSX },
            { ".xml", RFReportParserFormat.XML }
        };

        public static readonly string sIgnoreReport = "IGNORE";
        protected IRFReportBuilder _builder;
        protected RFReportParserConfig _config;

        public RFReportParserProcessor(RFReportParserConfig config, IRFReportBuilder builder)
        {
            _config = config;
            _builder = builder;
        }

        public static RFRawReport LoadFromFile(string filePath, RFDate? valueDate, RFReportParserConfig config, IRFReportBuilder builder = null)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return LoadFromStream(ms, new RFFileTrackedAttributes
                    {
                        FileName = Path.GetFileName(filePath),
                        ModifiedDate = File.GetLastWriteTime(filePath),
                        FullPath = filePath,
                        FileSize = new FileInfo(filePath).Length
                    },
                        valueDate, config, builder ?? new RFSimpleReportBuilder());
                }
            }
        }

        public static RFRawReport LoadFromStream(MemoryStream stream, RFFileTrackedAttributes attributes, RFDate? valueDate, RFReportParserConfig config, IRFReportBuilder builder)
        {
            RFReportParserFormat actualFormat = config.Format;
            if (config.Format == RFReportParserFormat.AutoDetect && config.CustomLoader == null)
            {
                var extension = Path.GetExtension(attributes.FileName).ToLower();
                if (!sExtensionFormats.TryGetValue(extension, out actualFormat))
                {
                    throw new RFSystemException(typeof(RFReportParserProcessor), "Unable to auto-detect file format of file {0}", attributes.FileName);
                }
            }

            var loader = config.CustomLoader ?? GetLoader(actualFormat, config);
            var tables = loader.Load(stream);
            if (tables == null || tables.Count == 0)
            {
                RFStatic.Log.Warning(typeof(RFReportParserProcessor), "No data loaded from file {0}", attributes.FileName);
                return null;
            }

            var rawReport = new RFRawReportBuilder().BuildReport(tables, builder, config);
            if (rawReport == null)
            {
                RFStatic.Log.Warning(typeof(RFReportParserProcessor), "No data extracted from file {0}", attributes.FileName);
                return null;
            }

            if (valueDate.HasValue) // override
            {
                rawReport.ValueDate = valueDate.Value;
            }
            else
            {
                rawReport.ValueDate = builder.ExtractValueDate(attributes, rawReport);
            }
            if (rawReport.ValueDate == RFDate.NullDate)
            {
                throw new RFLogicException(typeof(RFReportParserProcessor), "Unable to derive value date for file {0}.", attributes.FileName);
            }
            rawReport.ReportCode = config.ReportCode;
            rawReport.UpdateTime = attributes.ModifiedDate;
            rawReport.PostDeserialize();

            if (config.ValidatorFunc != null)
            {
                var errorMessage = config.ValidatorFunc(rawReport);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    if (errorMessage == sIgnoreReport)
                    {
                        return null;
                    }
                    throw new RFLogicException(typeof(RFReportParserProcessor), "Report validation failed - incorrect file? ({0})", errorMessage);
                }
            }

            if (config.RequiredColumns != null && config.RequiredColumns.Any())
            {
                var cols = new SortedSet<string>(rawReport.GetFirstSection().Columns);
                var missingColumns = config.RequiredColumns.Where(rc => !cols.Contains(rc));
                if (missingColumns.Any())
                {
                    throw new RFLogicException(typeof(RFReportParserProcessor), "Missing {0} mandatory columns - incorrect file? ({1})", missingColumns.Count(), string.Join(",", missingColumns));
                }
            }
            return rawReport;
        }

        public override RFProcessingResult Process()
        {
            var inputFile = Context.LoadDocumentContent<RFFile>(InstanceParams.Key);
            RFRawReport rawReport = null;

            using (var ms = new MemoryStream(inputFile.Data))
            {
                rawReport = LoadFromStream(ms, inputFile.Attributes, inputFile.ValueDate, _config, _builder);
            }

            if (rawReport != null)
            {
                rawReport.SourceUniqueKey = inputFile.UniqueKey;
                rawReport.SourceFilename = inputFile.Attributes.FileName;

                Context.SaveEntry(RFDocument.Create(RFRawReportKey.Create(KeyDomain, _config.ReportCode, new RFGraphInstance { ValueDate = rawReport.ValueDate, Name = _config.GraphInstance }),
                    rawReport));
            }

            return new RFProcessingResult { WorkDone = true };
        }

        private static IRFFormatLoader GetLoader(RFReportParserFormat format, RFReportParserConfig config)
        {
            switch (format)
            {
                case RFReportParserFormat.CSV:
                    return new RIFF.Interfaces.Formats.CSV.CSVLoader(config.Encoding, config.DateFormat, config.SkipRows, config.Separator);

                case RFReportParserFormat.ExcelXLS:
                    return new RIFF.Interfaces.Formats.XLS.XLSLoader(config.Password);

                case RFReportParserFormat.ExcelXLSX:
                    return new RIFF.Interfaces.Formats.XLSX.XLSXLoader(config.Password);

                default:
                    throw new RFSystemException(typeof(RFReportParserProcessor), "Unsupported file format for report parser: {0}", format.ToString());
            }
        }
    }
}
