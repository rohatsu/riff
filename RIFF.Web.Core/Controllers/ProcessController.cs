// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Framework;
using RIFF.Framework.Preferences;
using RIFF.Web.Core.Helpers;
using RIFF.Web.Core.Models.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    [RFControllerAuthorize(AccessLevel = RFAccessLevel.NotSet, Permission = null)]
    public class ProcessController : RIFFController
    {
        private IRFSystemContext _systemContext;

        static ProcessController()
        {
        }

        public ProcessController(IRFSystemContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
            _systemContext = context;
        }

        public static void SubmitModel(ProcessingModel model)
        {
            AddModelToCache(model.ProcessingKey, model);
        }

        [HttpGet]
        public JsonResult GetSystemStatus()
        {
            try
            {
                var errorQueue = _systemContext.DispatchStore.GetErrorQueue(0);
                var errorItems = errorQueue.Count(e => e.DispatchState == DispatchState.Error && !e.ShouldRetry);
                var warningItems = errorQueue.Count(e => e.DispatchState == DispatchState.Error && e.ShouldRetry);
                var stuckItems = errorQueue.Count(e => e.DispatchState == DispatchState.Started && e.LastStart.HasValue && (DateTimeOffset.Now - e.LastStart.Value).TotalHours > 2);

                if (errorItems == 0 && warningItems == 0 && stuckItems == 0)
                {
                    return Json(new
                    {
                        Status = "OK",
                        Message = "No errors"
                    });
                }
                else
                {
                    var sb = new StringBuilder();
                    if (errorItems > 0)
                    {
                        sb.AppendFormat("There are {0} fatal errors in Error Queue\r\n", errorItems.ToString());
                    }
                    if (warningItems > 0 || stuckItems > 0)
                    {
                        sb.AppendFormat("There are {0} transient errors in Error Queue\r\n", (warningItems + stuckItems).ToString());
                    }
                    return Json(new
                    {
                        Status = errorItems > 0 ? "Error" : "Warning",
                        Message = sb.ToString()
                    });
                }
            }
            catch (Exception)
            {
                return Json(new
                {
                    Status = "Error",
                    Message = "Internal System Error"
                });
            }
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult ProcessingStatus(string processKey)
        {
            if (processKey == "test")
            {
                return View(new ProcessingModel
                {
                    ProcessingKey = "test",
                    FileName = "Test file.xls",
                    FileKey = "TestFile.File",
                    FileSize = 56780,
                    Tracker = new RFProcessingTrackerHandle
                    {
                        TrackerCode = "test"
                    }
                });
            }
            var model = GetModelFromCache(processKey);
            if (model != null)
            {
                return View(model);
            }
            return Error("Inputs", "System", null, "Unable to find status entry for request {0}.", processKey);
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult RefreshProcessingStatus(string processKey)
        {
            try
            {
                if (processKey == "test")
                {
                    var r = new Random();
                    var f = r.Next(0, 10) + 4;
                    var p = r.Next(0, 10) + 4;
                    return Json(new
                    {
                        IsComplete = true,
                        CurrentProcess = "test",
                        FinishedCycles = f,
                        ProcessingCycles = p,
                        RemainingCycles = 300 - f - p,
                        Messages = new string[] { "Message 1", "Message 2" },
                        Keys = 5,
                        Time = "01:01",
                        IsValid = true,
                        IsError = false
                    });
                }
                Log.Info(this, "RefreshProcessingStatus {0}", processKey);
                var model = GetModelFromCache(processKey);
                RFProcessingTrackerHandle handle = null;
                if (model != null)
                {
                    handle = model.Tracker;
                }
                else
                {
                    handle = new RFProcessingTrackerHandle { TrackerCode = processKey };
                }
                RFProcessingTracker trackerObject = null;
                using (var rfService = new RFServiceClient())
                {
                    Log.Info(this, "Asking RF service for status on {0}", processKey);
                    trackerObject = rfService.RFService.GetProcessStatus(handle);
                }
                if (trackerObject != null)
                {
                    return Json(new
                    {
                        IsComplete = trackerObject.IsComplete,
                        CurrentProcess = trackerObject.CurrentProcess,
                        FinishedCycles = trackerObject.FinishedCycles,
                        ProcessingCycles = trackerObject.ProcessingCycles,
                        RemainingCycles = trackerObject.RemainingCycles,
                        Messages = ExtractMessages(trackerObject.Messages),
                        Keys = trackerObject.KeyCount,
                        Time = trackerObject.GetDuration().ToString(@"m'm 's's'"),
                        IsValid = true,
                        IsError = trackerObject.IsError()
                    });
                }
                return Json(JsonError.Throw("ProcessingStatus", "Unable to retrieve information about request {0}", processKey));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("no endpoint listening"))
                {
                    // fatal error
                    return Json(JsonError.Throw("ProcessingStatus", "System is offline - {0}", processKey));
                }
                else
                {
                    // recoverable error
                    return Json(new
                    {
                        IsValid = false,
                        Error = ex.Message
                    });
                }
            }
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult RetryError(string dp, string returnUrl = null)
        {
            try
            {
                using (var rfService = new RFServiceClient())
                {
                    RFProcessingTrackerHandle trackerKey = null;
                    trackerKey = rfService.RFService.RetryError(dp, new RFUserLogEntry
                    {
                        Action = "Retry",
                        Description = String.Format("Retried error on process {0}", dp),
                        IsUserAction = true,
                        IsWarning = false,
                        Username = Username
                    });
                    AddModelToCache(trackerKey.TrackerCode, new ProcessingModel
                    {
                        Tracker = trackerKey,
                        ProcessingKey = trackerKey.TrackerCode,
                        FileKey = string.Empty,
                        FileName = String.Format("Retry Error for {0}", dp),
                        FileSize = 0,
                        ReturnUrl = returnUrl
                    });
                    return RedirectToAction("ProcessingStatus", new { processKey = trackerKey.TrackerCode });
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "RetryError", ex);
                return Error("Index", "Home", null, "Error retrying error process: {0}", ex.Message);
            }
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult RunProcess(bool isGraph, string processName, string instanceName, RFDate? instanceDate, string returnUrl = null)
        {
            try
            {
                var graphInstance = /*isGraph ? */new RFGraphInstance
                {
                    Name = string.IsNullOrWhiteSpace(instanceName) ? RFGraphInstance.DEFAULT_INSTANCE : instanceName,
                    ValueDate = instanceDate.HasValue ? instanceDate.Value : RFDate.NullDate
                };// : null;

                using (var rfService = new RFServiceClient())
                {
                    RFProcessingTrackerHandle trackerKey = null;
                    trackerKey = rfService.RFService.RunProcess(isGraph, processName, graphInstance, new RFUserLogEntry
                    {
                        Action = "Run",
                        Description = String.Format("Manually run process {0}", processName),
                        ValueDate = instanceDate ?? RFDate.NullDate,
                        IsUserAction = true,
                        IsWarning = false,
                        Username = Username
                    });
                    AddModelToCache(trackerKey.TrackerCode, new ProcessingModel
                    {
                        Tracker = trackerKey,
                        ProcessingKey = trackerKey.TrackerCode,
                        FileKey = string.Empty,
                        FileName = String.Format("Process: {0} for value date {1}", processName, instanceDate),
                        FileSize = 0,
                        ReturnUrl = returnUrl
                    });
                    return RedirectToAction("ProcessingStatus", new { processKey = trackerKey.TrackerCode });
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "RunProcess", ex);
                return Error("Index", "Home", null, "Error running process: {0}", ex.Message);
            }
        }

        [HttpPost]
        public JsonResult SetPresentationMode(bool active)
        {
            try
            {
                return Json(UserPreferences.SetPresentationMode(Context, Username, active));
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Error setting presentation mode");
            }
            return Json(false);
        }

        [HttpPost]
        public ActionResult SubmitFile(
            string fileKey,
            HttpPostedFileBase fileData,
            RFDate? valueDate,
            string returnUrl = null,
            string instance = null)
        {
            try
            {
                if (fileData == null && Request.Files != null && Request.Files.Count > 0)
                {
                    fileData = Request.Files[0];
                }
                var uniqueKey = String.Format("{0}_web", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                if (fileData == null || fileData.FileName == null || fileData.InputStream == null)
                {
                    throw new RFSystemException(this, "No file submitted.");
                }

                var fileName = Path.GetFileName(fileData.FileName);

                var newFileEntry = RFDocument.Create(
                    RFFileKey.Create(
                        EngineConfig.KeyDomain,
                        RFEnum.FromString(fileKey),
                        uniqueKey),
                    new RFFile
                    {
                        Attributes = new RFFileTrackedAttributes
                        {
                            FileName = fileName,
                            FullPath = fileData.FileName,
                            FileSize = fileData.ContentLength,
                            ModifiedDate = DateTime.Now
                        },
                        Data = RFStreamHelpers.ReadBytes(fileData.InputStream),
                        FileKey = RFEnum.FromString(fileKey),
                        ValueDate = valueDate,
                        UniqueKey = uniqueKey
                    });

                // the file will have graph instance attached
                if (instance.NotBlank() && valueDate.HasValue)
                {
                    newFileEntry.Key.GraphInstance = new RFGraphInstance
                    {
                        Name = instance,
                        ValueDate = valueDate.Value
                    };
                }

                using (var rfService = new RFServiceClient())
                {
                    RFProcessingTrackerHandle trackerKey = null;
                    trackerKey = rfService.RFService.SubmitAndProcess(new List<RFCatalogEntryDTO> { new RFCatalogEntryDTO(newFileEntry) }, new RFUserLogEntry
                    {
                        Action = "Upload File",
                        Description = String.Format("Uploaded file {0} for processing", fileName),
                        IsUserAction = true,
                        IsWarning = false,
                        Processor = null,
                        ValueDate = valueDate.HasValue ? valueDate.Value : RFDate.NullDate,
                        Username = Username
                    });
                    //lock (sSync)
                    {
                        AddModelToCache(uniqueKey, new ProcessingModel
                        {
                            Tracker = trackerKey,
                            ProcessingKey = uniqueKey,
                            FileKey = fileKey,
                            FileName = fileName,
                            FileSize = fileData.ContentLength,
                            ReturnUrl = returnUrl
                        });
                    }
                    return RedirectToAction("ProcessingStatus", new { processKey = uniqueKey });
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "SubmitFile", ex);
                return Error("InputFiles", "System", null, "Error submitting file: {0}", ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SubmitFiles(
            RFDate? valueDate = null,
            string returnUrl = null,
            string instance = null
            )
        {
            try
            {
                if (Request.Files == null || Request.Files.Count == 0)
                {
                    throw new RFSystemException(this, "No files submitted.");
                }

                var processKey = String.Format("{0}_web", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                var submittable = new List<RFCatalogEntryDTO>();
                int uqIdx = 1;

                foreach (var knownFileEnum in RIFF.Web.Core.App_Start.RIFFStart.Config.GetInputFileKeys())
                {
                    // match up by string (or could have registered an enum type...)
                    foreach (string inputFileKey in Request.Files.Keys)
                    {
                        if (knownFileEnum.ToString() == inputFileKey)
                        {
                            var inputFile = Request.Files[inputFileKey] as HttpPostedFileBase;
                            if (inputFile != null && inputFile.ContentLength > 0)
                            {
                                var uniqueKey = String.Format("{0}_web_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), uqIdx++);

                                var doc = RFDocument.Create(
                                RFFileKey.Create(
                                    EngineConfig.KeyDomain,
                                    knownFileEnum,
                                    uniqueKey),
                                new RFFile
                                {
                                    Attributes = new RFFileTrackedAttributes
                                    {
                                        FileName = inputFile.FileName,
                                        FullPath = inputFile.FileName,
                                        FileSize = inputFile.ContentLength,
                                        ModifiedDate = DateTime.Now
                                    },
                                    Data = RFStreamHelpers.ReadBytes(inputFile.InputStream),
                                    FileKey = knownFileEnum,
                                    ValueDate = valueDate,
                                    UniqueKey = uniqueKey
                                });

                                if (instance.NotBlank() && valueDate.HasValue)
                                {
                                    doc.Key.GraphInstance = new RFGraphInstance { Name = instance, ValueDate = valueDate.Value };
                                }

                                submittable.Add(new RFCatalogEntryDTO(doc));
                            }
                        }
                    }
                }

                using (var rfService = new RFServiceClient())
                {
                    RFProcessingTrackerHandle trackerKey = null;
                    trackerKey = rfService.RFService.SubmitAndProcess(submittable, new RFUserLogEntry
                    {
                        Action = "Upload Files",
                        Description = String.Format("Uploaded {0} files for processing", submittable.Count),
                        IsUserAction = true,
                        IsWarning = false,
                        Processor = null,
                        ValueDate = valueDate.HasValue ? valueDate.Value : RFDate.NullDate,
                        Username = Username
                    });
                    //lock (sSync)
                    {
                        AddModelToCache(processKey, new ProcessingModel
                        {
                            Tracker = trackerKey,
                            ProcessingKey = processKey,
                            ReturnUrl = returnUrl
                        });
                    }
                    return RedirectToAction("ProcessingStatus", new { processKey = processKey });
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "SubmitFiles", ex);
                return Error("InputFiles", "System", null, "Error submitting files: {0}", ex.Message);
            }
        }

        protected static void AddModelToCache(string key, ProcessingModel model)
        {
            MemoryCache.Default.Add(key, model, DateTimeOffset.Now.AddDays(1));
        }

        protected static ProcessingModel GetModelFromCache(string key)
        {
            return MemoryCache.Default.Get(key) as ProcessingModel;
        }

        protected List<string> ExtractMessages(Dictionary<string, string> messages)
        {
            var output = new List<string>();
            if (messages != null && messages.Any())
            {
                var unavailableMessages = new List<string>();
                var warnings = new List<string>();
                foreach (var kvp in messages)
                {
                    foreach (var line in (kvp.Value ?? "-").Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.StartsWith("OK"))
                        {
                            output.Add(String.Format("<span class='msgok'><b>{0}:</b> {1}</span>", kvp.Key, line));
                        }
                        else if (line.Contains("Not yet available:"))
                        {
                            unavailableMessages.Add(String.Format("<span class='msggray'><b>{0}:</b> {1}</span>", kvp.Key, line));
                        }
                        else
                        {
                            warnings.Add(String.Format("<span class='msgwarn'><b>{0}:</b> {1}</span>", kvp.Key, line));
                        }
                    }
                }
                output.AddRange(unavailableMessages);
                output.AddRange(warnings);
            }
            output.Reverse();
            return output;
        }
    }
}
