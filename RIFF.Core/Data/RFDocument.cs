// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Class representing a generic object.
    /// </summary>
    [DataContract]
    [KnownType("GetKnownTypes")]
    public class RFDocument : RFCatalogEntry
    {
        [DataMember]
        public object Content { get; set; }

        [DataMember]
        public string Type { get; set; }

        public static RFDocument Create(RFCatalogKey key, object content, RFMetadata metadata = null)
        {
            return new RFDocument
            {
                Key = key,
                Content = content,
                Type = content.GetType().FullName,
                Metadata = metadata
            };
        }

        public static IEnumerable<Type> GetKnownTypes()
        {
            return RFXMLSerializer.GetKnownTypes(null);
        }

        public T GetContent<T>() where T : class
        {
            return (T)Content;
        }

        public override bool HasContent()
        {
            return Content != null;
        }

        public void SetContent<T>(T content) where T : class
        {
            Content = content;
        }
    }
}
