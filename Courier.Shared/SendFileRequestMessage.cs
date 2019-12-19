namespace Courier.Shared
{
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public class SendFileRequestMessage : Message
    {
        public string FilePath { get; set; }

        public bool Overwrite { get; set; }

        public override MessageType MessageType => MessageType.SendFileRequest;

        public override Stream GetMessageContentStream()
        {
            SendFileRequestMetadata metadata = new SendFileRequestMetadata()
            {
                Filename = Path.GetFileName(this.FilePath),
                Overwrite = this.Overwrite
            };

            string jsonContent = JsonConvert.SerializeObject(metadata, Formatting.None);
            byte[] metadataBytes = Encoding.UTF8.GetBytes(jsonContent);

            return new JoinedStream(
                metadataBytes,
                new FileStream(this.FilePath, FileMode.Open, FileAccess.Read));
        }
    }

    public class SendFileRequestMetadata
    {
        public string Filename { get; set; }

        public bool Overwrite { get; set; }
    }
}