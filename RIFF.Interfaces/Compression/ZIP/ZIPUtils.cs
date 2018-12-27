// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Ionic.Zip;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RIFF.Interfaces.Compression.ZIP
{
    public static class ZIPUtils
    {
        public static bool IsZip(Stream stream)
        {
            return Ionic.Zip.ZipFile.IsZipFile(stream, false);
        }

        public static List<Tuple<RFFileTrackedAttributes, byte[]>> UnzipArchive(Stream sourceStream, string password = null)
        {
            var contents = new List<Tuple<RFFileTrackedAttributes, byte[]>>();
            var zipFile = Ionic.Zip.ZipFile.Read(sourceStream);
            foreach (var entry in zipFile.Entries.Where(e => !e.IsDirectory))
            {
                using (var ms = new MemoryStream())
                {
                    if (entry.Encryption != EncryptionAlgorithm.None && !string.IsNullOrWhiteSpace(password))
                    {
                        entry.ExtractWithPassword(ms, password);
                    }
                    else
                    {
                        entry.Extract(ms);
                    }

                    contents.Add(new Tuple<RFFileTrackedAttributes, byte[]>(new RFFileTrackedAttributes
                    {
                        FileName = Path.GetFileName(entry.FileName),
                        FileSize = entry.UncompressedSize,
                        ModifiedDate = entry.LastModified,
                        FullPath = entry.FileName
                    }, ms.ToArray()));
                }
            }
            return contents;
        }

        public static void ZipFile(string sourceDirectory, string sourceFileName, string destDirectory, string password = null)
        {
            var destFileName = Path.GetFileNameWithoutExtension(sourceFileName) + ".zip";
            var sourcePath = Path.Combine(sourceDirectory, sourceFileName);
            var destPath = Path.Combine(destDirectory, destFileName);
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
            if (!File.Exists(sourcePath))
            {
                throw new RFSystemException(typeof(ZIPUtils), "Source file not found: {0}", sourcePath);
            }
            var zipFile = new ZipFile(destPath);
            if (!string.IsNullOrWhiteSpace(password))
            {
                zipFile.Password = password;
                zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
            }
            zipFile.AddFile(sourcePath, String.Empty);
            zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
            zipFile.Save();
        }

        public static void ZipFiles(string outputFile, Dictionary<string, byte[]> sourceStreams, string password = null)
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            if (sourceStreams == null || sourceStreams.Count == 0)
            {
                throw new RFSystemException(typeof(ZIPUtils), "No source streams for {0}", outputFile);
            }
            var zipFile = new ZipFile(outputFile);
            zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
            if (!string.IsNullOrWhiteSpace(password))
            {
                zipFile.Password = password;
                zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
            }
            foreach (var stream in sourceStreams)
            {
                zipFile.AddEntry(stream.Key, stream.Value);
            }
            zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
            zipFile.Save();
        }

        public static void ZipStream(Stream sourceStream, string sourceFileName, string destDirectory, string password = null)
        {
            var destFileName = Path.GetFileNameWithoutExtension(sourceFileName) + ".zip";
            var destPath = Path.Combine(destDirectory, destFileName);
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
            if (sourceStream == null || sourceStream.Length == 0)
            {
                throw new RFSystemException(typeof(ZIPUtils), "Source stream for {0} is empty", sourceFileName);
            }
            var zipFile = new ZipFile(destPath);
            zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
            if (!string.IsNullOrWhiteSpace(password))
            {
                zipFile.Password = password;
                zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
            }
            zipFile.AddEntry(sourceFileName, sourceStream);
            zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
            zipFile.Save();
        }
    }
}
