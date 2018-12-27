// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;

namespace RIFF.Framework
{
    public class RFDataEditorActivity : RFActivity
    {
        public RFDataEditorActivity(IRFProcessingContext context, string userName) : base(context, userName)
        {
        }

        public RFDocument GetDocumentForDownload(string type, long keyReference)
        {
            var allKeys = Context.GetKeysByType(RFReflectionHelpers.GetTypeByFullName(type));
            if (allKeys.ContainsKey(keyReference))
            {
                return Context.LoadEntry(allKeys[keyReference]) as RFDocument;
            }
            return null;
        }

        public object GetDocumentForEdit(string type, long keyReference)
        {
            var entry = GetDocumentForDownload(type, keyReference);
            if (entry != null)
            {
                return new
                {
                    Key = RFXMLSerializer.PrettySerializeContract(entry.Key),
                    Content = RFXMLSerializer.PrettySerializeContract(entry.Content)
                };
            }
            return null;
        }

        public List<RFCatalogKeyMetadata> GetDocuments(string type, DateTime? startTime, DateTime? endTime, int limitResults, RFDate? valueDate, bool latestOnly)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                type = typeof(RFCatalogKey).FullName;
            }
            return Context.SearchKeys(RFReflectionHelpers.GetTypeByFullName(type), startTime, endTime, limitResults, valueDate, latestOnly);
        }

        public bool SaveDocument(string keyType, string contentType, string keyData, string contentData)
        {
            var key = RFXMLSerializer.DeserializeContract(keyType, keyData) as RFCatalogKey;
            var content = RFXMLSerializer.DeserializeContract(contentType, contentData);
            var metadata = new RFMetadata();
            metadata.Properties.Add("Creator", "webuser");
            Context.SaveDocument(key, content, false, CreateUserLogEntry("Save Document", String.Format("Updated document {0}", key.FriendlyString()), null)); // silent
            return true;
        }

        public bool UpdateDocument(string type, long keyReference, string data)
        {
            var allKeys = Context.GetKeysByType(RFReflectionHelpers.GetTypeByFullName(type));
            if (allKeys.ContainsKey(keyReference))
            {
                var existingDocument = Context.LoadEntry(allKeys[keyReference]) as RFDocument;
                existingDocument.Content = RFXMLSerializer.DeserializeContract(existingDocument.Type, data);
                Context.SaveDocument(existingDocument.Key, existingDocument.Content, false, CreateUserLogEntry("Update Document", String.Format("Saved document {0}", existingDocument.Key.FriendlyString()), null));
            }
            return true;
        }
    }
}
