namespace Courier.PowerShell.Commands
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Text;
    using System.Threading.Tasks;

    using Courier.Shared;

    using Newtonsoft.Json;

    [Cmdlet(VerbsCommunications.Send, "File")]
    public class SendFile : DispatcherCmdlet
    {
        [Parameter(Mandatory = true)]
        public string FilePath { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override async Task ProcessInternal()
        {
            await Task.Yield();

            if (!File.Exists(this.FilePath))
            {
                throw new FileNotFoundException("The file was not found", this.FilePath);
            }

            if (SerialPortManager.Instance == null)
            {
                SerialPortManager.Initialize(CommunicationMode.Client);
            }

            using (SerialPortInstance serialPort = SerialPortInstance.Create(this))
            {
                SendFileRequestMessage request = new SendFileRequestMessage()
                {
                    MessageNumber = 1,
                    SequenceNumber = 1,
                    SequenceLength = 1,
                    FilePath = this.FilePath,
                    Overwrite = this.Force.ToBool()
                };

                while (true)
                {
                    serialPort.Manager.SendMessage(request);

                    // Request sent, now wait for the response
                    serialPort.Manager.ReceiveMessage();

                    if (OperationContext.Response.Message.MessageType != MessageType.SendFileResponse)
                    {
                        throw new Exception(
                            "Unexpected response type " + OperationContext.Response.Message.MessageType);
                    }

                    if (OperationContext.Response.Message.SequenceNumber == 1)
                    {
                        // This is the first response message in the sequence, so create the response
                        // stream. It should be null at this point.
                        if (OperationContext.Response.ContentStream != null)
                        {
                            throw new Exception("OperationContext.Response.ContentStream is not null");
                        }

                        OperationContext.Response.ContentStream = new MemoryStream();
                    }

                    // Write the content from this message into the content stream
                    OperationContext.Response.ContentStream.Write(
                        OperationContext.Response.Message.ContentBytes,
                        0,
                        OperationContext.Response.Message.ContentBytes.Length);

                    if (OperationContext.Response.Message.SequenceNumber ==
                        OperationContext.Response.Message.SequenceLength)
                    {
                        // We have received all of the requests in the sequence
                        break;
                    }

                    // There are more message in the sequence, so send another request
                    request = new SendFileRequestMessage();
                }

                // We have received all of the requests in the sequence. Allocate a buffer to hold the
                // concatenated response.
                byte[] contentBytes = new byte[OperationContext.Response.ContentStream.Length];

                // Reset the position pointer to the beginning of the stream
                OperationContext.Response.ContentStream.Position = 0;

                // Read the response content into a byte array
                OperationContext.Response.ContentStream.Read(contentBytes, 0, contentBytes.Length);

                string jsonContent = Encoding.UTF8.GetString(contentBytes);
                SendFileResponseContent content =
                    JsonConvert.DeserializeObject<SendFileResponseContent>(jsonContent);

                OperationContext.Clear();

                if (!content.Success)
                {
                    this.DispatcherWriteError(content.Error);
                }
            }
        }
    }
}