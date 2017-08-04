// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RIFF.Framework
{
    /// <summary>
    /// For loading simple reports that only have a single section and date derivable from the filename
    /// </summary>
    public class RFSimpleReportBuilder : IRFReportBuilder
    {
        public Func<RFDate> DefaultDate { get; set; }

        public string Format { get; set; }

        public int Offset { get; set; }

        // if one of these values is in first column, it will be treated as column line of a section
        public IEnumerable<string> SectionStarts { get; set; }

        // if one of these values is in first column, next line will be treated as column line of a new section
        public IEnumerable<string> SectionPrefixes { get; set; }

        public int Start { get; set; }

        public virtual RFDate ExtractValueDate(RFFileTrackedAttributes fileAttributes, RFRawReport content)
        {
            if (!string.IsNullOrWhiteSpace(Format))
            {
                try
                {
                    if (fileAttributes != null && !string.IsNullOrWhiteSpace(fileAttributes.FileName) && fileAttributes.FileName.Length >= (Start + Format.Length))
                    {
                        return new RFDate(DateTime.ParseExact(fileAttributes.FileName.Substring(Start, Format.Length), Format, CultureInfo.InvariantCulture)).OffsetWeekdays(Offset);
                    }
                }
                catch (Exception)
                {
                    throw new RFLogicException(this, "Unable to extract date from file name {0} - has the format changed?", fileAttributes.FileName);
                }
            }
            if (DefaultDate != null)
            {
                return DefaultDate();
            }
            return RFDate.NullDate;
        }

        public virtual string IsNewSectionStart(string[] line)
        {
            if (line.Length > 0)
            {
                var trimLine = (line[0] ?? "").Trim(' ', '\r', '\n');
                return SectionStarts != null && SectionStarts.Contains(trimLine) ? trimLine : null;
            }
            return null;
        }

        public virtual string IsNewSectionPrefix(string[] line)
        {
            if (line.Length > 0)
            {
                var trimLine = (line[0] ?? "").Trim(' ', '\r', '\n');
                return SectionPrefixes != null && SectionPrefixes.Contains(trimLine) ? trimLine : null;
            }
            return null;
        }
    }

    public interface IRFReportBuilder
    {
        RFDate ExtractValueDate(RFFileTrackedAttributes fileAttributes, RFRawReport content);

        string IsNewSectionStart(string[] line);

        string IsNewSectionPrefix(string[] line);
    }
}
