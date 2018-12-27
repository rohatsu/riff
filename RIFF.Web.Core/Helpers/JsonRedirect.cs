// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Web.Core.Controllers;

namespace RIFF.Web.Core.Helpers
{
    public class JsonRedirect
    {
        public string RedirectUrl { get; set; }

        public JsonRedirect(string url)
        {
            RedirectUrl = url;
        }

        public JsonRedirect(RIFFController parent, string action, string controller, object routeValues)
        {
            RedirectUrl = parent.Url.Action(action, controller, routeValues, parent.Request.Url.Scheme);
        }
    }
}
