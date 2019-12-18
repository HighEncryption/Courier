namespace Courier.Shared
{
    using System;
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

    public class SendFileResponseMessage : Message
    {
        public override MessageType MessageType => MessageType.SendFileResponse;

        public SendFileResponseContent Content { get; set; }

        public override Stream GetMessageContentStream()
        {
            if (Content == null)
            {
                return new MemoryStream();
            }

            string jsonContent = JsonConvert.SerializeObject(this.Content, Formatting.None);
            byte[] contentBytes = Encoding.UTF8.GetBytes(jsonContent);
            return new MemoryStream(contentBytes);
        }
    }

    public class SendFileResponseContent
    {
        public bool Success { get; set; }

        public string Error { get; set; }
    }

    public class ReceiveFileResponseContent
    {
        public bool Success { get; set; }

        public string Error { get; set; }
    }

    public class SendFileRequestMetadata
    {
        public string Filename { get; set; }

        public bool Overwrite { get; set; }
    }

    public class ReceiveFileRequestMetadata
    {
        public string Filename { get; set; }

        public bool Overwrite { get; set; }
    }

    public class ReceiveFileResponseMetadata
    {
        public string Filename { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastWriteTime { get; set; }

        public long Length { get; set; }
    }
}