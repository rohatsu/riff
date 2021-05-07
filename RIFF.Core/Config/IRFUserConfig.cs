// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    public interface IRFUserConfig
    {
        List<RFUserConfigValue> GetAllValues();

        bool GetBool(string section, string item, bool mandatory, bool defaultValue, params string[] path);
        
        // mandatory
        decimal GetDecimal(string section, string item, params string[] path);

        // non-mandatory with default value
        decimal GetDecimal(string section, string item, decimal defaultValue, params string[] path);

        // non-mandatory with null
        decimal? TryGetDecimal(string section, string item, params string[] path);

        // mandatory
        int GetInt(string section, string item, params string[] path);

        // non-mandatory with default value
        int GetInt(string section, string item, int defaultValue, params string[] path);

        // non-mandatory with null
        int? TryGetInt(string section, string item, params string[] path);

        string GetString(string section, string item, bool mandatory, params string[] path);

        bool UpdateValue(int userConfigKeyID, string environment, string newValue, string userName);

        T GetEnum<T>(string section, string item, bool mandatory, params string[] path) where T : struct, IConvertible;

        List<string> GetStrings(string section, string item, bool mandatory, char separator, params string[] path);

        List<T> GetEnums<T>(string section, string item, bool mandatory, char separator, params string[] path) where T : struct, IConvertible;
    }
}
