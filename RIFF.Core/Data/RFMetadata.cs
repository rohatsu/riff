// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace RIFF.Core
{
    /// <summary>
    /// Metadata can be attached to any catalog entry for storing non-economic data (performance, debug).
    /// </summary>
    [DataContract]
    public class RFMetadata
    {
        [DataMember]
        public Dictionary<string, string> Properties { get; set; }

        public RFMetadata()
        {
            Properties = new Dictionary<string, string>();
        }

        public static RFMetadata Deserialize(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return new RFMetadata();
            }
            var serializer = new DataContractSerializer(typeof(RFMetadata));
            var sw = new StringReader(xml);
            using (var reader = new XmlTextReader(sw))
            {
                return (RFMetadata)serializer.ReadObject(reader);
            }
        }

        public string Serialize()
        {
            if (Properties.Count == 0)
            {
                return null;
            }

            var serializer = new DataContractSerializer(typeof(RFMetadata));
            var sw = new StringWriter();
            using (var writer = new XmlTextWriter(sw))
            {
                writer.Formatting = Formatting.None;
                serializer.WriteObject(writer, this);
                writer.Flush();
                return sw.ToString();
            }
        }

        public void SetProperty(string property, string value)
        {
            if (!string.IsNullOrWhiteSpace(property))
            {
                if (Properties.ContainsKey(property))
                {
                    Properties.Remove(property);
                }
                Properties[property] = value;
            }
        }
    }
}
