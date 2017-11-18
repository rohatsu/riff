// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using System.Web;

namespace RIFF.Web.Core.Helpers
{
    public static class JsonNetHelpers
    {
        public static HtmlString SerializeObject(object o)
        {
            return new HtmlString(JsonConvert.SerializeObject(o));
        }
    }
}
