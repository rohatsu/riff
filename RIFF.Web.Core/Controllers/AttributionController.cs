// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Framework;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    public abstract class AttributionController<A> : RIFFController where A : IRFAttributionActivity, IRFActivity
    {
        protected Func<IRFProcessingContext, string, A> _activityFunc;
        protected RedirectTarget _applyRedirect;
        protected RedirectTarget _errorRedirect;

        protected AttributionController(
            IRFProcessingContext context,
            RFEngineDefinition engineConfig,
            Func<IRFProcessingContext, string, A> activityFunc,
            RedirectTarget applyRedirect,
            RedirectTarget errorRedirect = null) : base(context, engineConfig)
        {
            _activityFunc = activityFunc;
            _applyRedirect = applyRedirect;
            _errorRedirect = errorRedirect;
            if (_errorRedirect == null)
            {
                _errorRedirect = new RedirectTarget
                {
                    Action = "Index",
                    Area = "",
                    Controller = "Home"
                };
            }
        }

        [HttpGet]
        public ActionResult Apply(RFDate valueDate)
        {
            using (var activity = _activityFunc(Context, Username))
            {
                return TrackProcess(activity.ApplyTemplate(valueDate), _applyRedirect.Action, _applyRedirect.Controller, new { area = _applyRedirect.Area, valueDate = valueDate });
            }
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult Attributions(RFDate valueDate)
        {
            using (var activity = _activityFunc(Context, Username))
            {
                return Json(GetGrid(activity.GetTemplate(valueDate)));
            }
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Index(RFDate? valueDate)
        {
            valueDate = valueDate ?? RFDate.Today();

            using (var activity = _activityFunc(Context, Username))
            {
                valueDate = activity.GetLatestDate(valueDate.Value);

                if (!valueDate.HasValue)
                {
                    return Error(_errorRedirect.Action, _errorRedirect.Controller, new { area = _errorRedirect.Area }, "No Attributions found.");
                }
                else
                {
                    return View(new AttributionModel
                    {
                        ValueDate = valueDate.Value,
                        RequiresApply = activity.RequiresApply(valueDate.Value),
                        AreaName = Request.RequestContext.RouteData.DataTokens["area"].ToString(),
                        ControllerName = ControllerContext.RouteData.Values["controller"].ToString(),
                        PresentationMode = IsPresentationMode()
                    });
                }
            }
        }

        [HttpPost]
        public JsonResult Update(RFDate valueDate, FormCollection collection)
        {
            try
            {
                using (var activity = _activityFunc(Context, Username))
                {
                    var row = ExtractRow(collection);
                    if (row != null && row.IsValid())
                    {
                        return Json(activity.Replace(valueDate, row));
                    }
                    else
                    {
                        return Json(JsonError.Throw("Update", "Internal system error: invalid update."));
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("Update", ex));
            }
        }

        protected abstract IRFMappingDataRow ExtractRow(FormCollection collection);

        protected abstract IEnumerable<object> GetGrid(IRFDataSet dataSet);
    }
}
