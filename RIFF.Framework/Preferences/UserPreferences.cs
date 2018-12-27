// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Framework.Preferences
{
    public static class UserPreferences
    {
        public static bool IsPresentationMode(IRFProcessingContext context, string username)
        {
            username = username.ToLower();
            var preferences = GetPresentationPreferences(context);
            return preferences.Active.Contains(username);
        }

        public static bool SetPresentationMode(IRFProcessingContext context, string username, bool active)
        {
            username = username.ToLower();
            var preferences = GetPresentationPreferences(context);
            if (active && !preferences.Active.Contains(username))
            {
                preferences.Active.Add(username);
            }
            else if (!active && preferences.Active.Contains(username))
            {
                preferences.Active.Remove(username);
            }
            context.SaveDocument(PresentationKey(), preferences, false);
            return true;
        }

        private static PresentationPreferences GetPresentationPreferences(IRFProcessingContext context)
        {
            var presentationKey = PresentationKey();
            var preferences = context.LoadDocumentContent<PresentationPreferences>(presentationKey);
            return preferences ?? new PresentationPreferences();
        }

        private static RFCatalogKey PresentationKey()
        {
            return new RFGenericCatalogKey
            {
                GraphInstance = null,
                Name = "Presentation Mode",
                Plane = RFPlane.System,
                Root = "sys",
                StoreType = RFStoreType.Document
            };
        }
    }

    [DataContract]
    public class PresentationPreferences
    {
        [DataMember]
        public List<string> Active { get; set; }

        public PresentationPreferences()
        {
            Active = new List<string>();
        }
    }
}
