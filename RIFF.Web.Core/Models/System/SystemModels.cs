// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Web.Core.Models.System
{
    public class DataEditorModel
    {
        public IEnumerable<string> ContentTypes { get; set; }
        public IEnumerable<string> Dates { get; set; }
        public IEnumerable<string> KeyTypes { get; set; }
        public IEnumerable<string> Paths { get; set; }
    }
}
