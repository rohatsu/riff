// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Linq;
using System.Reflection;

namespace RIFF.Framework
{
    public static class RFPropertyMapper
    {
        public static R CreateRow<R>(RFRawReportRow sourceRow) where R : RFDataRow, new()
        {
            var r = new R();

            foreach (var p in r.GetType().GetProperties())
            {
                var attr = GetMappedAttribute(p);
                if (attr != null)
                {
                    try
                    {
                        var t = p.PropertyType;
                        object v = null;
                        if (t.Equals(typeof(string)))
                        {
                            v = sourceRow.GetString(attr.SourceColumn);
                        }
                        else if (t.Equals(typeof(decimal?)) || t.Equals(typeof(decimal)))
                        {
                            v = sourceRow.GetDecimal(attr.SourceColumn);
                        }
                        else if (t.Equals(typeof(double?)) || t.Equals(typeof(double)))
                        {
                            v = sourceRow.GetDouble(attr.SourceColumn);
                        }
                        else if (t.Equals(typeof(float?)) || t.Equals(typeof(float)))
                        {
                            v = sourceRow.GetFloat(attr.SourceColumn);
                        }
                        else if (t.Equals(typeof(int?)) || t.Equals(typeof(int)))
                        {
                            v = sourceRow.GetInt(attr.SourceColumn);
                        }
                        else if (t.Equals(typeof(bool?)) || t.Equals(typeof(bool)))
                        {
                            v = sourceRow.GetBool(attr.SourceColumn);
                        }
                        else if (t.Equals(typeof(RFDate?)) || t.Equals(typeof(RFDate)))
                        {
                            v = sourceRow.GetDate(attr.SourceColumn, attr.Format);
                        }
                        else if (t.Equals(typeof(DateTime?)) || t.Equals(typeof(DateTime)))
                        {
                            v = sourceRow.GetDateTime(attr.SourceColumn, attr.Format);
                        }
                        else if (t.Equals(typeof(DateTimeOffset?)) || t.Equals(typeof(DateTimeOffset)))
                        {
                            v = sourceRow.GetDateTime(attr.SourceColumn, attr.Format);
                            if (v != null)
                            {
                                v = new DateTimeOffset((DateTime)v);
                            }
                        }

                        if (v == null)
                        {
                            if (attr.IsMandatory)
                            {
                                throw new RFLogicException(typeof(RFPropertyMapper), "Missing mandatory value in column {0}", attr.SourceColumn);
                            }
                        }
                        else
                        {
                            p.SetValue(r, v);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attr.IsMandatory)
                        {
                            throw new RFLogicException(typeof(RFPropertyMapper), ex, "Error parsing value in column {0}", attr.SourceColumn);
                        }
                        RFStatic.Log.Warning(typeof(RFPropertyMapper), "Error parsing value in column {0}", attr.SourceColumn);
                    }
                }
            }
            return r;
        }

        public static RFMappedPropertyAttribute GetMappedAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(RFMappedPropertyAttribute), true).FirstOrDefault() as RFMappedPropertyAttribute;
        }
    }

    /// <summary>
    /// Specifices a property can be mapped to a report.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RFMappedPropertyAttribute : Attribute
    {
        public string Format { get; set; }
        public bool IsMandatory { get; set; }
        public string SourceColumn { get; set; }

        public RFMappedPropertyAttribute(string sourceColumn, bool isMandatory = false, string format = null)
        {
            SourceColumn = sourceColumn;
            Format = format;
            IsMandatory = isMandatory;
        }
    }
}
