// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Web.Core.Controllers;
using System.Web.Http;
//*using System.Web.OData.Builder;
//using System.Web.OData.Extensions;

namespace RIFF.Web.Core
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            /*config.AddODataQueryFilter();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<RFCatalogKeyData>("RFCatalogKeyData");
            config.MapODataServiceRoute("ODataRoute", "odata", builder.GetEdmModel());*/
        }
    }
}
