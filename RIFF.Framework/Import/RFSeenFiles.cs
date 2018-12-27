// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFSeenFiles
    {
        [DataMember]
        public Dictionary<string, List<RFFileTrackedAttributes>> SeenAttributes { get; set; }

        public RFSeenFiles()
        {
            SeenAttributes = new Dictionary<string, List<RFFileTrackedAttributes>>();
        }

        public static bool IsExpired(RFFileTrackedAttributes attributes, DateTime utcNow, decimal? maxAge)
        {
            if (maxAge.HasValue)
            {
                return (decimal)(utcNow - attributes.ModifiedDate).TotalHours > maxAge;
            }
            return false;
        }

        public void CleanUpMaxAge(DateTime utcNow, decimal? maxAge)
        {
            if (maxAge.HasValue)
            {
                foreach (var fileEntry in SeenAttributes)
                {
                    var toRemove = fileEntry.Value.Where(e => IsExpired(e, utcNow, maxAge)).ToList();
                    if (toRemove.Count > 0)
                    {
                        foreach (var tr in toRemove)
                        {
                            RFStatic.Log.Debug(this, "Removing max aged entry: {0}", RFXMLSerializer.SerializeContract(tr));
                        }
                    }
                    foreach (var remove in toRemove)
                    {
                        fileEntry.Value.Remove(remove);
                    }
                }
            }
        }

        public bool HaveSeenFile(string fileKey, RFFileTrackedAttributes attributes)
        {
            if (SeenAttributes.ContainsKey(fileKey))
            {
                var seenAttributes = SeenAttributes[fileKey];
                var haveSeen = seenAttributes.Any(a => a.Equals(attributes));
                if (!haveSeen)
                {
                    RFStatic.Log.Debug(this, "File {0} ({1}) is new. None of existing {2} have-seens match:", fileKey, RFXMLSerializer.SerializeContract(attributes), seenAttributes.Count);
                    /*foreach(var a in seenAttributes)
                    {
                        RFStatic.Log.Debug(this, RFXMLSerializer.SerializeContract(a));
                    }*/
                }
                return haveSeen;
            }
            else
            {
                RFStatic.Log.Debug(this, "No seen files entries for file {0}", fileKey);
            }
            return false;
        }

        public void MarkSeenFile(string fileKey, RFFileTrackedAttributes attributes)
        {
            if (SeenAttributes.ContainsKey(fileKey))
            {
                var seenEntry = SeenAttributes[fileKey];
                seenEntry.Add(attributes);
            }
            else
            {
                SeenAttributes.Add(fileKey, new List<RFFileTrackedAttributes>() { attributes });
            }
        }
    }
}
