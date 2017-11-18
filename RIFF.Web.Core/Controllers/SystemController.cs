// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIFF.Core;
using RIFF.Framework;
using RIFF.Interfaces.Formats.CSV;
using RIFF.Interfaces.Formats.JSON;
using RIFF.Web.Core.App_Start;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models.IO;
using RIFF.Web.Core.Models.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    public class SystemController : RIFFController
    {
        private IRFSystemContext _systemContext;

        public SystemController(IRFSystemContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
            _systemContext = context;
        }

        public static string ImplyContentType(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var extension = System.IO.Path.GetExtension(fileName).ToLower().Trim();
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    switch (extension)
                    {
                        case ".xls":
                            return "application/excel";

                        case ".xlsx":
                            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                        case ".csv":
                            return "text/plain";

                        case ".pdf":
                            return "application/pdf";

                        case ".zip":
                            return "application/zip";

                        case ".txt":
                            return "text/plain";
                    }
                }
            }
            return "application/octet-stream";
        }

        public ActionResult Config()
        {
            return View();
        }

        public ActionResult Console()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ConsoleCommand(string method = null, List<string> @params = null, string id = null)
        {
            if (!string.IsNullOrWhiteSpace(method))
            {
                var w = new StringWriter();
                System.Console.SetOut(w);
                RIFFStart.ConsoleExecutor.ExecuteCommand(String.Join(" ", new string[] { method }.Union(@params ?? new List<string>()).Select(s => $"\"{s}\"")));
                return Json(new
                {
                    result = w.GetStringBuilder().ToString(),
                    id = id,
                });
            }
            return Json(new
            {
                result = "No command",
                id = id,
            });
        }

        public ActionResult DataEditor()
        {
            return View(new DataEditorModel());
            /*var cache = RFCatalogKeyDataController.RefreshCache(Context, Username);
            return View(new DataEditorModel
            {
                 ContentTypes = cache.Select(c => c.ContentType).Distinct().OrderBy(c => c),
                 KeyTypes = cache.Select(c => c.KeyType).Distinct().OrderBy(c => c),
                 Paths = cache.Select(c => c.FriendlyString).Distinct().OrderBy(c => c),
                 Dates = cache.Select(c => c.ValueDate).Distinct().OrderBy(c => c)
            });*/
        }

        public ActionResult DataSet(string type, long keyReference)
        {
            using (var activity = new RFDataSetsActivity(Context))
            {
                ViewBag.KeyReference = keyReference;
                ViewBag.Type = type;
                var columnTypes = new List<KeyValuePair<string, Type>>();
                var dataSetDocument = activity.GetDataSetDocument(keyReference);
                if (dataSetDocument != null)
                {
                    ViewBag.Key = String.Format("{0} / {1}", dataSetDocument.Key.FriendlyString(), dataSetDocument.Key.GetInstance());
                    ViewBag.Data = JSONGenerator.ExportToJSON(dataSetDocument.GetContent<IRFDataSet>(), columnTypes);
                    ViewBag.ColumnTypes = columnTypes;
                }
                return View();
            }
        }

        public FileResult DownloadEntry(string type, long keyReference)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                var entry = dataEditor.GetDocumentForDownload(type, keyReference);
                if (entry != null)
                {
                    var shortType = System.IO.Path.GetExtension(type).TrimStart('.');
                    var content = entry.Content;
                    if (content is IRFDataSet)
                    {
                        var namePart = entry.Key.FriendlyString();
                        Array.ForEach(Path.GetInvalidFileNameChars(), c => namePart = namePart.Replace(c.ToString(), String.Empty));
                        var date = (entry.Key.GraphInstance != null ? entry.Key.GraphInstance.ValueDate : null) ?? DateTime.Now;

                        return File(RIFF.Interfaces.Formats.XLSX.XLSXGenerator.ExportToXLSX(type, content as IRFDataSet), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        String.Format("{0}_{3}-{1}-{2}.xlsx", namePart, shortType, keyReference, date.ToString("yyyyMMdd")));
                    }
                    else if (entry.Content is RFFile)
                    {
                        var file = entry.Content as RFFile;
                        return File(file.Data, ImplyContentType(file.ContentName), file.Attributes.FileName);
                    }
                    else if (entry.Content is RFRawReport)
                    {
                        var report = entry.Content as RFRawReport;
                        var csvBuilder = new StringBuilder();
                        foreach (var section in report.Sections)
                        {
                            try
                            {
                                csvBuilder.Append(CSVBuilder.FromDataTable(section.AsDataTable()));
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(this, "Error exporting section {0} of report {1} to .csv: {2}", section.Name, report.ReportCode, ex.Message);
                            }
                        }
                        return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", String.Format("{0}_{1}.csv", report.ReportCode, report.ValueDate.ToString("yyyy-MM-dd")));
                    }
                    else
                    {
                        var namePart = entry.Key.FriendlyString();
                        Array.ForEach(Path.GetInvalidFileNameChars(), c => namePart = namePart.Replace(c.ToString(), String.Empty));
                        var date = (entry.Key.GraphInstance != null ? entry.Key.GraphInstance.ValueDate : null) ?? DateTime.Now;

                        var xml = RFXMLSerializer.PrettySerializeContract(content);
                        return File(Encoding.UTF8.GetBytes(xml), "text/xml", String.Format("{0}_{3}-{1}-{2}.xml", namePart, shortType, keyReference, date.ToString("yyyyMMdd")));
                    }
                }
            }
            return null;
        }

        public ActionResult ErrorQueue()
        {
            return View();
        }

        public FileResult ExportEntry(string type, long keyReference)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                var entry = dataEditor.GetDocumentForDownload(type, keyReference);
                if (entry != null)
                {
                    var shortType = System.IO.Path.GetExtension(type).TrimStart('.');
                    var xml = RFXMLSerializer.PrettySerializeContract(entry);
                    return File(Encoding.UTF8.GetBytes(xml), "text/xml", String.Format("RFDocument_{0}-{1}-{2}.xml", shortType, keyReference, DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                }
            }
            return null;
        }

        public JsonResult GetConfigs()
        {
            using (var userConfig = new RFConfigActivity(Context, Username))
            {
                return Json(userConfig.GetConfigs().Select(c => new
                {
                    Section = c.Section,
                    Item = c.Item,
                    Key = c.Key,
                    UserConfigKeyID = c.UserConfigKeyID,
                    UserConfigValueID = c.UserConfigValueID,
                    Description = c.Description,
                    Environment = c.Environment,
                    Value = c.Value,
                    Version = c.Version,
                    UpdateTime = c.UpdateTime,
                    UpdateUser = c.UpdateUser
                }));
            }
        }

        public JsonResult GetDocumentForEdit(string type, long keyReference)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                return Json(dataEditor.GetDocumentForEdit(type, keyReference));
            }
        }

        public JsonResult GetErrorQueue()
        {
            var queue = _systemContext.DispatchStore.GetErrorQueue();
            return Json(new
            {
                ErrorQueue = queue.Where(q => q.DispatchState != DispatchState.Finished && q.DispatchState != DispatchState.Ignored && q.DispatchState != DispatchState.Skipped).Select(e => new
                {
                    DispatchState = e.DispatchState.ToString(),
                    DispatchKey = e.DispatchKey,
                    GraphInstance = e.Instance?.Name ?? String.Empty,
                    ValueDate = e.Instance?.ValueDate?.ToJavascript() ?? String.Empty,
                    e.Weight,
                    e.ProcessName,
                    e.Message,
                    e.ShouldRetry,
                    e.LastStart,
                    IsGraph = e.Instance != null
                }),
                Completed = queue.Where(q => q.DispatchState == DispatchState.Finished || q.DispatchState == DispatchState.Ignored || q.DispatchState == DispatchState.Skipped).Select(e => new
                {
                    DispatchState = e.DispatchState.ToString(),
                    GraphInstance = e.Instance?.Name ?? String.Empty,
                    ValueDate = e.Instance?.ValueDate?.ToJavascript() ?? String.Empty,
                    e.Weight,
                    e.ProcessName,
                    e.Message,
                    e.ShouldRetry,
                    e.LastStart
                })
            });
        }

        public FileResult GetInputFile(string uniqueKey)
        {
            var activity = new RFInputFilesActivity(Context);
            var report = activity.GetInputFile(uniqueKey);
            if (report != null)
            {
                return File(report.Data, ImplyContentType(report.ContentName), report.Attributes.FileName);
            }
            return null;
        }

        public JsonResult GetInputFiles(RFDate receivedDate)
        {
            var activity = new RFInputFilesActivity(Context);
            return Json(activity.GetInputFilesList(null, receivedDate).Select(c => new
            {
                Key = c.Key.ToString(),
                UpdateDate = c.UpdateTime,
                UpdateTime = c.UpdateTime,
                FileKey = c.FileKey,
                FileName = c.Attributes.FileName,
                FileSize = c.Attributes.FileSize,
                ModifiedTime = new DateTimeOffset(c.Attributes.ModifiedDate),
                UniqueKey = c.UniqueKey
            }));
        }

        public JsonResult GetInputReport(long keyReference)
        {
            var activity = new RFInputReportsActivity(Context);
            var report = activity.GetInputReport(keyReference);
            if (report != null)
            {
                return Json(report.GetFirstSection().AsDataTable());
            }
            return null;
        }

        public JsonResult GetInputReports(RFDate valueDate)
        {
            var activity = new RFInputReportsActivity(Context);
            return Json(activity.GetInputReportsList(valueDate).Select(c => new
            {
                Key = c.Key.ToString(),
                KeyReference = c.KeyReference,
                UpdateDate = c.UpdateTime,
                UpdateTime = c.UpdateTime,
                ReportCode = c.ReportCode,
                ReportDescription = c.ReportDescription,
                NumRows = c.NumRows,
                ValueDate = c.ValueDate.ToJavascript(),
                SourceUniqueKey = c.SourceUniqueKey
            }));
        }

        public JsonResult GetInstances(string keyType, long keyReference)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                var document = dataEditor.GetDocumentForDownload(keyType, keyReference);

                var fs = document.Key.FriendlyString();
                var vd = document.Key.GraphInstance?.ValueDate;

                return Json(dataEditor.GetDocuments(keyType, null, null, 0, null, false)
                    .Where(d => d.IsValid && d.Key.FriendlyString() == fs && (!vd.HasValue || (d.Key.GraphInstance?.ValueDate == null) || d.Key.GraphInstance.ValueDate <= vd.Value))
                    .Select(d => new
                    {
                        KR = d.KeyReference,
                        UT = d.UpdateTime,
                        VD = (d.Key.GraphInstance != null && d.Key.GraphInstance.ValueDate.HasValue && d.Key.GraphInstance.ValueDate.Value != RFDate.NullDate) ? d.Key.GraphInstance.ValueDate.Value.ToJavascript() : null,
                        DS = d.DataSize
                    }));
            }
        }

        public JsonResult GetLatestDocuments(RFDate? valueDate = null, bool validOnly = true)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                return Json(dataEditor.GetDocuments(null, null, null, 0, valueDate, true)
                    .Where(d => !validOnly || d.IsValid)
                    .Where(d => d.KeyType != "RIFF.Framework.RFFileKey")
                    .GroupBy(d => new Tuple<string, string, string>(d.KeyType, d.ContentType, d.Key.FriendlyString()))
                    .Select(d => new { Key = d.Key, Latest = d.OrderByDescending(i => i.Instance?.ValueDate).First(), All = d.AsEnumerable() })
                    .Select(d => new
                    {
                        FriendlyString = d.Key.Item3,
                        KeyReference = d.Latest.KeyReference,
                        KeyTypeFull = d.Key.Item1,
                        Plane = d.Latest.Key.Plane.ToString(),
                        ContentTypeFull = d.Key.Item2,
                        DataSize = d.Latest.DataSize,
                        IsValid = d.Latest.IsValid,
                        UpdateTime = d.Latest.UpdateTime,
                        IsLatest = (d.Latest.Key.GraphInstance?.ValueDate == null || d.Latest.Key.GraphInstance.ValueDate.Value == RFDate.NullDate || d.Latest.Key.GraphInstance?.ValueDate.Value == valueDate),
                        ValueDate = (d.Latest.Key.GraphInstance?.ValueDate == null || d.Latest.Key.GraphInstance.ValueDate.Value == RFDate.NullDate) ? null : d.Latest.Key.GraphInstance.ValueDate.Value.ToJavascript(),
                        /*Instances = d.All.Select(i => new
                        {
                            KR = i.KeyReference,
                            UT = i.UpdateTime,
                            VD = (i.Key.GraphInstance != null && i.Key.GraphInstance.ValueDate.HasValue && i.Key.GraphInstance.ValueDate.Value != RFDate.NullDate) ? i.Key.GraphInstance.ValueDate.Value.ToJavascript() : null,
                            DS = i.DataSize
                        })*/
                    }));
            }
        }

        public JsonResult GetLog(long logID)
        {
            var logs = Context.SystemLog.GetLogs(null, logID);
            if (logs != null && logs.Count == 1)
            {
                return Json(logs.First());
            }
            return null;
        }

        public JsonResult GetLogs()
        {
            return Json(new
            {
                SystemLog = Context.SystemLog.GetLogs().Select(l => new
                {
                    LogID = l.LogID,
                    Timestamp = l.Timestamp,
                    Hostname = l.Hostname != null ? l.Hostname.ToLower() : "n/a",
                    Level = l.Level,
                    Source = l.Source,
                    Message = l.Message,
                    Thread = l.Thread,
                    Exception = !string.IsNullOrWhiteSpace(l.Exception) ? l.Exception.Substring(0, 30) : String.Empty,
                    Content = !string.IsNullOrWhiteSpace(l.Content) ? l.Content.Substring(0, 30) : String.Empty
                }),
                ProcessLog = Context.SystemLog.GetProcesses().Select(p => new
                {
                    LogID = p.LogID,
                    Timestamp = p.Timestamp,
                    Message = p.Message,
                    GraphName = p.GraphName,
                    ProcessName = p.ProcessName,
                    Instance = p.GraphInstance.Name,
                    ValueDate = p.GraphInstance.ValueDate?.ToJavascript(),
                    IOTime = p.IOTime,
                    ProcessingTime = p.ProcessingTime,
                    NumUpdates = p.NumUpdates,
                    Success = p.Success
                })
            });
        }

        public FileResult GetProcessDomain(string processName, string instanceName, DateTime? instanceDate)
        {
            using (var activity = new RFEngineActivity(Context, EngineConfig))
            {
                RFGraphProcessorDomain domain = null;
                var engineConfig = activity.GetEngineConfig();
                var graphInstance = new RFGraphInstance
                {
                    Name = string.IsNullOrWhiteSpace(instanceName) ? null : instanceName,
                    ValueDate = instanceDate.HasValue ? new RFDate(instanceDate.Value.Date) : RFDate.NullDate
                };

                foreach (var graph in engineConfig.Graphs.Values)
                {
                    var process = graph.Processes.Values.FirstOrDefault(p => RFGraphDefinition.GetFullName(graph.GraphName, p.Name) == processName);
                    if (process != null)
                    {
                        var processor = process.Processor();
                        domain = processor.CreateDomain();
                        if (domain != null)
                        {
                            foreach (var propertyInfo in domain.GetType().GetProperties())
                            {
                                var ioBehaviourAttribute = (propertyInfo.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute);
                                RFCatalogKey ioKey = null;
                                if (ioBehaviourAttribute != null)
                                {
                                    var ioBehaviour = ioBehaviourAttribute.IOBehaviour;
                                    var ioMapping = process.IOMappings.FirstOrDefault(m => m.PropertyName == propertyInfo.Name);
                                    if (ioMapping != null)
                                    {
                                        ioKey = ioMapping.Key.CreateForInstance(graphInstance);
                                        var options = RFGraphInstance.ImplyOptions(ioMapping);
                                        var entry = Context.LoadEntry(ioKey, options) as RFDocument;
                                        if (entry != null)
                                        {
                                            propertyInfo.SetValue(domain, entry.Content);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }

                if (domain != null)
                {
                    var xmlString = RFXMLSerializer.PrettySerializeContract(domain);
                    return File(
                        Encoding.UTF8.GetBytes(xmlString),
                        "text/xml",
                        String.Format("{0}_{1}_{2}.xml", processName, instanceName ?? String.Empty, instanceDate.Value.ToString("yyyy-MM-dd"))
                        );
                }
            }
            return null;
        }

        public JsonResult GetProcesses(string instanceName, DateTime? instanceDate)
        {
            using (var activity = new RFEngineActivity(Context, EngineConfig))
            {
                var engineConfig = activity.GetEngineConfig();
                var graphInstance = new RFGraphInstance
                {
                    Name = string.IsNullOrWhiteSpace(instanceName) ? RFGraphInstance.DEFAULT_INSTANCE : instanceName,
                    ValueDate = instanceDate.HasValue ? new RFDate(instanceDate.Value.Date) : RFDate.NullDate
                };
                var stats = activity.GetEngineStats(graphInstance);

                var allProcesses = new List<object>();
                foreach (var process in engineConfig.Processes.Values)
                {
                    var processor = process.Processor();
                    var stat = stats.GetStat(process.Name);
                    allProcesses.Add(new
                    {
                        Graph = "(none)",
                        Name = process.Name,
                        FullName = process.Name,
                        Description = process.Description,
                        Type = processor.GetType().FullName,
                        RequiresIdle = false,
                        IsGraph = false,
                        LastRun = stat != null ? (DateTimeOffset?)stat.LastRun : null,
                        LastDuration = stat != null ? (long?)stat.LastDuration : null,
                    });
                }

                foreach (var graph in engineConfig.Graphs.Values)
                {
                    foreach (var process in graph.Processes.Values)
                    {
                        var fullName = RFGraphDefinition.GetFullName(graph.GraphName, process.Name);
                        var stat = stats.GetStat(fullName);
                        var processor = process.Processor();
                        var domain = processor.CreateDomain();
                        var io = new List<object>();
                        if (domain != null)
                        {
                            foreach (var propertyInfo in domain.GetType().GetProperties())
                            {
                                var ioBehaviourAttribute = (propertyInfo.GetCustomAttributes(typeof(RFIOBehaviourAttribute), true).FirstOrDefault() as RFIOBehaviourAttribute);
                                string direction = "-";
                                string dateType = "-";
                                RFCatalogKey ioKey = null;
                                if (ioBehaviourAttribute != null)
                                {
                                    var ioBehaviour = ioBehaviourAttribute.IOBehaviour;
                                    direction = ioBehaviour.ToString();

                                    var ioMapping = process.IOMappings.FirstOrDefault(m => m.PropertyName == propertyInfo.Name);
                                    if (ioMapping != null)
                                    {
                                        ioKey = ioMapping.Key.CreateForInstance(graphInstance);
                                        var options = RFGraphInstance.ImplyOptions(ioMapping);
                                        dateType = options.DateBehaviour.ToString();
                                    }
                                }

                                io.Add(new
                                {
                                    Field = propertyInfo.Name,
                                    Direction = direction,
                                    DateType = dateType,
                                    DataType = propertyInfo.PropertyType.Name,
                                    KeyType = ioKey != null ? ioKey.GetType().Name : null,
                                    ShortKey = ioKey != null ? ioKey.FriendlyString() : null,
                                    FullKey = ioKey != null ? RFXMLSerializer.SerializeContract(ioKey) : null
                                });
                            }
                        }
                        allProcesses.Add(new
                        {
                            Graph = graph.GraphName,
                            Name = process.Name,
                            FullName = fullName,
                            IsGraph = true,
                            Description = process.Description,
                            Type = processor.GetType().FullName,
                            IO = io,
                            LastRun = stat != null ? (DateTimeOffset?)stat.LastRun : null,
                            LastDuration = stat != null ? (long?)stat.LastDuration : null
                        });
                    }
                }

                return Json(allProcesses);
            }
        }

        public JsonResult GetTasks(string instanceName, DateTime? instanceDate)
        {
            try
            {
                using (var activity = new RFEngineActivity(Context, EngineConfig))
                {
                    var engineConfig = activity.GetEngineConfig();
                    var graphInstance = new RFGraphInstance
                    {
                        Name = string.IsNullOrWhiteSpace(instanceName) ? RFGraphInstance.DEFAULT_INSTANCE : instanceName,
                        ValueDate = instanceDate.HasValue ? new RFDate(instanceDate.Value.Date) : RFDate.NullDate
                    };
                    var engineStats = activity.GetEngineStats(graphInstance);
                    var errors = _systemContext.DispatchStore.GetErrorQueue(0);

                    var tasks = new List<object>();

                    foreach (var t in EngineConfig.Tasks)
                    {
                        var stat = engineStats?.GetStat(t.ProcessName);
                        var dispatchKey = new RFParamProcessInstruction(t.ProcessName, null).DispatchKey();
                        var error = dispatchKey.NotBlank() ? errors.Where(e => e.DispatchKey == dispatchKey).FirstOrDefault() : null;

                        tasks.Add(new
                        {
                            t.TaskName,
                            t.Description,
                            t.GraphName,
                            Schedule = String.Join(", ", new string[] { t.Trigger, t.SchedulerSchedule, t.SchedulerRange }.Where(s => s.NotBlank())),
                            t.IsSystem,
                            Status = error?.DispatchState.ToString() ?? "OK",
                            Message = error?.Message,
                            LastRun = stat?.LastRun ?? error?.LastStart,
                            LastDuration = stat?.LastDuration,
                            IsGraph = false,
                            FullName = t.ProcessName
                        });
                    }

                    foreach (var g in EngineConfig.Graphs.Where(g => g.Value.GraphTasks.Any()))
                    {
                        var graphStats = activity.GetGraphStats(g.Value.GraphName, graphInstance);
                        foreach (var t in g.Value.GraphTasks)
                        {
                            var processName = RFGraphDefinition.GetFullName(t.GraphName, t.ProcessName);
                            var stat = graphStats?.GetStat(t.ProcessName);
                            var dispatchKey = new RFGraphProcessInstruction(graphInstance, processName)?.DispatchKey();
                            var error = dispatchKey.NotBlank() ? errors.Where(e => e.DispatchKey == dispatchKey).FirstOrDefault() : null;

                            tasks.Add(new
                            {
                                t.TaskName,
                                t.Description,
                                t.GraphName,
                                Schedule = String.Join(", ", new string[] { t.Trigger, t.SchedulerSchedule, t.SchedulerRange }.Where(s => s.NotBlank())),
                                //t.SchedulerRange,
                                //t.SchedulerSchedule,
                                //t.Trigger,
                                t.IsSystem,
                                Status = error?.DispatchState.ToString() ?? ((stat?.CalculationOK ?? false) ? "OK" : String.Empty),
                                Message = error?.Message ?? stat?.Message,
                                LastRun = stat?.LastRun ?? error?.LastStart,
                                IsGraph = true,
                                FullName = processName
                            });
                        }
                    }

                    return Json(tasks);
                }
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("GetTasks", ex));
            }
        }

        public JsonResult GetUserLog(RFDate logDate)
        {
            return Json(Context.UserLog.GetEntries(logDate).Select(c => new
            {
                Area = c.Area,
                Action = c.Action,
                Description = c.Description,
                Username = c.Username,
                Processor = c.Processor,
                Timestamp = c.Timestamp,
                ValueDate = c.ValueDate != RFDate.NullDate ? c.ValueDate.ToJavascript() : null,
                KeyType = c.KeyType,
                KeyReference = c.KeyReference,
                IsUserAction = c.IsUserAction,
                IsWarning = c.IsWarning
            }));
        }

        [HttpPost]
        public JsonResult IgnoreError(string dp)
        {
            try
            {
                if (!dp.IsBlank())
                {
                    _systemContext.DispatchStore.Ignored(dp);
                    Context.UserLog.LogEntry(new RFUserLogEntry
                    {
                        Action = "Ignore Error",
                        Area = "Error Queue",
                        Description = String.Format("Ignored error on processor {0}", dp),
                        IsUserAction = true,
                        IsWarning = false,
                        Timestamp = DateTimeOffset.Now,
                        Username = Username,
                    });
                    return GetErrorQueue();
                }
                return Json(JsonError.Throw("IgnoreError", "System error: blank key"));
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("IgnoreError", ex));
            }
        }

        [HttpPost]
        [RFControllerAuthorize(AccessLevel = RFAccessLevel.Write, ResponseType = ResponseType.Page)]
        public ActionResult ImportEntry(HttpPostedFileBase fileData)
        {
            try
            {
                if (fileData == null || fileData.FileName == null || fileData.InputStream == null)
                {
                    throw new RFSystemException(this, "No file submitted.");
                }

                var xml = System.Text.Encoding.UTF8.GetString(RFStreamHelpers.ReadBytes(fileData.InputStream));
                var document = RFXMLSerializer.DeserializeContract(typeof(RFDocument).FullName, xml) as RFDocument;

                if (document == null)
                {
                    return Error("ImportEntry", "System", null, "Unable to deserialize object.");
                }
                else
                {
                    Context.SaveDocument(document.Key, document.Content);
                    return RedirectToAction("DataEditor", "System");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "ImportEntry", ex);
                return Error("ImportEntry", "System", null, "Error submitting entry: {0}", ex.Message);
            }
        }

        public ActionResult InputFiles()
        {
            return View();
        }

        public ActionResult InputReport(long keyReference)
        {
            ViewBag.KeyReference = keyReference;
            return View();
        }

        public ActionResult InputReports()
        {
            return View();
        }

        public ActionResult InvalidateEntry(string type, long keyReference)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                var entry = dataEditor.GetDocumentForDownload(type, keyReference);
                if (entry != null)
                {
                    Context.Invalidate(entry.Key);
                }
            }
            return RedirectToAction("DataEditor", "System", new { area = "" });
        }

        /*
        public JsonResult GetDocuments(string type = null, DateTime? startTime = null, DateTime? endTime = null, int limitResults = 0)
        {
            using (var dataEditor = new RFDataEditorActivity(Context, Username))
            {
                return Json(dataEditor.GetDocuments(type, startTime, endTime, limitResults, null).Select(d => new
                {
                    FriendlyString = d.Key.FriendlyString(),
                    KeyReference = d.KeyReference,
                    KeyType = RFReflectionHelpers.TrimType(d.KeyType),
                    KeyTypeFull = d.KeyType,
                    Plane = d.Key.Plane.ToString(),
                    ContentType = RFReflectionHelpers.TrimType(d.ContentType),
                    ContentTypeFull = d.ContentType,
                    DataSize = d.DataSize,
                    IsValid = d.IsValid,
                    UpdateTime = d.UpdateTime,
                    UpdateDate = d.UpdateTime,
                    ValueDate = (d.Key.GraphInstance != null && d.Key.GraphInstance.ValueDate.HasValue && d.Key.GraphInstance.ValueDate.Value != RFDate.NullDate) ? d.Key.GraphInstance.ValueDate.Value.ToJavascript() : null
                }));
            }
        }*/

        public ActionResult Logs()
        {
            return View();
        }

        public ActionResult Maintenance()
        {
            return View();
        }

        public ActionResult Processes()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Restart()
        {
            try
            {
                var status = RFServiceMaintainer.Restart(Username);
                if (status == RFServiceMaintainer.SUCCESS)
                {
                    return Json(true);
                }
                else
                {
                    throw new RFSystemException(this, status);
                }
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("Restart", "Error restarting service: {0}", ex.Message));
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        [RFControllerAuthorize(AccessLevel = RFAccessLevel.Write, ResponseType = ResponseType.Json)]
        public JsonResult SaveDocument(string keyType, string contentType, string keyData, string contentData)
        {
            if (!string.IsNullOrWhiteSpace(keyType) && !string.IsNullOrWhiteSpace(keyData) && !string.IsNullOrWhiteSpace(contentType) && !string.IsNullOrWhiteSpace(contentData))
            {
                try
                {
                    using (var dataEditor = new RFDataEditorActivity(Context, Username))
                    {
                        return Json(dataEditor.SaveDocument(keyType, contentType, keyData, contentData));
                    }
                }
                catch (Exception ex)
                {
                    return Json(JsonError.Throw("SaveDocument", ex));
                }
            }
            return Json(JsonError.Throw("SaveDocument", "Internal system error."));
        }

        [HttpGet]
        public JsonResult Status()
        {
            try
            {
                using (var service = new RFServiceClient())
                {
                    var status = service.RFService.Status();
                    if (status != null)
                    {
                        if (status.Running)
                        {
                            return Json(new
                            {
                                Status = "OK",
                                Message = "System is up",
                                WorkingSet = status.WorkingSet,
                                NumThreads = status.NumThreads,
                                RequestsServed = status.RequestsServed,
                                LastRequestTime = status.LastRequestTime,
                                StartTime = status.StartTime
                            });
                        }
                        else
                        {
                            return Json(new
                            {
                                Status = "ERROR",
                                Message = "Not Running"
                            });
                        }
                    }
                    else
                    {
                        return Json(new
                        {
                            Status = "ERROR",
                            Message = "Blank Response"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Status = "ERROR",
                    Message = ex.Message
                });
            }
        }

        public ActionResult Tasks()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult UpdateConfig(FormCollection collection)
        {
            try
            {
                if (collection.GetValue("UserConfigKeyID") != null)
                {
                    using (var configActivity = new RFConfigActivity(Context, Username))
                    {
                        var userConfigKeyID = Int32.Parse(collection["UserConfigKeyID"]);
                        string environment = collection["Environment"];
                        string value = collection["Value"];
                        string section = collection["Section"];
                        string item = collection["Item"];
                        string key = collection["Key"];
                        var path = string.Format("{0}/{1}/{2}", section, item, key);

                        return Json(configActivity.UpdateValue(userConfigKeyID, environment, value, Username, path));
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("UpdateConfig", ex));
            }
            return Json(JsonError.Throw("UpdateConfig", "Internal system error."));
        }

        [HttpPost]
        [ValidateInput(false)]
        [RFControllerAuthorize(AccessLevel = RFAccessLevel.Write, ResponseType = ResponseType.Json)]
        public JsonResult UpdateDocument(string type, long keyReference, string data)
        {
            if (!string.IsNullOrWhiteSpace(type) && keyReference > 0 && !string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    using (var dataEditor = new RFDataEditorActivity(Context, Username))
                    {
                        return Json(dataEditor.UpdateDocument(type, keyReference, data));
                    }
                }
                catch (Exception ex)
                {
                    return Json(JsonError.Throw("UpdateDocument", ex));
                }
            }
            return Json(JsonError.Throw("UpdateDocument", "Internal system error."));
        }

        public ActionResult UpdateInputReport(long keyReference, string updates)
        {
            try
            {
                var activity = new RFInputReportsActivity(Context);
                var report = activity.GetInputReportDocument(keyReference);
                if (report != null)
                {
                    var section = report.GetContent<RFRawReport>().GetFirstSection();
                    foreach (var update in JsonConvert.DeserializeObject<List<dynamic>>(updates))
                    {
                        int rowNum = update.rowNum;
                        if (section.Rows.Count >= rowNum)
                        {
                            var row = section.Rows.Skip(rowNum - 1).First();
                            foreach (JToken token in ((JObject)update.data).Children())
                            {
                                if (token is JProperty)
                                {
                                    row.SetString((token as JProperty).Name, (token as JProperty).Value.ToString());
                                }
                            }
                        }
                    }

                    using (var rfService = new RFServiceClient())
                    {
                        RFProcessingTrackerHandle trackerKey = null;
                        trackerKey = rfService.RFService.SubmitAndProcess(new List<RFCatalogEntryDTO> { new RFCatalogEntryDTO(report) }, new RFUserLogEntry
                        {
                            Action = "Update Input Report",
                            Description = String.Format("Manually updated report {0}", report.Key.FriendlyString()),
                            IsUserAction = true,
                            IsWarning = false,
                            Processor = null,
                            ValueDate = report.Key.GraphInstance.ValueDate.Value,
                            Username = Username
                        });

                        ProcessController.SubmitModel(new ProcessingModel
                        {
                            Tracker = trackerKey,
                            ProcessingKey = trackerKey.TrackerCode,
                            FileKey = (report.Key as RFRawReportKey).ReportCode,
                            FileName = (report.Key as RFRawReportKey).FriendlyString(),
                            FileSize = 0,
                            ReturnUrl = null
                        });

                        return RedirectToAction("ProcessingStatus", "Process", new { processKey = trackerKey.TrackerCode });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Error updating input report {0}", keyReference);
            }
            return null;
        }

        public ActionResult UserLog()
        {
            return View();
        }
    }
}
