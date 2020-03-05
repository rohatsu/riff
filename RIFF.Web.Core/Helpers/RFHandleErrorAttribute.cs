// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Text;
using System.Web.Mvc;

namespace RIFF.Web.Core.Helpers
{
    public class RFHandleJsonErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            Exception ex = filterContext.Exception;
            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.StatusCode = 500;
            filterContext.Result = new JsonResult { Data = JsonError.Throw(string.Format("{0}/{1}/{2}", filterContext.RouteData.DataTokens["area"].ToString(), filterContext.RouteData.Values["controller"].ToString(), filterContext.RouteData.Values["action"].ToString()), ex), JsonRequestBehavior = JsonRequestBehavior.AllowGet, ContentEncoding = Encoding.UTF8, ContentType = "application/json" };
        }
    }
}
