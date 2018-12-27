// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;

namespace RIFF.Interfaces.Protocols
{
    public interface IFTPConnection
    {
        void Cancel();

        void DeleteFile(string fullPath);

        void Disconnect();

        List<RFFileTrackedAttributes> ListFiles(string directory, string regexString = null, bool recursive = false);

        void PutFile(string directory, string fileName, byte[] data);

        byte[] RetrieveFile(string filePath);

        void MoveFile(string sourcePath, string destinationPath);
    }
}
