// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace RIFF.Core
{
    public static class RFEnumHelpers
    {
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        public static string[] GetEnumValues(Type type)
        {
            return Enum.GetNames(type);
        }

        public static string ToQualifiedString(this Enum e)
        {
            return e.GetType().Name + "." + e.ToString();
        }

        public static string ToDescription(this Enum e)
        {
            return GetEnumDescription(e);
        }

        public static T GetEnum<T>(string text, bool throwOnFail = true) where T : struct, IConvertible
        {
            var o = GetEnum(typeof(T), text, throwOnFail);
            if (o != null)
            {
                return (T)o;
            }
            return default(T);
        }

        private static Dictionary<string, DescriptionAttribute> _attributeCache = new Dictionary<string, DescriptionAttribute>();
        private static volatile object _cacheLock = new object();

        public static object GetEnum(Type t, string text, bool throwOnFail = true)
        {
            if (text.NotBlank())
            {
                if (Int32.TryParse(text.Trim(), out var i))
                {
                    return i;
                }
                foreach (var field in t.GetFields())
                {
                    var cacheKey = t.FullName + "." + field.Name;
                    if(!_attributeCache.TryGetValue(cacheKey, out var attribute))
                    {
                        attribute = Attribute.GetCustomAttribute(field,
                            typeof(DescriptionAttribute)) as DescriptionAttribute;

                        lock (_cacheLock)
                        {
                            if(!_attributeCache.ContainsKey(cacheKey))
                            {
                                _attributeCache.Add(cacheKey, attribute);
                            }
                        }
                    }

                    if (attribute != null)
                    {
                        if (attribute.Description == text)
                            return field.GetValue(null);
                    }
                    else if (field.Name == text)
                    {
                        return field.GetValue(null);
                    }
                }
                if (throwOnFail)
                {
                    throw new Exception($"Unable to parse value {text} for enum type {t.FullName}");
                }
            }
            return null;
        }
    }
}
