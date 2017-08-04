// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public class RFDocumentFileSite : RFFileSite
    {
        [IgnoreDataMember]
        public volatile static int _counter = 1;

        [IgnoreDataMember]
        protected IRFProcessingContext mContext;

        [IgnoreDataMember]
        protected RFKeyDomain mKeyDomain;

        public RFDocumentFileSite(RFKeyDomain keyDomain) : base(RFEnum.NullEnum, null, null)
        {
            mKeyDomain = keyDomain;
        }

        public override void Cancel()
        {
        }

        public override List<RFFileAvailableEvent> CheckSite(List<RFMonitoredFile> monitoredFiles)
        {
            var availableFiles = new List<RFFileAvailableEvent>();
            using (var activity = new RFInputFilesActivity(mContext))
            {
                foreach (var monitoredFile in monitoredFiles)
                {
                    foreach (var file in activity.GetInputFilesList(new List<RFEnum> { monitoredFile.FileKey }))
                    {
                        availableFiles.Add(new RFFileAvailableEvent
                        {
                            FileAttributes = new RFFileTrackedAttributes
                            {
                                FileName = file.Attributes.FileName,
                                FileSize = file.Attributes.FileSize,
                                FullPath = file.UniqueKey,
                                ModifiedDate = file.UpdateTime.UtcDateTime
                            },
                            FileKey = RFEnum.FromString(file.FileKey)
                        });
                    }
                }
            }
            return availableFiles;
        }

        public override void Close()
        {
        }

        public override void DeleteFile(RFFileAvailableEvent file)
        {
            // not deleting from the catalog
            return;
        }

        public override string CombinePath(string directoryName, string fileName)
        {
            return System.IO.Path.Combine(directoryName, fileName);
        }

        public override void MoveFile(string sourcePath, string destinationPath)
        {
            // not archiving
            return;
        }

        public override byte[] GetFile(RFFileAvailableEvent availableFile)
        {
            using (var activity = new RFInputFilesActivity(mContext))
            {
                return activity.GetInputFile(availableFile.FileAttributes.FullPath).Data;
            }
        }

        public override void Open(IRFProcessingContext context)
        {
            mContext = context;
        }

        public override void PutFile(RFFileAvailableEvent file, RFMonitoredFile fileConfig, byte[] data)
        {
            var uniqueKey = GenerateUniqueKey(file);
            var fileEntry = new RFFile
            {
                Attributes = file.FileAttributes,
                FileKey = file.FileKey,
                Data = data,
                UniqueKey = uniqueKey
            };

            mContext.SaveEntry(RFDocument.Create(
                            RFFileKey.Create(mKeyDomain, file.FileKey, uniqueKey),
                            fileEntry));
        }

        protected static string GenerateUniqueKey(RFFileAvailableEvent e)
        {
            return String.Format("{0}_{1}_{2}_{3}",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                _counter++,
                e.FileKey,
                e.FileAttributes.FileName);
        }
    }
}
