// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace RIFF.Core
{
    public class RFCachedUserConfig : RFUserConfig
    {
        private int _timeout;
        private List<RFUserConfigValue> _cache;
        private DateTime _cacheUpdate;
        private volatile object _sync = new object();

        public RFCachedUserConfig(string connectionString, string environmentName, int timeout = 1000) : base(connectionString, environmentName)
        {
            _connectionString = connectionString;
            _environmentName = environmentName;
            _timeout = timeout;
        }

        protected void UpdateCache()
        {
            lock (_sync)
            {
                if (_cache == null || (DateTime.UtcNow - _cacheUpdate).TotalMilliseconds > _timeout)
                {
                    _cache = base.GetAllValues();
                    _cacheUpdate = DateTime.UtcNow;
                }
            }
        }

        public override List<RFUserConfigValue> GetAllValues()
        {
            UpdateCache();
            return _cache;
        }

        public override string GetString(string section, string item, bool mandatory, params string[] path)
        {
            if (item == null)
            {
                item = "(global)";
            }
            if (path == null && mandatory)
            {
                throw new RFSystemException(this, "No config path specified in " + section);
            }
            var key = string.Join(SEPARATOR, path);
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(item) || string.IsNullOrWhiteSpace(key))
            {
                RFStatic.Log.Error(this, "Section/Key/Item passed to UserConfig is blank");
                if (mandatory)
                {
                    throw new RFSystemException(this, "Section or Key is blank.");
                }
            }

            UpdateCache();
            lock (_sync)
            {
                var candidates = _cache.Where(c => c.Section.Equals(section, StringComparison.OrdinalIgnoreCase) && c.Item.Equals(item, StringComparison.OrdinalIgnoreCase) && c.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
                var value = candidates.Find(c => _environmentName.Equals(c.Environment, StringComparison.OrdinalIgnoreCase)) ?? candidates.Find(c => c.Environment?.Length == 0);
                if (value == null && mandatory)
                {
                    throw new RFSystemException(this, "Can't find mandatory config entry {0}/{1}/{2}", section, item, String.Join("/", path));
                }
                return value?.Value;
            }
        }

        public override bool UpdateValue(int userConfigKeyID, string environment, string newValue, string userName)
        {
            if (base.UpdateValue(userConfigKeyID, environment, newValue, userName))
            {
                UpdateCache();
                return true;
            }
            return false;
        }
    }
}
