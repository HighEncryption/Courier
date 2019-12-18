namespace Courier.Shared
{
    using System.IO;

    public class GetFilesRequestMessage : Message
    {
        public override MessageType MessageType => MessageType.ListFilesRequest;

        public override Stream GetMessageContentStream()
        {
            return new MemoryStream(new byte[0]);
        }
    }
}