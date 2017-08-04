// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Base for catalog entries of all types.
    /// </summary>
    [DataContract]
    public abstract class RFCatalogEntry
    {
        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public RFCatalogKey Key { get; set; }

        [DataMember]
        public RFMetadata Metadata { get; set; }

        [DataMember]
        public DateTimeOffset UpdateTime { get; set; }

        [DataMember]
        public int Version { get; set; }

        protected RFCatalogEntry()
        {
            Metadata = new RFMetadata();
            Version = 0;
            UpdateTime = new DateTimeOffset(DateTime.Now);
            IsValid = true;
        }

        public abstract bool HasContent();

        public void SetInvalid()
        {
            IsValid = false;
        }
    }

    public class RFCatalogEntryStats
    {
        public bool IsValid { get; set; }

        public long KeyReference { get; set; }

        public DateTimeOffset? UpdateTime { get; set; }
    }
}
