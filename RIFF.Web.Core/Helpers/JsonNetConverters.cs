// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using RIFF.Core;
using System;
using System.Globalization;

namespace RIFF.Web.Core.Helpers
{
    public class RFDateJsonConverter : JsonConverter
    {
        // Summary: Gets a value indicating whether this Newtonsoft.Json.JsonConverter can write JSON.
        public override bool CanWrite { get { return true; } }

        public RFDateJsonConverter()
        {
        }

        // Summary: Determines whether this instance can convert the specified object type.
        //
        // Parameters: objectType: Type of the object.
        //
        // Returns: true if this instance can convert the specified object type; otherwise, false.
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RFDate);
        }

        // Summary: Reads the JSON representation of the object.
        //
        // Parameters: reader: The Newtonsoft.Json.JsonReader to read from.
        //
        // objectType: Type of the object.
        //
        // existingValue: The existing value of object being read.
        //
        // serializer: The calling serializer.
        //
        // Returns: The object value.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return RFDate.Parse(reader.ReadAsString());
        }

        // Summary: Writes the JSON representation of the object.
        //
        // Parameters: writer: The Newtonsoft.Json.JsonWriter to write to.
        //
        // value: The value.
        //
        // serializer: The calling serializer.
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((RFDate)value).ToString());
        }
    }

    public class SimpleEnumConverter : JsonConverter
    {
        /// <summary>
        /// Custom class based off StringEnumConverter so that Enums are serialized using .ToString() and ignore EnumMember(Value) - to match ASP MVC binder
        /// </summary>
        public SimpleEnumConverter()
        {
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Enum e = (Enum)value;

            writer.WriteValue(e.ToString());
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            bool isNullable = IsNullableType(objectType);
            Type t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;

            try
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string enumText = reader.Value.ToString();

                    return Enum.Parse(objectType, enumText, true);
                }

                if (reader.TokenType == JsonToken.Integer)
                {
                    return Enum.Parse(objectType, reader.Value.ToString(), true);
                }
            }
            catch (Exception)
            {
                throw;
            }

            throw new RFSystemException(this, "This shouldn't happen.");
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            Type t = (IsNullableType(objectType))
                ? Nullable.GetUnderlyingType(objectType)
                : objectType;

            return t.IsEnum;
        }

        public static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
