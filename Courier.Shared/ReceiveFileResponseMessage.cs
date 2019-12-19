namespace Courier.Shared
{
    using System;
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public class ReceiveFileResponseMessage : Message
    {
        public override MessageType MessageType => MessageType.ReceiveFileResponse;

        public ReceiveFileResponseContent Content { get; set; }

        public string FilePath { get; set; }

        public override Stream GetMessageContentStream()
        {
            string folderPath = Path.GetDirectoryName(this.FilePath);

            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            string filename = Path.GetFileName(this.FilePath);

            System.IO.FileInfo[] fileInfo = directoryInfo.GetFiles(filename);

            ReceiveFileResponseMetadata responseMetadata = new ReceiveFileResponseMetadata()
            {
                Filename = fileInfo[0].Name,
                CreationTime = fileInfo[0].CreationTime,
                LastWriteTime = fileInfo[0].LastWriteTime,
                Length = fileInfo[0].Length
            };

            string jsonMetadataContent = JsonConvert.SerializeObject(responseMetadata);
            byte[] metadataBytes = Encoding.UTF8.GetBytes(jsonMetadataContent);

            FileStream fileStream = new FileStream(
                FilePath,
                FileMode.Open,
                FileAccess.Read);

            var stream = new JoinedStream(
                metadataBytes,
                fileStream);

            stream.Position = 0;

            return stream;
        }
    }

    public class ReceiveFileResponseContent
    {
        public bool Success { get; set; }

        public string Error { get; set; }
    }

    public class ReceiveFileResponseMetadata
    {
        public string Filename { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastWriteTime { get; set; }

        public long Length { get; set; }
    }
}