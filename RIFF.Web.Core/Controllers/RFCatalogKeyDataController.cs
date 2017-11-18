// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using RIFF.Core;
using RIFF.Framework;
using RIFF.Web.Core.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Http;
#if (false)
using System.Web.OData;
using System.Web.OData.Query;

namespace RIFF.Web.Core.Controllers
{
    public class RFCatalogKeyData
    {
        public string ContentType { get; set; }

        public string ContentTypeFull { get; set; }

        public long DataSize { get; set; }

        public string FriendlyString { get; set; }

        public string Instance { get; set; }

        public bool IsValid { get; set; }

        public string Key { get; set; }

        [Key]
        public long KeyReference { get; set; }

        public string KeyType { get; set; }
        public string KeyTypeFull { get; set; }
        public string Metadata { get; set; }
        public string Plane { get; set; }
        public DateTimeOffset UpdateTime { get; set; }
        public string ValueDate { get; set; }
    }

    [RFControllerAuthorize(AccessLevel = RFAccessLevel.NotSet, Permission = null)]
    public class RFCatalogKeyDataController : ODataController
    {
        private static List<RFCatalogKeyData> _cache;
        private volatile static object _sync;

        static RFCatalogKeyDataController()
        {
            _cache = new List<RFCatalogKeyData>();
            _sync = new object();
        }

        public static List<RFCatalogKeyData> RefreshCache(IRFProcessingContext context, string username)
        {
            using (var dataEditor = new RFDataEditorActivity(context, username))
            {
                var cache = dataEditor.GetDocuments(null, null, null, 0, null, true).Select(d => new RFCatalogKeyData
                {
                    Key = JsonConvert.SerializeObject(d.Key),
                    FriendlyString = d.Key.FriendlyString(),
                    KeyReference = d.KeyReference,
                    KeyType = RFReflectionHelpers.TrimType(d.KeyType),
                    KeyTypeFull = d.KeyType,
                    Plane = d.Key.Plane.ToString(),
                    ContentType = RFReflectionHelpers.TrimType(d.ContentType),
                    ContentTypeFull = d.ContentType,
                    DataSize = d.DataSize,
                    Metadata = JsonConvert.SerializeObject(d.Metadata),
                    IsValid = d.IsValid,
                    UpdateTime = d.UpdateTime,
                    Instance = d.Key.GraphInstance != null ? d.Key.GraphInstance.Name : null,
                    ValueDate = (d.Key.GraphInstance != null && d.Key.GraphInstance.ValueDate.HasValue && d.Key.GraphInstance.ValueDate.Value != RFDate.NullDate) ?
                        d.Key.GraphInstance.ValueDate.Value.ToJavascript() : null
                }).ToList();
                lock (_sync)
                {
                    _cache = cache;
                }
                return cache;
            }
        }

        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
        public IHttpActionResult Get()
        {
            lock (_sync)
            {
                return Ok(_cache.AsQueryable());
            }
        }
    }
}
#endif