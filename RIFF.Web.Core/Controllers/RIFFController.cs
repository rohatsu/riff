// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json.Linq;
using RIFF.Core;
using RIFF.Framework.Preferences;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    [RFControllerAuthorize(AccessLevel = RFAccessLevel.Read, ResponseType = ResponseType.Page)] // every RIFF controller needs at least Read permission to run
    public abstract class RIFFController : Controller
    {
        public string LoginUsername
        {
            get
            {
                var username = User?.Identity?.Name;
                if (username != null)
                {
                    if (username.Contains("\\"))
                    {
                        username = username.Substring(username.IndexOf('\\') + 1);
                    }
                    return username.ToLower().Trim();
                }
                return "Guest";
            }
        }

        public string Username
        {
            get
            {
                if (_userName != null)
                {
                    return _userName;
                }
                else
                {
                    _userName = RFUser.GetUserName(User);
                    return _userName;
                }
            }
        }

        protected IRFProcessingContext Context { get { return _context; } }

        protected RFEngineDefinition EngineConfig { get { return _engineConfig; } }

        protected IRFLog Log { get { return _context.SystemLog; } }

        private IRFProcessingContext _context;
        private RFEngineDefinition _engineConfig;
        private string _userName;

        protected RIFFController(IRFProcessingContext context, RFEngineDefinition engineConfig)
        {
            _context = context;
            _engineConfig = engineConfig;
        }

        public static DateTimeOffset ConvertJsDate(string jsDate)
        {
            string formatString = "ddd MMM d yyyy HH:mm:ss 'GMT'zzzzz";

            var gmtIndex = jsDate.IndexOf(" GMT");
            if (gmtIndex > -1)
            {
                jsDate = jsDate.Remove(gmtIndex + 9);
                return DateTimeOffset.ParseExact(jsDate, formatString, null);
            }
            return DateTimeOffset.Parse(jsDate);
        }

        public ViewResult Error(string redirectAction, string redirectController, dynamic routeValues, string message, params object[] formats)
        {
            return Error(redirectAction, redirectController, routeValues, null, message, formats);
        }

        public ViewResult Error(string redirectAction, string redirectController, dynamic routeValues, Exception ex, string message, params object[] formats)
        {
            if (ex != null)
            {
                Log.Exception(this, ex, message, formats);
            }
            else
            {
                Log.Error(this, message, formats);
            }

            return View("RIFFError", new ErrorModel
            {
                Message = formats != null ? String.Format(message, formats) : message,
                RedirectAction = redirectAction,
                RedirectController = redirectController,
                RouteValues = routeValues,
                DebugInfo = null,
                Exception = ex
            });
        }

        public ViewResult ErrorMessage(string message)
        {
            return Error("Index", "Home", new { area = "" }, null, message, null);
        }

        public bool IsPresentationMode()
        {
            return UserPreferences.IsPresentationMode(Context, Username);
        }

        public JsonResult ReportProcessingError(string action, RFProcessingTracker status)
        {
            if (status != null)
            {
                if (!status.IsComplete)
                {
                    return Json(JsonError.Throw(action, "Operation timeout - please contact support."));
                }
                else if (status.IsError())
                {
                    return Json(JsonError.Throw(action, status.Error));
                }
                else
                {
                    return Json(true);
                }
            }
            return Json(JsonError.Throw(action, "Unknown error - please contact support."));
        }

        public ActionResult TrackProcess(RFProcessingTrackerHandle handle, string continueAction, string continueController, object routeValues)
        {
            ProcessController.SubmitModel(new Models.IO.ProcessingModel
            {
                Tracker = handle,
                ProcessingKey = handle.TrackerCode,
                ReturnAction = continueAction,
                ReturnController = continueController,
                ReturnValues = routeValues
            });
            return RedirectToAction("ProcessingStatus", "Process", new { area = "", processKey = handle.TrackerCode });
        }

        protected static bool? GetCollectionBool(string field, FormCollection collection, bool? defaultValue = null)
        {
            var strVal = GetCollectionString(field, collection);
            if (!string.IsNullOrWhiteSpace(strVal))
            {
                if (strVal.ToUpper() == "TRUE" || strVal.ToUpper() == "1" || strVal.ToUpper() == "YES")
                {
                    return true;
                }
                else if (strVal.ToUpper() == "FALSE" || strVal.ToUpper() == "0" || strVal.ToUpper() == "NO")
                {
                    return false;
                }
                bool x;
                if (Boolean.TryParse(strVal, out x))
                {
                    return x;
                }
            }
            return defaultValue;
        }

        protected static RFDate? GetCollectionDate(string field, FormCollection collection, RFDate? defaultValue = null)
        {
            try
            {
                var strVal = GetCollectionString(field, collection);
                if (!string.IsNullOrWhiteSpace(strVal))
                {
                    return RFDate.Parse(strVal);
                }
            }
            catch (Exception)
            {
            }
            return defaultValue;
        }

        protected static DateTimeOffset GetCollectionDateTimeOffset(string field, FormCollection collection)
        {
            try
            {
                var strVal = GetCollectionString(field, collection);
                if (!string.IsNullOrWhiteSpace(strVal))
                {
                    // Fri Nov 25 2016 17:22:48 GMT+0900 (Japan Standard Time)
                    return ConvertJsDate(strVal);
                }
            }
            catch (Exception)
            {
            }
            return DateTimeOffset.MinValue;
        }

        protected static decimal? GetCollectionDecimal(string field, FormCollection collection, decimal? defaultValue = null)
        {
            var strVal = GetCollectionString(field, collection);
            if (!string.IsNullOrWhiteSpace(strVal))
            {
                decimal x;
                if (Decimal.TryParse(strVal.Replace(",", ""), NumberStyles.Any, null, out x))
                {
                    return x;
                }
            }
            return defaultValue;
        }

        protected static int? GetCollectionInt(string field, FormCollection collection, int? defaultValue = null)
        {
            var strVal = GetCollectionString(field, collection);
            if (!string.IsNullOrWhiteSpace(strVal))
            {
                int x;
                if (Int32.TryParse(strVal, out x))
                {
                    return x;
                }
            }
            return defaultValue;
        }

        protected static string GetCollectionString(string field, FormCollection collection)
        {
            if (collection.GetValue(field) != null)
            {
                return collection[field];
            }
            return null;
        }

        protected static bool? GetJsonBool(string field, JToken obj, bool mandatory = false)
        {
            var s = GetJsonString(field, obj, mandatory);
            if (s != null)
            {
                if (s.ToUpper() == "YES" || s.ToUpper() == "TRUE" || s.ToUpper() == "1")
                {
                    return true;
                }
                if (s.ToUpper() == "NO" || s.ToUpper() == "FALSE" || s.ToUpper() == "0")
                {
                    return false;
                }
            }
            if (mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return null;
        }

        protected static RFDate? GetJsonDate(string field, JToken obj, bool mandatory = false)
        {
            var v = obj[field] as JValue;
            if (v?.Value != null)
            {
                if (v.Value is DateTime)
                {
                    return (DateTime)v.Value;
                }
                else
                {
                    return RFDate.Parse(v.Value.ToString());
                }
            }
            if (mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return null;
        }

        protected static DateTime? GetJsonDateTime(string field, JToken obj, bool mandatory = false)
        {
            var v = obj[field] as JValue;
            if (v != null && v.Value is DateTime)
            {
                return (DateTime)v.Value;
            }
            if (mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return null;
        }

        protected static decimal? GetJsonDecimal(string field, JToken obj, bool mandatory = false)
        {
            var s = GetJsonString(field, obj, mandatory);
            if (s != null)
            {
                decimal d;
                if (Decimal.TryParse(s, out d))
                {
                    return d;
                }
            }
            if (mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return null;
        }

        protected static T? GetJsonEnum<T>(string field, JToken obj, bool mandatory = false) where T : struct, IComparable, IConvertible, IFormattable
        {
            var s = GetJsonString(field, obj, mandatory);
            if (s != null)
            {
                T d;
                if (Enum.TryParse<T>(s, out d))
                {
                    return d;
                }
            }
            if (mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return null;
        }

        protected static int? GetJsonInt(string field, JToken obj, bool mandatory = false)
        {
            var s = GetJsonString(field, obj, mandatory);
            if (s != null)
            {
                int d;
                if (Int32.TryParse(s, out d))
                {
                    return d;
                }
            }
            if (mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return null;
        }

        protected static string GetJsonString(string field, JToken obj, bool mandatory = false)
        {
            var v = obj[field];
            if (v == null && mandatory)
            {
                throw new RFLogicException(typeof(RIFFController), "Mandatory field {0} not found.", field);
            }
            return v?.ToString();
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }
            filterContext.Result = Error("Index", "Home", new { area = "" }, filterContext.Exception, "RIFF Support has been notified");
            filterContext.ExceptionHandled = true;
        }
    }
}
