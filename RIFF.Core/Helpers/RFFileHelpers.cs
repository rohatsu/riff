// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    public static class RFFileHelpers
    {
        public static string GetUnixPath(string directory, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return String.Format("{0}/{1}", directory, fileName);
            }
            return fileName;
        }

        public static string SanitizeFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
    }
}
