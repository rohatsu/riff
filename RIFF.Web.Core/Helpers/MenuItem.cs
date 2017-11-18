// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Web.Core.Helpers
{
    public class RFMenuItem
    {
        public string Action { get; set; }

        public string Area { get; set; }

        public string Controller { get; set; }

        public bool Disabled { get; set; }

        public string Icon { get; set; }

        public List<RFMenuItem> SubMenu { get; set; }

        public string Text { get; set; }

        public string Url { get; set; }

        // TODO: access permissions
    }
}
