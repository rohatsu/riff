// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
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
            filterContext.Result = new JsonResult { Data = JsonError.Throw(filterContext.RouteData.Values["action"].ToString(), ex), JsonRequestBehavior = JsonRequestBehavior.AllowGet, ContentEncoding = Encoding.UTF8, ContentType = "application/json" };
        }
    }
}
