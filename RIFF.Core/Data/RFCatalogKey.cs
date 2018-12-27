// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public enum RFStoreType
    {
        [EnumMember]
        Document = 1,
    }

    /// <summary>
    /// Parent class representing a key uniquely identifying any catalog entry that will be serialized.
    /// </summary>
    [DataContract]
    public abstract class RFCatalogKey : IComparable, IRFGraphKey
    {
        [DataMember(EmitDefaultValue = false)]
        [XmlAttribute("g")]
        public RFGraphInstance GraphInstance { get; set; }

        [DataMember]
        [XmlAttribute("p")]
        public RFPlane Plane { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [XmlAttribute("r")]
        public string Root { get; set; }

        [DataMember]
        [XmlAttribute("s")]
        public RFStoreType StoreType { get; set; }

        protected RFCatalogKey()
        {
            GraphInstance = new RFGraphInstance();
        }

        public int CompareTo(object obj)
        {
            if (obj is RFCatalogKey)
            {
                return this.CompareTo((obj as RFCatalogKey).ToString());
            }
            return -1;
        }

        public RFCatalogKey CreateForInstance(RFGraphInstance instance)
        {
            var copy = MemberwiseClone() as RFCatalogKey;
            copy.GraphInstance = instance != null ? instance.Clone() : null;
            return copy;
        }

        public override bool Equals(object obj)
        {
            if (obj is RFCatalogKey)
            {
                return (obj as RFCatalogKey).ToString().Equals(ToString());
            }
            return false;
        }

        public abstract string FriendlyString();

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public RFGraphInstance GetInstance()
        {
            return GraphInstance ?? new RFGraphInstance();
        }

        public virtual bool MatchesRoot(RFCatalogKey key)
        {
            return key != null && RootString() == key.RootString();
        }

        public RFCatalogKey RootKey()
        {
            return CreateForInstance(null);
        }

        public override string ToString()
        {
            return RFXMLSerializer.SerializeContract(this);
        }

        private string RootString()
        {
            return RFXMLSerializer.SerializeContract(RootKey());
        }
    }

    /// <summary>
    /// Encapsulate data item keys for type-checking
    /// </summary>
    /// <typeparam name="T">Path in created keys</typeparam>
    public abstract class RFCatalogKeySet<T> : IRFCatalogKeySet where T : class
    {
    }

    /// <summary>
    /// Generic key based on a path and name where additional specialization is not necessary.
    /// </summary>
    [DataContract]
    public class RFGenericCatalogKey : RFCatalogKey
    {
        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Path { get; set; }

        [IgnoreDataMember]
        public static readonly string PATH_SEPARATOR = "/";

        public static RFGenericCatalogKey Create(RFKeyDomain keyDomain, string path, string name, RFGraphInstance instance)
        {
            return keyDomain.Associate(new RFGenericCatalogKey
            {
                GraphInstance = instance,
                Name = name,
                Path = path,
                Plane = RFPlane.User,
                StoreType = RFStoreType.Document
            });
        }

        public static RFGenericCatalogKey Create(RFKeyDomain keyDomain, string path, RFEnum _enum, RFGraphInstance instance)
        {
            return keyDomain.Associate(new RFGenericCatalogKey
            {
                GraphInstance = instance,
                Name = _enum.ToString(),
                Path = path,
                Plane = RFPlane.User,
                StoreType = RFStoreType.Document
            });
        }

        public override string FriendlyString()
        {
            return string.Format("{0}/{1}", Path, Name);
        }
    }

    // do not use
    public interface IRFCatalogKeySet
    { }

    public interface IRFGraphKey
    {
        RFGraphInstance GetInstance();

        bool MatchesRoot(RFCatalogKey key);
    }
}
