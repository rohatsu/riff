// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.IO;

namespace RIFF.Core
{
    public static class RFStreamHelpers
    {
        public static byte[] ReadBytes(Stream input)
        {
            byte[] buffer = new byte[65536];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
