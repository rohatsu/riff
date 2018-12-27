// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using RIFF.Core;
using RIFF.Core.Data;
using RIFF.Framework;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models.Mirror;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    [RFControllerAuthorize(AccessLevel = RFAccessLevel.NotSet, Permission = null)]
    public class MirrorController : RIFFController
    {
        public MirrorController(IRFProcessingContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
        }

        public ActionResult Index()
        {
            using (var activity = new RFMirrorActivity(Context, Username))
            {
                return View(new IndexModel
                {
                    Sources = JsonConvert.SerializeObject(new object[] { new
                    {
                        id = "0",
                        name = "All Sources",
                        expanded = true,
                        type = "all",
                        items = activity.GetMirrorSources().Select(m => new
                        {
                            id = m.Name,
                            name = m.Name,
                            type = "source",
                            expanded = true,
                            items = m.SiteEnums.Select(s => new
                            {
                                id = s.Enum,
                                name = s.Enum,
                                type = "site"
                            })
                        })
                    }})
                });
            }
        }

        [RFHandleJsonError]
        [HttpGet]
        public JsonResult GetFiles(string name, string type)
        {
            using (var activity = new RFMirrorActivity(Context, Username))
            {
                IEnumerable<string> sites = null;
                switch (type)
                {
                    case "all":
                        break;
                    case "source":
                        sites = activity.GetSitesForSource(name);
                        break;
                    default:
                        sites = new List<string> { name };
                        break;
                }

                return Json(new
                {
                    files = activity.GetFiles(sites).OrderByDescending(d => d.ReceivedTime).Take(50).AsEnumerable().Select(f => new
                    {
                        f.FileName,
                        f.FileSize,
                        f.IsExtracted,
                        f.Message,
                        f.MirroredFileID,
                        f.MirrorPath,
                        f.ModifiedTime,
                        f.NamedFileKey,
                        f.NumRows,
                        f.Processed,
                        f.ReceivedTime,
                        f.SourcePath,
                        f.SourceSite,
                        ValueDate = f.ValueDate.HasValue ? new RFDate(f.ValueDate.Value).ToJavascript() : null
                    })
                });
            }
        }

        [HttpGet]
        public FileResult GetFile(int mirroredFileID)
        {
            using (var activity = new RFMirrorActivity(Context, Username))
            {
                var file = activity.GetFile(mirroredFileID);
                if (file.content != null)
                {
                    return File(file.content, RFFileHelpers.GetContentType(file.mirroredFile.FileName), file.mirroredFile.FileName);
                }
                return null;
            }
        }

        [HttpGet]
        [RFHandleJsonError]
        public JsonResult GetPreview(int mirroredFileID, int sectionNo = 0, int maxRows = 100)
        {
            using (var activity = new RFMirrorActivity(Context, Username))
            {
                var file = activity.GetFile(mirroredFileID);
                if (file.content != null)
                {
                    var report = RFReportParserProcessor.LoadFromStream(new System.IO.MemoryStream(file.content), new RFFileTrackedAttributes
                    {
                        FileName = file.mirroredFile.FileName,
                        FileSize = file.mirroredFile.FileSize,
                        FullPath = file.mirroredFile.MirrorPath,
                        ModifiedDate = file.mirroredFile.ModifiedTime
                    }, RFDate.Today(), new RFReportParserConfig
                    {
                        Format = RFReportParserFormat.AutoDetect,
                        HasHeaders = false
                    }, new RFSimpleReportBuilder());
                    if (report != null && report.Sections.Count > sectionNo)
                    {
                        var dataTable = report.Sections.Skip(sectionNo).First().AsDataTable();
                        if (dataTable != null)
                        {
                            dataTable.Columns["RFRowNo"].SetOrdinal(0);
                            dataTable.Columns["RFRowNo"].ColumnName = "#";
                            foreach (DataColumn c in dataTable.Columns)
                            {
                                if (Int32.TryParse(c.ColumnName, out var n))
                                {
                                    c.ColumnName = Interfaces.Formats.XLS.XLSGenerator.GetExcelColumnName(n + 1);
                                }
                            }
                            for (var i = dataTable.Rows.Count - 1; i >= maxRows; i--)
                            {
                                dataTable.Rows[i].Delete();
                            }
                        }

                        var sections = new List<object>();
                        int sn = 0;
                        foreach (var s in report.Sections)
                        {
                            sections.Add(new { id = sn, name = $"[{sn + 1}] {s.Name}" });
                            sn++;
                        }

                        return Json(new
                        {
                            sections = sections,
                            selectedSection = sectionNo,
                            preview = dataTable
                        });
                    }
                }
                return null;
            }
        }
    }
}
