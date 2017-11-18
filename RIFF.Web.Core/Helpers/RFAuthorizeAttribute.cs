// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Web.Core.App_Start;
using System;
using System.Web;
using System.Web.Mvc;

namespace RIFF.Web.Core.Helpers
{
    public enum ResponseType
    {
        Page = 1,
        Json = 2
    }

    public enum RFAccessLevel
    {
        NotSet = 0,
        Read = 1,
        Write = 2
    }

    public class RFControllerAuthorizeAttribute : AuthorizeAttribute
    {
        // either generic Read/Write or specific Permission
        public RFAccessLevel AccessLevel { get; set; } = RFAccessLevel.NotSet;

        public string Permission { get; set; }

        public ResponseType ResponseType { get; set; } = ResponseType.Page;

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // no permission
            if (AccessLevel == RFAccessLevel.NotSet && string.IsNullOrWhiteSpace(Permission))
            {
                SetCachePolicy(filterContext);
                return;
            }
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                if(RFSettings.GetAppSetting("DisableAuthentication", false))
                {
                    return;
                }

                // auth failed, redirect to login page
                filterContext.Result = new HttpUnauthorizedResult();
            }

            var userName = filterContext.HttpContext.User.Identity.Name;
            var controllerName = filterContext.RouteData.GetRequiredString("controller");
            var areaName = filterContext.RouteData.DataTokens["area"]?.ToString() ?? "Core";
            var actionName = filterContext.RouteData.GetRequiredString("action");
            var accessOk = AccessLevel == RFAccessLevel.NotSet || RIFFStart.UserRole.HasPermission(userName, areaName, controllerName, AccessLevel.ToString());
            var permissionOk = string.IsNullOrWhiteSpace(Permission) || RIFFStart.UserRole.HasPermission(userName, areaName, controllerName, Permission);

            if (!accessOk || !permissionOk)
            {
                RFStatic.Log.Warning(this, "Denying authorization to user {0} to area {1}/{2}/{3}:{4}",
                    userName, areaName, controllerName, AccessLevel.ToString(), Permission);

                var message = String.Format("Unauthorized - permission required: {0}/{1}/{2}/{3}", areaName,
                    controllerName, AccessLevel.ToString(), Permission);
                switch (ResponseType)
                {
                    case ResponseType.Page:
                        {
                            var viewData = new ViewDataDictionary(new RIFF.Web.Core.Models.ErrorModel
                            {
                                Message = message
                            });
                            viewData.Add("Title", "Unauthorized");
                            filterContext.Result = new ViewResult { ViewName = "RIFFError", ViewData = viewData };
                        }
                        break;

                    case ResponseType.Json:
                        filterContext.Result = new JsonResult
                        {
                            ContentType = "application/json",
                            Data = JsonError.Throw(actionName, message)
                        };
                        break;
                }
            }
            else
            {
                SetCachePolicy(filterContext);
            }
        }

        protected void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
        }

        protected void SetCachePolicy(AuthorizationContext filterContext)
        {
            // ** IMPORTANT ** Since we're performing authorization at the action level, the
            // authorization code runs after the output caching module. In the worst case this could
            // allow an authorized user to cause the page to be cached, then an unauthorized user
            // would later be served the cached page. We work around this by telling proxies not to
            // cache the sensitive page, then we hook our custom authorization code into the caching
            // mechanism so that we have the final say on whether a page should be served from the cache.
            HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, null /* data */);
        }
    }
}
