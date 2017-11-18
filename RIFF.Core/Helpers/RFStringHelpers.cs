// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Text;

namespace RIFF.Core
{
    public static class RFStringHelpers
    {
        public static bool IsBlank(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static bool NotBlank(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        public static string Limit(string s, int length)
        {
            if (!string.IsNullOrEmpty(s) && s.Length > length)
            {
                return s.Substring(0, length);
            }
            return s;
        }

        public static string NullIfEmpty(string str)
        {
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        public static string OrdinalSuffix(int num)
        {
            var n100 = num % 100;
            if (n100 >= 11 && n100 <= 13) return "th";

            switch (num % 10)
            {
                case 1:
                    return "st";

                case 2:
                    return "nd";

                case 3:
                    return "rd";

                default:
                    return "th";
            }
        }

        public static Int32 QuickHash(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                foreach (char c in s)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        public static string StringFromSQL(object o)
        {
            if (o == null || o == DBNull.Value)
            {
                return null;
            }
            return o.ToString();
        }

        public static object StringToSQL(string s, bool nullable, int maxLength, bool allowUnicode = true)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return nullable ? (object)DBNull.Value : (object)String.Empty;
            }

            if (!allowUnicode)
            {
                s = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s));
            }

            if (!string.IsNullOrEmpty(s) && s.Length > maxLength)
            {
                s = s.Substring(0, maxLength);
            }

            return s;
        }

        public static string ThrowIfBlank(this string s, string message = null)
        {
            if(s.IsBlank())
            {
                throw new RFLogicException(typeof(RFStringHelpers), message ?? "Parameter cannot be blank");
            }
            return s;
        }
    }
}
