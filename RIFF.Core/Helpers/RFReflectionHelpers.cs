// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RIFF.Core
{
    public static class RFReflectionHelpers
    {
        public static Type MAPPING_KEY_TYPE;

        static RFReflectionHelpers()
        {
            MAPPING_KEY_TYPE = GetTypeByFullName("RIFF.Framework.RFMappingKey");
        }

        public static void CopyProperties(object sourceObject, object destinationObject)
        {
            if (sourceObject != null && destinationObject != null)
            {
                var destinationProperties = destinationObject.GetType().GetProperties();
                foreach (var propertyInfo in sourceObject.GetType().GetProperties())
                {
                    var derivedProperty = destinationProperties.FirstOrDefault(p => p.Name == propertyInfo.Name);
                    if (derivedProperty != null && derivedProperty.PropertyType == propertyInfo.PropertyType && propertyInfo.GetSetMethod() != null)
                    {
                        try
                        {
                            derivedProperty.SetValue(destinationObject, propertyInfo.GetValue(sourceObject));
                        }
                        catch (Exception ex)
                        {
                            RFStatic.Log.Warning(typeof(RFReflectionHelpers), "Error copying properties on type {0}: {1}", sourceObject.GetType().Name, ex.Message);
                        }
                    }
                }
            }
        }

        public static string FullName(this PropertyInfo property)
        {
            return String.Format("{0}.{1}", property.DeclaringType.Name, property.Name);
        }

        public static RFDateBehaviour GetDateBehaviour(PropertyInfo property)
        {
            var attribute = property.GetCustomAttributes(typeof(RFDateBehaviourAttribute), true).FirstOrDefault() as RFDateBehaviourAttribute;
            if (attribute != null)
            {
                return attribute.DateBehaviour;
            }
            else
            {
                return RFDateBehaviour.NotSet;
            }
        }

        public static RFIOBehaviour GetIOBehaviour(PropertyInfo property)
        {
            var attribute = property.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute;
            if (attribute != null)
            {
                return attribute.IOBehaviour;
            }
            else
            {
                return RFIOBehaviour.NotSet;
            }
        }

        public static PropertyInfo GetProperty<IO>(this Expression<Func<IO, object>> propertyExpression)
        {
            var memberName = GetPropertyName<IO>(propertyExpression);
            return typeof(IO).GetProperty(memberName);
        }

        public static string GetPropertyName<IO>(this Expression<Func<IO, object>> propertyExpression)
        {
            var memberName = String.Empty;
            if (propertyExpression.Body is ConstantExpression)
            {
                memberName = (propertyExpression.Body as ConstantExpression).Value.ToString();
            }
            else if (propertyExpression.Body is MemberExpression)
            {
                memberName = (propertyExpression.Body as MemberExpression).Member.Name;
            }
            else if (propertyExpression.Body is UnaryExpression)
            {
                var expression = (propertyExpression.Body as UnaryExpression).Operand as System.Linq.Expressions.MemberExpression;
                memberName = expression.Member.Name;
            }
            return memberName;
        }

        public static Type GetTypeByFullName(string type)
        {
            foreach (var t in RFXMLSerializer.GetKnownTypes(null))
            {
                if(t.FullName == type)
                {
                    return t;
                }
            }
            /*
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!a.FullName.Contains("IKVM"))
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (t.FullName == type)
                        {
                            return t;
                        }
                    }
                }
            }*/
            return Type.GetType(type);
        }

        public static bool IsDecimalDouble(Type t)
        {
            return t.Equals(typeof(decimal)) || t.Equals(typeof(double));
        }

        public static bool IsDictionary(PropertyInfo p)
        {
            return p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public static bool IsMandatory(PropertyInfo property)
        {
            var attribute = property.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute;
            if (attribute != null)
            {
                return attribute.IsMandatory;
            }
            else
            {
                return false;
            }
        }

        public static bool IsMappingKey(PropertyInfo p)
        {
            return p.PropertyType.IsSubclassOf(MAPPING_KEY_TYPE);
        }

        public static bool IsNumber(Type t)
        {
            return IsDecimalDouble(t) || t.Equals(typeof(int));
        }

        public static bool IsStruct(PropertyInfo p)
        {
            return p.PropertyType.IsValueType && !p.PropertyType.IsPrimitive && !p.PropertyType.IsEnum && !p.PropertyType.Namespace.StartsWith("System", StringComparison.OrdinalIgnoreCase) && p.PropertyType.Name != "RFDate";
        }

        public static string TrimType(string fullName)
        {
            if (fullName.Contains('.'))
            {
                return fullName.Substring(fullName.LastIndexOf('.') + 1);
            }
            return fullName;
        }
    }
}
