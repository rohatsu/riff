// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Formats.XLS;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFRawReportRow
    {
        [DataMember]
        public List<string> Values { get; set; }

        [NonSerialized]
        protected Dictionary<string, int> _columns;

        public RFRawReportRow()
        {
            Values = new List<string>();
            _columns = null;
        }

        public bool GetBool(string column, bool defaultValue = false)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return defaultValue;
                }
                else if (stringValue.ToUpper() == "TRUE" || stringValue.ToUpper() == "YES" || stringValue == "1" || stringValue.ToUpper() == "ON")
                {
                    return true;
                }
                else if (stringValue.ToUpper() == "FALSE" || stringValue.ToUpper() == "NO" || stringValue == "0" || stringValue.ToUpper() == "OFF")
                {
                    return false;
                }
                bool val = defaultValue;
                if (Boolean.TryParse(stringValue, out val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing bool value: ", ex.Message);
            }
            return defaultValue;
        }

        public RFDate? GetDate(string column, string format = null)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                DateTime val = DateTime.MinValue;
                if (DateTime.TryParseExact(stringValue, format ?? RFCore.sDateFormat, null, DateTimeStyles.AssumeLocal, out val))
                {
                    return new RFDate(val);
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing date: ", ex.Message);
            }
            return null;
        }

        public RFDate? GetDate(int colIndex, string format = null)
        {
            try
            {
                var stringValue = GetString(colIndex);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                DateTime val = DateTime.MinValue;
                if (DateTime.TryParseExact(stringValue, format ?? RFCore.sDateFormat, null, DateTimeStyles.AssumeLocal, out val))
                {
                    return new RFDate(val);
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing date: ", ex.Message);
            }
            return null;
        }

        public DateTime? GetDateTime(string column, string format, DateTimeStyles style = DateTimeStyles.None)
        {
            try
            {
                var stringValue = GetString(column);
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    return DateTime.ParseExact(stringValue, format, null, style);
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing date value: ", ex.Message);
            }
            return null;
        }

        public DateTime? GetDateTime(string column, DateTimeStyles styles = DateTimeStyles.None)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                DateTime val = DateTime.MinValue;
                if (DateTime.TryParseExact(stringValue, RFCore.sDateTimeFormat, null, styles, out val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing date: ", ex.Message);
            }
            return null;
        }

        public decimal? GetDecimal(int colIndex)
        {
            try
            {
                var stringValue = GetString(colIndex);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                return Decimal.Parse(stringValue.Trim(' ', Convert.ToChar(160)), NumberStyles.Any);
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing decimal value: ", ex.Message);
            }
            return null;
        }

        public decimal? GetDecimal(string column)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                decimal val = 0;
                if (Decimal.TryParse(stringValue.Trim(' ', Convert.ToChar(160)), NumberStyles.Any, null, out val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing decimal value: ", ex.Message);
            }
            return null;
        }

        public double? GetDouble(int colIndex)
        {
            try
            {
                var stringValue = GetString(colIndex);
                if (string.IsNullOrWhiteSpace(stringValue) || stringValue == "NaN")
                {
                    return null;
                }
                return Double.Parse(stringValue.Trim(' ', Convert.ToChar(160)), NumberStyles.Any);
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing double value: ", ex.Message);
            }
            return null;
        }

        public double? GetDouble(string column)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue) || stringValue == "NaN")
                {
                    return null;
                }
                double val = 0;
                if (Double.TryParse(stringValue.Trim(' ', Convert.ToChar(160)), NumberStyles.Any, null, out val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing double value: ", ex.Message);
            }
            return null;
        }

        public RFDate? GetExcelDate(string column)
        {
            try
            {
                var decimalValue = GetDecimal(column);
                if (decimalValue != null)
                {
                    return new RFDate(DateTime.FromOADate((double)decimalValue));
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing Excel date value: ", ex.Message);
            }
            return null;
        }

        public DateTime? GetExcelDateTime(string column)
        {
            try
            {
                var decimalValue = GetDecimal(column);
                if (decimalValue != null)
                {
                    return DateTime.FromOADate((double)decimalValue);
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing Excel datetime value: ", ex.Message);
            }
            return null;
        }

        public float? GetFloat(string column)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue) || stringValue == "NaN")
                {
                    return null;
                }
                float val = 0;
                if (float.TryParse(stringValue.Trim(' ', Convert.ToChar(160)), NumberStyles.Any, null, out val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing float value: ", ex.Message);
            }
            return null;
        }

        public int? GetInt(string column)
        {
            try
            {
                var stringValue = GetString(column);
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }
                int val = 0;
                if (Int32.TryParse(stringValue.Trim(' ', Convert.ToChar(160)), NumberStyles.Any, null, out val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "Error parsing integer value: ", ex.Message);
            }
            return null;
        }

        public int GetOutlineLevel()
        {
            return GetInt(XLSLoader.OUTLINE_LEVEL) ?? 0;
        }

        public string GetString(int colIndex)
        {
            if (colIndex < Values.Count)
            {
                return Values[colIndex];
            }
            return null;
        }

        public string GetString(string column)
        {
            if (_columns.ContainsKey(column))
            {
                int index = _columns[column];
                if (index < Values.Count)
                {
                    return Values[index];
                }
            }
            return null;
        }

        public void SetColumns(Dictionary<string, int> columns)
        {
            _columns = columns;
        }

        public void SetString(string column, string value)
        {
            if (_columns.ContainsKey(column))
            {
                int index = _columns[column];
                if (index < Values.Count)
                {
                    Values[index] = value;
                }
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dic = new Dictionary<string, string>();
            int i = 0;
            foreach (var v in Values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                {
                    string colName;
                    if (_columns != null)
                    {
                        colName = _columns.FirstOrDefault(c => c.Value == i).Key;
                        if (string.IsNullOrWhiteSpace(colName))
                        {
                            colName = "Col" + i;
                        }
                    }
                    else
                    {
                        colName = "Col" + i;
                    }
                    if (!dic.ContainsKey(v))
                    {
                        dic.Add(colName.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim(), v.Trim());
                    }
                }
                i++;
            }
            return dic;
        }
    }
}
