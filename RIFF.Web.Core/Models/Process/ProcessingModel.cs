// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;

namespace RIFF.Web.Core.Models.IO
{
    public class ProcessingModel
    {
        public string FileKey { get; set; }

        public string FileName { get; set; }

        public int FileSize { get; set; }

        public string ProcessingKey { get; set; }

        // or
        public string ReturnAction { get; set; }

        public string ReturnController { get; set; }

        // either
        public string ReturnUrl { get; set; }

        public object ReturnValues { get; set; }

        public RFProcessingTrackerHandle Tracker { get; set; }
    }
}
