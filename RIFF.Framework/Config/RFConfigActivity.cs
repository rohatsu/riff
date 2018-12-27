// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;

namespace RIFF.Framework
{
    public class RFConfigActivity : RFActivity
    {
        public RFConfigActivity(IRFProcessingContext context, string userName) : base(context, userName)
        {
        }

        public List<RFUserConfigValue> GetConfigs()
        {
            return Context.UserConfig.GetAllValues();
        }

        public bool UpdateValue(int userConfigKeyID, string environment, string newValue, string userName, string configPath)
        {
            var success = Context.UserConfig.UpdateValue(userConfigKeyID, environment, newValue, userName);
            if (success)
            {
                Context.UserLog.LogEntry(CreateUserLogEntry("Update", string.Format("Updated configuration value {0}", configPath), null));
            }
            return success;
        }
    }
}
