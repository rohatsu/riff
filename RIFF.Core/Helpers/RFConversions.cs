// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    public static class RFConversions
    {
        public static bool ParseBool(string str, bool defaultValue)
        {
            return ParseBool(str) ?? defaultValue;
        }

        public static bool? ParseBool(string str)
        {
            if (str.NotBlank())
            {
                var s = str.ToUpper().Trim();
                if (s == "TRUE" || s == "YES" || s == "1")
                {
                    return true;
                }
                if (s == "FALSE" || s == "NO" || s == "0")
                {
                    return false;
                }
            }
            return null;
        }

        public static RFDate ParseDate(string str, string format = null)
        {
            return RFDate.Parse(str, format);
        }

        public static decimal? ParseDecimal(string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                decimal d;
                bool negate = false;
                str = str.Trim();
                // check for DR or ()
                if (str.EndsWith("DR", StringComparison.Ordinal))
                {
                    negate = true;
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.StartsWith("(", StringComparison.Ordinal) && str.EndsWith(")", StringComparison.Ordinal))
                {
                    negate = true;
                    str = str.Substring(1, str.Length - 2);
                }
                if (Decimal.TryParse(str, System.Globalization.NumberStyles.Any, null, out d))
                {
                    return negate ? -d : d;
                }
            }
            return null;
        }

        public static decimal ParseDecimal(string str, decimal defaultVal)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                decimal d;
                bool negate = false;
                str = str.Trim();
                // check for DR or ()
                if (str.EndsWith("DR", StringComparison.Ordinal))
                {
                    negate = true;
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.StartsWith("(", StringComparison.Ordinal) && str.EndsWith(")", StringComparison.Ordinal))
                {
                    negate = true;
                    str = str.Substring(1, str.Length - 2);
                }
                if (Decimal.TryParse(str, System.Globalization.NumberStyles.Any, null, out d))
                {
                    return negate ? -d : d;
                }
            }
            return defaultVal;
        }

        public static T ParseEnum<T>(string str)
        {
            return (T)Enum.Parse(typeof(T), str);
        }

        public static int ParseInt(string str, int defaultVal)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                int d;
                if (Int32.TryParse(str.Trim(' '), System.Globalization.NumberStyles.Any, null, out d))
                {
                    return d;
                }
            }
            return defaultVal;
        }
    }
}
