// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace RIFF.Web.Core.Helpers
{
    public class DateTimeBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            try
            {
                var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                if (value != null)
                {
                    var date = value.ConvertTo(typeof(DateTime), CultureInfo.CurrentCulture);
                    return date;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
    }

    public class NullableDecimalBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            try
            {
                decimal value = 0;
                if (Decimal.TryParse(controllerContext.HttpContext.Request.Form[bindingContext.ModelName], NumberStyles.Any, null, out value))
                {
                    return (decimal?)value;
                }
            }
            catch (Exception)
            {
            }
            return (decimal?)null;
        }
    }

    public class NullableDecimalBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(Type modelType)
        {
            if (modelType == typeof(decimal?))
            {
                return new NullableDecimalBinder();
            }
            return null;
        }
    }

    public class SimpleDateBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            try
            {
                RFDate value = RFDate.NullDate;
                var stringValue = controllerContext.HttpContext.Request.Form[bindingContext.ModelName];
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    stringValue = controllerContext.HttpContext.Request.QueryString[bindingContext.ModelName];
                }
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                int ymd = 0;
                if (Int32.TryParse(stringValue, out ymd))
                {
                    return new RFDate(ymd);
                }
                if(stringValue == "null")
                {
                    return RFDate.NullDate;
                }
                DateTime dateTime = DateTime.MinValue;
                if (DateTime.TryParseExact(stringValue.Substring(0, 10), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime))
                {
                    return new RFDate(dateTime);
                }
                if (DateTime.TryParseExact(stringValue.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime))
                {
                    return new RFDate(dateTime);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
    }

    public class SimpleDateBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(Type modelType)
        {
            if (modelType == typeof(RFDate) || modelType == typeof(RFDate?))
            {
                return new SimpleDateBinder();
            }
            return null;
        }
    }
}
