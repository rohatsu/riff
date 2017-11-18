// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Web.Core.Helpers
{
    public class JsonError
    {
        public string ErrorMessage { get; set; }

        public static JsonError Throw(string action, string message)
        {
            return new JsonError
            {
                ErrorMessage = String.Format("({0}) {1}", action, message)
            };
        }

        public static JsonError Throw(string action, string message, params object[] formats)
        {
            return Throw(action, String.Format(message, formats));
        }

        public static JsonError Throw(string action, Exception ex)
        {
            return Throw(action, ex.Message);
        }
    }
}
