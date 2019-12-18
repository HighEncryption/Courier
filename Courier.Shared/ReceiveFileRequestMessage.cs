namespace Courier.Shared
{
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public class ReceiveFileRequestMessage : Message
    {
        public string FileName { get; set; }

        public override MessageType MessageType => MessageType.ReceiveFileRequest;

        public override Stream GetMessageContentStream()
        {
            ReceiveFileRequestMetadata metadata = new ReceiveFileRequestMetadata()
            {
                Filename = Path.GetFileName(this.FileName),
            };

            string jsonContent = JsonConvert.SerializeObject(metadata, Formatting.None);
            byte[] metadataBytes = Encoding.UTF8.GetBytes(jsonContent);

            return new MemoryStream(metadataBytes);
        }
    }
}