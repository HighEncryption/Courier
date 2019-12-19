namespace Courier.Shared
{
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public class SendFileResponseMessage : Message
    {
        public override MessageType MessageType => MessageType.SendFileResponse;

        public SendFileResponseContent Content { get; set; }

        public override Stream GetMessageContentStream()
        {
            if (this.Content == null)
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

}