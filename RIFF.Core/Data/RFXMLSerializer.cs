// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace RIFF.Core
{
    /// <summary>
    /// Mark class to be cached for serialization at startup
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class RFCacheSerializer : Attribute
    {
    }

    public class RFXMLSerializer
    {
        protected static List<Type> mPotentialTypes = new List<Type>();
        protected static object sCacheLock = new object();
        protected static Dictionary<string, DataContractSerializer> sSerializerCache = new Dictionary<string, DataContractSerializer>();
        protected static SortedSet<string> sTypeAssemblies = new SortedSet<string> { "RIFF.Core", "RIFF.Framework", "RIFF.Extensions" };

        public static string BinaryDeserializeXML(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }
            var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(data, XmlDictionaryReaderQuotas.Max);
            binaryDictionaryReader.Read();
            return binaryDictionaryReader.ReadOuterXml();
        }

        public static byte[] BinarySerializeXML(string xml)
        {
            if (xml.IsBlank())
            {
                return null;
            }

            using (var stream2 = new MemoryStream())
            {
                XmlDictionaryWriter binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream2);

                using (var xmlReader = XmlReader.Create(new StringReader(xml)))
                {
                    binaryDictionaryWriter.WriteNode(xmlReader, false);
                    binaryDictionaryWriter.Flush();
                    return stream2.ToArray();
                }
            }
        }

        public static object BinaryDeserializeContract(string type, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }
            if (type == typeof(RFXMLObject).FullName)
            {
                return new RFXMLObject(BinaryDeserializeXML(data));
            }
            var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(data, XmlDictionaryReaderQuotas.Max);
            var serializer = GetOrCreateSerializer(type);

            return serializer.ReadObject(binaryDictionaryReader, false);
        }

        public static byte[] BinarySerializeContract(object o)
        {
            if (o == null)
            {
                return new byte[0];
            }
            if (o.GetType() == typeof(RFXMLObject))
            {
                return BinarySerializeXML((o as RFXMLObject).Xml);
            }
            var serializer = GetOrCreateSerializer(o.GetType());

            using (var stream2 = new MemoryStream())
            {
                XmlDictionaryWriter binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream2);
                serializer.WriteObject(binaryDictionaryWriter, o);
                binaryDictionaryWriter.Flush();
                return stream2.ToArray();
            }
        }

        public static object DeserializeContract(DataContractSerializer serializer, string xml)
        {
            if (xml.IsBlank())
            {
                return null;
            }
            var sw = new StringReader(xml);
            using (var reader = new XmlTextReader(sw))
            {
                return serializer.ReadObject(reader);
            }
        }

        public static object DeserializeContract(string type, string xml)
        {
            return DeserializeContract(GetOrCreateSerializer(type), xml);
        }

        public static object DeserializeContract(Type type, string xml)
        {
            return DeserializeContract(GetOrCreateSerializer(type), xml);
        }

        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            return mPotentialTypes;
        }

        public static void Initialize(IEnumerable<string> assemblyNames = null)
        {
            if (assemblyNames != null)
            {
                foreach (var assembly in assemblyNames)
                {
                    if (assembly.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sTypeAssemblies.Add(assembly.Substring(0, assembly.Length - 4));
                    }
                    else
                    {
                        sTypeAssemblies.Add(assembly);
                    }
                }
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => sTypeAssemblies.Contains(a.GetName().Name)))
            {
                mPotentialTypes.AddRange(assembly.GetTypes().Where(t => (t.IsPublic || t.IsNestedPublic) && !t.IsInterface && !t.IsAbstract &&
                    (t.GetCustomAttribute(typeof(DataContractAttribute)) != null || t.GetCustomAttribute(typeof(RFCacheSerializer)) != null)));
            }

            // pre-create serializers
            lock (sCacheLock)
            {
                foreach (var pt in mPotentialTypes)
                {
                    GetOrCreateSerializer(pt, true);
                }

                // system types
                GetOrCreateSerializer(typeof(string[]), true);
                GetOrCreateSerializer(typeof(string), true);
                GetOrCreateSerializer(typeof(RFDate), true);
                GetOrCreateSerializer(typeof(RFWorkQueueItem), true);
                GetOrCreateSerializer(typeof(RFInstruction), true);
                GetOrCreateSerializer(typeof(RFProcessInstruction), true);
                GetOrCreateSerializer(typeof(RFParamProcessInstruction), true);
                GetOrCreateSerializer(typeof(RFIntervalInstruction), true);
                GetOrCreateSerializer(typeof(RFGraphProcessInstruction), true);
                GetOrCreateSerializer(typeof(RFGraphInstance), true);
                GetOrCreateSerializer(typeof(RFEngineProcessorKeyParam), true);
                GetOrCreateSerializer(typeof(RFEngineProcessorGraphInstanceParam), true);
                GetOrCreateSerializer(typeof(RFEvent), true);
                GetOrCreateSerializer(typeof(RFCatalogUpdateEvent), true);
                GetOrCreateSerializer(typeof(RFIntervalEvent), true);
                GetOrCreateSerializer(typeof(RFProcessingFinishedEvent), true);
            }

            RFStatic.Log.Info(typeof(RFXMLSerializer), "Cached {0} serializers.", sSerializerCache.Count);
        }

        public static string PrettySerializeContract(object o)
        {
            if (o == null)
            {
                return String.Empty;
            }
            var serializer = GetOrCreateSerializer(o.GetType());
            var sw = new StringWriter();
            using (var writer = new XmlTextWriter(sw))
            {
                writer.Formatting = System.Xml.Formatting.Indented;
                serializer.WriteStartObject(writer, o);
                writer.WriteAttributeString("xmlns", "c", null, "RIFF.Core");
                serializer.WriteObjectContent(writer, o);
                serializer.WriteEndObject(writer);
                writer.Flush();
                return sw.ToString();
            }
        }

        public static string SerializeContract(object o)
        {
            if (o == null)
            {
                return String.Empty;
            }
            var serializer = GetOrCreateSerializer(o.GetType());
            StringWriter sw = new StringWriter();
            using (var writer = new XmlTextWriter(sw))
            {
                writer.Formatting = System.Xml.Formatting.None;

                serializer.WriteStartObject(writer, o);
                writer.WriteAttributeString("xmlns", "c", null, "RIFF.Core");
                serializer.WriteObjectContent(writer, o);
                serializer.WriteEndObject(writer);
                writer.Flush();
            }
            return sw.ToString();
        }

        protected static DataContractSerializer CreateDeserializer(Type type)
        {
            return new DataContractSerializer(type, mPotentialTypes);
        }

        protected static DataContractSerializer GetOrCreateSerializer(Type type, bool isCaching = false)
        {
            return GetOrCreateSerializer(type.FullName, isCaching, type);
        }

        protected static DataContractSerializer GetOrCreateSerializer(string typeName, bool isCaching = false, Type nonCachedType = null)
        {
#if (DEBUG)
            const bool debug = true;
#else
            const bool debug = false;
#endif
            lock (sCacheLock)
            {
                DataContractSerializer serializer = null;
                if (!sSerializerCache.TryGetValue(typeName, out serializer))
                {
                    if (!isCaching && !debug)
                    {
                        RFStatic.Log.Warning(typeof(RFXMLSerializer), "Serializer cache miss: no serializer for type {0}", typeName);
                    }

                    try
                    {
                        var type = RFReflectionHelpers.GetTypeByFullName(typeName) ?? nonCachedType;
                        if (!isCaching || !debug)
                        {
                            serializer = CreateDeserializer(type);
                            sSerializerCache.Add(typeName, serializer);
                        }
                        if (!mPotentialTypes.Contains(type))
                        {
                            mPotentialTypes.Add(type);
                        }
                    }
                    catch (Exception ex)
                    {
                        RFStatic.Log.Exception(typeof(RFXMLSerializer), ex, "Unable to create serializer for {0}", typeName);
                    }
                }
                return serializer;
            }
        }
    }
}
