// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;

namespace RIFF.Web.Core.Models
{
    public class AttributionModel
    {
        public string AreaName { get; set; }

        public string ControllerName { get; set; }

        public bool PresentationMode { get; set; }
        public bool RequiresApply { get; set; }

        public RFDate ValueDate { get; set; }
    }
}
