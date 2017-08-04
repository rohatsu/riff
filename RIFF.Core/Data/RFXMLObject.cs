// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Class for storing generic objects as XML (use for external types)
    /// </summary>
    [DataContract]
    public class RFXMLObject
    {
        [DataMember]
        public string Xml { get; set; }

        public RFXMLObject(object o)
        {
            Xml = RFXMLSerializer.PrettySerializeContract(o);
        }

        public RFXMLObject(string xml)
        {
            Xml = xml;
        }
    }
}
