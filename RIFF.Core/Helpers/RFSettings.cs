// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Configuration;
using System.Globalization;

namespace RIFF.Core
{
    public static class RFSettings
    {
        public static bool GetAppSetting(string settingName, bool defaultValue)
        {
            var setting = GetAppSetting(settingName);
            if (setting != null)
            {
                var str = setting.ToLower().Trim();
                return str == "1" || str == "true" || str == "yes";
            }
            return defaultValue;
        }

        public static int GetAppSetting(string settingName, int defaultValue)
        {
            var setting = ConfigurationManager.AppSettings[settingName];
            if (setting != null)
            {
                int v;
                if (Int32.TryParse(setting, out v))
                {
                    return v;
                }
            }
            return defaultValue;
        }

        public static string GetAppSetting(string settingName)
        {
            return ConfigurationManager.AppSettings[settingName];
        }

        public static RFSchedulerRange GetDowntime()
        {
            try
            {
                var day = GetAppSetting("DowntimeDay");
                var startTime = GetAppSetting("DowntimeStart");
                var endTime = GetAppSetting("DowntimeEnd");
                if (day.NotBlank() && startTime.NotBlank() && endTime.NotBlank())
                {
                    var weeklyWindow = new RFWeeklyWindow((DayOfWeek)Enum.Parse(typeof(DayOfWeek), day));
                    var dailyWindow = new RFDailyWindow(TimeSpan.ParseExact(startTime, "hh\\:mm", CultureInfo.InvariantCulture), TimeSpan.ParseExact(endTime, "hh\\:mm", CultureInfo.InvariantCulture), null);
                    return new RFCompositeRange { ExplicitRanges = new System.Collections.Generic.List<RFSchedulerRange> { weeklyWindow, dailyWindow } };
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(typeof(RFServiceEnvironment), "Unable to read downtime from configuration", ex);
            }
            return null;
        }
    }
}
