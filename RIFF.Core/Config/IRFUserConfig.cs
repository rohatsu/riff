// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    public interface IRFUserConfig
    {
        List<RFUserConfigValue> GetAllValues();

        bool GetBool(string section, string item, bool mandatory, bool defaultValue, params string[] path);

        decimal? GetDecimal(string section, string item, bool mandatory, decimal? defaultValue, params string[] path);

        int? GetInt(string section, string item, bool mandatory, int? defaultValue, params string[] path);

        string GetString(string section, string item, bool mandatory, params string[] path);

        bool UpdateValue(int userConfigKeyID, string environment, string newValue, string userName);

        T GetEnum<T>(string section, string item, bool mandatory, params string[] path) where T : struct, IConvertible;

        List<string> GetStrings(string section, string item, bool mandatory, char separator, params string[] path);

        List<T> GetEnums<T>(string section, string item, bool mandatory, char separator, params string[] path) where T : struct, IConvertible;
    }
}
