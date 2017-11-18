// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace RIFF.Core
{
    public static class RFSettings
    {
        public static bool GetAppSetting(string settingName, bool defaultValue)
        {
            var setting = GetAppSetting(settingName, null);
            if (setting.NotBlank())
            {
                var str = setting.ToLower().Trim();
                return str == "1" || str == "true" || str == "yes";
            }
            return defaultValue;
        }

        public static int GetAppSetting(string settingName, int defaultValue)
        {
            var setting = GetAppSetting(settingName, null);
            if (setting.NotBlank())
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
            return ConfigurationManager.AppSettings[settingName].ThrowIfBlank($"Missing AppSetting {settingName}");
        }

        public static string GetAppSetting(string settingName, string defaultValue)
        {
            var s = ConfigurationManager.AppSettings[settingName];
            return s.IsBlank() ? defaultValue : s;
        }

        public static RFSchedulerRange GetDowntime()
        {
            try
            {
                var day = GetAppSetting("DowntimeDay", null);
                var days = GetAppSetting("DowntimeDays", null);
                var dayList = String.Join(",", (new string[] { day, days }).Where(d => d.NotBlank()));
                var startTime = GetAppSetting("DowntimeStart", null);
                var endTime = GetAppSetting("DowntimeEnd", null);
                if (dayList.NotBlank() && startTime.NotBlank() && endTime.NotBlank())
                {
                    var weeklyWindow = new RFWeeklyWindow(dayList.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d)).ToList());
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
