// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models.Mapper;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    [RFControllerAuthorize(AccessLevel = RFAccessLevel.NotSet, Permission = null)]
    public class MapperController : RIFFController
    {
        public MapperController(IRFProcessingContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
        }

        public ActionResult GraphMap(bool full = true)
        {
            var license = RIFF.Core.RFPublicRSA.GetHost(EngineConfig.LicenseTokens.Key, EngineConfig.LicenseTokens.Value);
            var graphMap = RFGraphMapper.MapGraphs(EngineConfig.Graphs.Values, full);
            return View(new GraphMapModel
            {
                LicenseName = license.Key,
                LicenseDate = license.Value,
                Graphs = graphMap.GetCytoscapeStrings(),
                MapCytoscapeCrossGraph = graphMap.GetCytoscapeCrossGraphs()
            });
        }
    }
}
