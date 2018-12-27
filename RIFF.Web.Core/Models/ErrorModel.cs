// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Web.Core.Models
{
    public class ErrorModel
    {
        public string DebugInfo { get; set; }

        public Exception Exception { get; set; }

        public string Message { get; set; }

        public string RedirectAction { get; set; }

        public string RedirectController { get; set; }

        public dynamic RouteValues { get; set; }
    }
}
