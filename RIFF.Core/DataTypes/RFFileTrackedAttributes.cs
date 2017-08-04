// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFFileTrackedAttributes
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public long FileSize { get; set; }

        [DataMember]
        public string FullPath { get; set; }

        [DataMember]
        public DateTime ModifiedDate { get; set; }

        // MD5?

        public override bool Equals(object obj)
        {
            var other = obj as RFFileTrackedAttributes;
            return (FileName.Equals(other.FileName) && Math.Abs((ModifiedDate - other.ModifiedDate).TotalSeconds) <= 1 && FileSize == other.FileSize
                && (string.IsNullOrWhiteSpace(FullPath) || string.IsNullOrWhiteSpace(other.FullPath) || FullPath == other.FullPath)); // compare full path only if both present
        }

        public override int GetHashCode()
        {
            return (int)ModifiedDate.ToOADate();
        }
    }
}
