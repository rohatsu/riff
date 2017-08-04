// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json.Linq;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RIFF.Interfaces.Formats.JSON
{
    public static class JSONGenerator
    {
        public static string ExportToJSON(IRFDataSet dataSet, List<KeyValuePair<string, Type>> columnTypes)
        {
            var array = new JArray();

            // header row
            var rowType = dataSet.GetRowType();
            foreach (var r in dataSet.GetRows())
            {
                var obj = new JObject();
                foreach (var propertyInfo in rowType.GetProperties())
                {
                    if (RFReflectionHelpers.IsStruct(propertyInfo) || RFReflectionHelpers.IsMappingKey(propertyInfo))
                    {
                        var valueStruct = propertyInfo.GetValue(r);
                        foreach (var innerPropertyInfo in propertyInfo.PropertyType.GetProperties())
                        {
                            if (valueStruct != null)
                            {
                                var columnName = String.Format("{0},{1}", propertyInfo.Name, innerPropertyInfo.Name);
                                obj.Add(new JProperty(
                                    columnName,
                                    GetValue(columnName, innerPropertyInfo.GetValue(valueStruct), columnTypes)));
                            }
                        }
                    }
                    else
                    {
                        var columnName = propertyInfo.Name;
                        obj.Add(new JProperty(
                            columnName,
                            GetValue(columnName, propertyInfo.GetValue(r), columnTypes)));
                    }
                }
                array.Add(obj);
            }

            return array.ToString();
        }

        public static object GetValue(string columnName, object value, List<KeyValuePair<string, Type>> columnTypes)
        {
            if (value != null)
            {
                if (columnTypes.All(c => c.Key != columnName))
                {
                    columnTypes.Add(new KeyValuePair<string, Type>(columnName, value.GetType()));
                }

                if (value is RFDate?)
                {
                    return (value as RFDate?).Value.ToString("yyyy-MMM-dd");
                }
                else if (value is RFDate)
                {
                    return ((RFDate)value).ToString("yyyy-MMM-dd");
                }
                else if (value is RFEnum)
                {
                    return value.ToString();
                }
                else if (value is Enum)
                {
                    return value.ToString();
                }
                else if (RFReflectionHelpers.IsNumber(value.GetType()))
                {
                    return value;
                }

                return value.ToString();
            }
            return String.Empty;
        }
    }
}
