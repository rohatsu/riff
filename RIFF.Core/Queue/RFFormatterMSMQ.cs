// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.IO;
#if !NETSTANDARD2_0
using System.Messaging;

namespace RIFF.Core
{
    internal class RFFormatterMSMQ : IMessageFormatter
    {
        public bool CanRead(Message message)
        {
            return true;
        }

        public object Clone()
        {
            return new RFFormatterMSMQ();
        }

        public object Read(Message message)
        {
            var reader = new StreamReader(message.BodyStream);
            var contentType = reader.ReadLine();
            var content = reader.ReadToEnd();
            return RFXMLSerializer.DeserializeContract(contentType, content);
        }

        public void Write(Message message, object obj)
        {
            message.BodyStream = new MemoryStream();
            var writer = new StreamWriter(message.BodyStream);
            writer.WriteLine(obj.GetType().FullName);
            writer.WriteLine(RFXMLSerializer.SerializeContract(obj));
            writer.Flush();
        }
    }
}
#endif
