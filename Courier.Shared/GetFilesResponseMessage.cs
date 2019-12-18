namespace Courier.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public class GetFilesResponseMessage : Message
    {
        public override MessageType MessageType => MessageType.ListFilesResponse;

        public override Stream GetMessageContentStream()
        {
            byte[] contentBytes = GetContentBytes();
            return new MemoryStream(contentBytes);
        }

        private byte[] GetContentBytes()
        {
            GetFilesResponseContent content = new GetFilesResponseContent()
            {
                Files = new List<FileInfo>()
            };

            try
            {
                string folderPath = ConfigurationManager.AppSettings["FolderPath"];

                DirectoryInfo directory = new DirectoryInfo(folderPath);

                System.IO.FileInfo[] files = directory.GetFiles();
                foreach (System.IO.FileInfo file in files)
                {
                    content.Files.Add(
                        new FileInfo()
                        {
                            Name = file.Name,
                            Created = file.CreationTime,
                            LastModified = file.LastWriteTime,
                            Size = file.Length
                        });
                }

                content.Success = true;
            }
            catch (Exception e)
            {
                content.Success = false;
                content.Error = e.Message;
            }

            string jsonContent =
                JsonConvert.SerializeObject(content, Formatting.None);

            byte[] contentBytes = Encoding.UTF8.GetBytes(jsonContent);

            return contentBytes;
        }
    }

    public class GetFilesResponseContent
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public List<FileInfo> Files { get; set; }
    }

    public class FileInfo
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }
}