// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RIFF.Framework
{
    public class RFInputFileProperties
    {
        public RFFileTrackedAttributes Attributes { get; set; }

        public string FileKey { get; set; }

        public RFCatalogKey Key { get; set; }

        public string UniqueKey { get; set; }

        public DateTimeOffset UpdateTime { get; set; }
    }

    public class RFInputFilesActivity : RFActivity
    {
        public RFInputFilesActivity(IRFProcessingContext context) : base(context, null)
        {
        }

        public RFFile GetInputFile(string uniqueKey)
        {
            if (!string.IsNullOrWhiteSpace(uniqueKey))
            {
                var inputFileKey = Context.GetKeysByType<RFFileKey>().FirstOrDefault(f => f.Value.UniqueKey == uniqueKey).Value;
                if (inputFileKey != null)
                {
                    var fileEntry = Context.LoadEntry(inputFileKey);
                    return (fileEntry as RFDocument).GetContent<RFFile>();
                }
            }
            return null;
        }

        public List<RFInputFileProperties> GetInputFilesList(List<RFEnum> fileKeys = null, RFDate? receivedDate = null)
        {
            SortedSet<long> keysInScope = null;
            if (receivedDate != null)
            {
                keysInScope = new SortedSet<long>(Context.SearchKeys(typeof(RFFileKey), null, null, 0, null, false).Where(m => m.UpdateTime.ToLocalTime().Date == receivedDate.Value.Date).Select(m => m.KeyReference));
            }

            var files = new List<RFInputFileProperties>();
            foreach (var fileKey in Context.GetKeysByType<RFFileKey>())
            {
                if (fileKeys != null && !fileKeys.Contains(fileKey.Value.FileKey))
                {
                    continue;
                }
                if (keysInScope != null && !keysInScope.Contains(fileKey.Key))
                {
                    continue;
                }
                try
                {
                    var fileEntry = Context.LoadEntry(fileKey.Value); // TODO: don't load the actual file content here!
                    var fileContent = (fileEntry as RFDocument)?.GetContent<RFFile>();
                    if (fileContent != null)
                    {
                        files.Add(new RFInputFileProperties
                        {
                            Attributes = fileContent.Attributes,
                            FileKey = fileContent.FileKey,
                            UniqueKey = fileContent.UniqueKey,
                            UpdateTime = fileEntry.UpdateTime,
                            Key = fileKey.Value
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(this, ex, "Exception loading File {0}", fileKey);
                }
            }
            return files;
        }
    }
}
