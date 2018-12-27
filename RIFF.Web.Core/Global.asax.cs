// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Web.Core.App_Start;
using RIFF.Web.Core.Config;
using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace RIFF.Web.Core
{
    public class MvcApplication<C> : System.Web.HttpApplication where C : IRFWebConfig, new()
    {
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            Server.ClearError();
            Response.Redirect("~/Home/ErrorMessage?message=" + Uri.EscapeUriString(exception.Message));
        }

        protected void Application_Start()
        {
            RIFFStart.SetConfig(new C());

            PreStart();

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            PostStart();
        }

        protected virtual void PreStart()
        {
        }

        protected virtual void PostStart()
        {
        }
    }
}
