// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.IO;

namespace RIFF.Core
{
    internal class RFFormatterRabbitMQ
    {
        public object Read(System.ReadOnlyMemory<byte> message)
        {
            var reader = new StreamReader(new MemoryStream(message.ToArray()));
            var contentType = reader.ReadLine();
            var content = reader.ReadToEnd();
            return RFXMLSerializer.DeserializeContract(contentType, content);
        }

        public System.ReadOnlyMemory<byte> Write(object obj)
        {
            var bodyStream = new MemoryStream();
            var writer = new StreamWriter(bodyStream);
            writer.WriteLine(obj.GetType().FullName);
            writer.WriteLine(RFXMLSerializer.SerializeContract(obj));
            writer.Flush();
            return bodyStream.ToArray();
        }
    }
}
