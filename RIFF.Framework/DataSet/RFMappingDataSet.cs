// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public abstract class RFMappingDataRow<K> : RFDataRow, IRFMappingDataRow where K : RFMappingKey
    {
        [DataMember]
        public K Key { get; set; }

        public abstract bool IsMissing();

        public bool IsValid()
        {
            return Key.IsValid();
        }

        public virtual string RowEncryptionKeyID()
        {
            return String.Join(".", typeof(K).Name, Key.ToString());
        }
    }

    [DataContract]
    public abstract class RFMappingDataSet<K, R> : RFDataSet<R> where R : RFMappingDataRow<K>, new() where K : RFMappingKey
    {
        [IgnoreDataMember]
        private Dictionary<K, R> _cache;

        public object Clone() // won't copy rows
        {
            return MemberwiseClone();
        }

        public bool TryGetMapping(K key, out R val)
        {
            val = GetMapping(key);
            return val != null;
        }

        public R GetMapping(K key)
        {
            if (key == null)
            {
                return null;
            }
            if (_cache == null)
            {
                BuildCache();
            }
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }
            return Rows.SingleOrDefault(r => r.Key == key);
        }

        public R GetOrCreateMapping(K key)
        {
            if (key == null)
            {
                throw new RFSystemException(this, "Empty key mapped to mapping data set.");
            }
            if (_cache != null && _cache.ContainsKey(key))
            {
                return _cache[key];
            }
            var row = Rows.SingleOrDefault(r => r.Key == key);
            if (row == null)
            {
                row = new R
                {
                    Key = key
                };
                Rows.Add(row);
            }
            return row;
        }

        public bool HasMapping(K key)
        {
            return GetMapping(key) != null;
        }

        public bool HasMissing()
        {
            return Rows.Any(r => r.IsMissing());
        }

        [OnDeserialized]
        private void PostDeserialize(StreamingContext ctx)
        {
            BuildCache();
        }

        public void Remove(K key)
        {
            Rows.RemoveAll(r => r.Key == key);
            BuildCache();
        }

        public void Replace(R row)
        {
            Remove(row.Key);
            Rows.Add(row);
            BuildCache();
        }

        protected void BuildCache()
        {
            _cache = Rows.ToDictionary(r => r.Key, r => r) ?? new Dictionary<K, R>();
        }
    }

    [DataContract]
    public abstract class RFMappingKey : IEquatable<RFMappingKey>, IComparable, IComparable<RFMappingKey>
    {
        private string _cachedComparisonString;

        public static bool operator !=(RFMappingKey left, RFMappingKey right)
        {
            return !(left == right);
        }

        public static bool operator ==(RFMappingKey left, RFMappingKey right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public RFMappingKey Clone()
        {
            return MemberwiseClone() as RFMappingKey;
        }

        public int CompareTo(object obj)
        {
            if (obj is RFMappingKey)
            {
                return string.Compare((obj as RFMappingKey).ComparisonString(), ComparisonString(), StringComparison.Ordinal);
            }
            return -1;
        }

        public int CompareTo(RFMappingKey other)
        {
            return string.Compare(other.ComparisonString(), ComparisonString(), StringComparison.Ordinal);
        }

        public abstract object[] ComparisonFields();

        public bool Equals(RFMappingKey other)
        {
            return other != null && ComparisonString() == other.ComparisonString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RFMappingKey);
        }

        public override int GetHashCode()
        {
            return ComparisonString().GetHashCode();
        }

        public abstract bool IsValid();

        public override string ToString()
        {
            return ComparisonString();
        }

        private string ComparisonString()
        {
            if (_cachedComparisonString == null)
            {
                _cachedComparisonString = String.Join(",", ComparisonFields());
            }
            return _cachedComparisonString;
        }
    }

    [DataContract]
    public class RFStringKey : RFMappingKey
    {
        [DataMember]
        public string Key { get; set; }

        public RFStringKey(string s)
        {
            Key = s;
        }

        public override object[] ComparisonFields()
        {
            return new object[] { Key };
        }

        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Key);
        }
    }

    public interface IRFMappingDataRow
    {
        bool IsMissing();

        bool IsValid();
    }
}
