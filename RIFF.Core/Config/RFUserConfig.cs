// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace RIFF.Core
{
    public class RFUserConfig : IRFUserConfig
    {
        protected static string SEPARATOR = ".";

        protected string _connectionString;
        protected string _environmentName;

        public RFUserConfig(string connectionString, string environmentName)
        {
            _connectionString = connectionString;
            _environmentName = environmentName;
        }

        public virtual List<RFUserConfigValue> GetAllValues()
        {
            var entries = new List<RFUserConfigValue>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string getConfigsSQL = "SELECT * FROM [RIFF].[UserConfigLatestView]";
                    using (var getConfigsCommand = new SqlCommand(getConfigsSQL, connection))
                    {
                        using (var reader = getConfigsCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                            {
                                foreach (DataRow dataRow in dataTable.Rows)
                                {
                                    try
                                    {
                                        entries.Add(new RFUserConfigValue
                                        {
                                            UserConfigKeyID = (int)dataRow["UserConfigKeyID"],
                                            Section = dataRow["Section"].ToString(),
                                            Item = dataRow["Item"].ToString(),
                                            Key = dataRow["Key"].ToString(),
                                            Description = dataRow["Description"].ToString(),
                                            UserConfigValueID = (int)dataRow["UserConfigValueID"],
                                            Environment = dataRow["Environment"].ToString(),
                                            Value = dataRow["Value"].ToString(),
                                            UpdateTime = (DateTimeOffset)dataRow["UpdateTime"],
                                            UpdateUser = dataRow["UpdateUser"].ToString()
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        RFStatic.Log.Exception(this, ex, "Error retrieving user config entry");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, ex, "Error retrieving user config values");
            }
            return entries;
        }

        public bool GetBool(string section, string item, bool mandatory, bool defaultValue, params string[] path)
        {
            var stringValue = GetString(section, item, mandatory, path);
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue.Trim().ToLower() == "true" || stringValue.Trim() == "1" || stringValue.Trim().ToLower() == "yes";
            }
            else if (mandatory)
            {
                throw new RFSystemException(this, "Can't find mandatory config entry {0}/{1}/{2}", section, item, String.Join("/", path));
            }
            else
            {
                return defaultValue;
            }
        }

        public decimal? GetDecimal(string section, string item, bool mandatory, decimal? defaultValue, params string[] path)
        {
            var stringValue = GetString(section, item, mandatory, path);
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                decimal d;
                if (Decimal.TryParse(stringValue, out d))
                {
                    return d;
                }
            }

            if (mandatory)
            {
                throw new RFSystemException(this, "Can't find valid mandatory config entry {0}/{1}/{2}", section, item, String.Join("/", path));
            }

            return defaultValue;
        }

        public T GetEnum<T>(string section, string item, bool mandatory, params string[] path) where T : struct, IConvertible
        {
            var stringValue = GetString(section, item, mandatory, path);
            if (stringValue.NotBlank())
            {
                return RFEnumHelpers.GetEnum<T>(stringValue, mandatory);
            }
            return default(T);
        }

        public List<T> GetEnums<T>(string section, string item, bool mandatory, char separator, params string[] path) where T : struct, IConvertible
        {
            var stringList = GetStrings(section, item, mandatory, separator, path);
            if (stringList != null)
            {
                return stringList.Select(s => RFEnumHelpers.GetEnum<T>(s, mandatory)).ToList();
            }
            return new List<T>();
        }

        public int? GetInt(string section, string item, bool mandatory, int? defaultValue, params string[] path)
        {
            var stringValue = GetString(section, item, mandatory, path);
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                int i;
                if (Int32.TryParse(stringValue, out i))
                {
                    return i;
                }
            }

            if (mandatory)
            {
                throw new RFSystemException(this, "Can't find mandatory config entry {0}/{1}/{2}", section, item, String.Join("/", path));
            }

            return defaultValue;
        }

        public virtual string GetString(string section, string item, bool mandatory, params string[] path)
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
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string getConfigsSQL = "SELECT * FROM [RIFF].[UserConfigLatestView] WHERE [Section] = @Section AND [Key] = @Key AND [Item] = @Item";
                    using (var getConfigsCommand = new SqlCommand(getConfigsSQL, connection))
                    {
                        getConfigsCommand.Parameters.AddWithValue("@Section", section);
                        getConfigsCommand.Parameters.AddWithValue("@Item", item);
                        getConfigsCommand.Parameters.AddWithValue("@Key", key);
                        using (var reader = getConfigsCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                            {
                                var entries = new Dictionary<string, string>();
                                foreach (DataRow dataRow in dataTable.Rows)
                                {
                                    try
                                    {
                                        var environment = dataRow["Environment"] != DBNull.Value ? dataRow["Environment"].ToString() : String.Empty;
                                        var value = dataRow["Value"] != DBNull.Value ? dataRow["Value"].ToString() : String.Empty;
                                        entries.Add(environment, value);
                                    }
                                    catch (Exception ex)
                                    {
                                        RFStatic.Log.Exception(this, ex, "Error retrieving user config entry for {0} / {1}", section, key);
                                    }
                                }
                                if (entries.ContainsKey(_environmentName))
                                {
                                    return entries[_environmentName];
                                }
                                else if (entries.ContainsKey(String.Empty))
                                {
                                    return entries[String.Empty];
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, ex, "Error retrieving config value for {0} / {1}.", section, key);
                throw; // this is bad
            }
            if (mandatory)
            {
                throw new RFSystemException(this, "Can't find mandatory config entry {0}/{1}/{2}", section, item, String.Join("/", path));
            }
            return null;
        }

        public List<string> GetStrings(string section, string item, bool mandatory, char separator, params string[] path)
        {
            var stringValue = GetString(section, item, mandatory, path);
            if (stringValue.NotBlank())
            {
                return stringValue.Split(separator).Where(s => s.NotBlank()).Select(s => s.Trim()).ToList();
            }
            return new List<string>();
        }

        public virtual bool UpdateValue(int userConfigKeyID, string environment, string newValue, string userName)
        {
            if (userConfigKeyID == 0)
            {
                throw new RFSystemException(this, "No config key ID specified");
            }
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int version = 1;
                            string getExistingSQL = "SELECT MAX([Version]) FROM [RIFF].[UserConfigValue] WHERE [UserConfigKeyID] = @UserConfigKeyID AND [Environment] = @Environment";
                            using (var getExistingCommand = new SqlCommand(getExistingSQL, connection, transaction))
                            {
                                getExistingCommand.Parameters.AddWithValue("@UserConfigKeyID", userConfigKeyID);
                                getExistingCommand.Parameters.AddWithValue("@Environment", environment);
                                var existingVersion = getExistingCommand.ExecuteScalar();
                                if (existingVersion != null && existingVersion != DBNull.Value)
                                {
                                    version = (int)existingVersion + 1;
                                }
                            }

                            string insertValueSQL = "INSERT INTO [RIFF].[UserConfigValue] ( UserConfigKeyID, Environment, Value, Version, UpdateTime, UpdateUser ) VALUES "
                                + "( @UserConfigKeyID, @Environment, @Value, @Version, @UpdateTime, @UpdateUser )";
                            using (var insertValueCommand = new SqlCommand(insertValueSQL, connection, transaction))
                            {
                                insertValueCommand.Parameters.AddWithValue("@UserConfigKeyID", userConfigKeyID);
                                insertValueCommand.Parameters.AddWithValue("@Environment", environment ?? String.Empty);
                                insertValueCommand.Parameters.AddWithValue("@Value", newValue);
                                insertValueCommand.Parameters.AddWithValue("@Version", version);
                                insertValueCommand.Parameters.AddWithValue("@UpdateTime", DateTimeOffset.Now);
                                insertValueCommand.Parameters.AddWithValue("@UpdateUser", userName);
                                var result = insertValueCommand.ExecuteNonQuery();
                                if (result != 1)
                                {
                                    throw new RFSystemException(this, "Error updating user config.");
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RFSystemException(this, ex, "Error updating user config for {0}/{1} with {2}", userConfigKeyID.ToString(), environment, newValue);
            }
            return true;
        }
    }

    public class RFUserConfigValue
    {
        public string Description { get; set; }

        public string Environment { get; set; }

        public string Item { get; set; }

        public string Key { get; set; }

        public string Section { get; set; }

        public DateTimeOffset UpdateTime { get; set; }

        public string UpdateUser { get; set; }

        public int UserConfigKeyID { get; set; }

        public int UserConfigValueID { get; set; }

        public string Value { get; set; }

        public int Version { get; set; }
    }
}
