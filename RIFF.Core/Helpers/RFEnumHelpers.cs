// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
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
    }
}
