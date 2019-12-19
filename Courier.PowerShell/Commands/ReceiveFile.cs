namespace Courier.PowerShell.Commands
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Text;
    using System.Threading.Tasks;

    using Courier.Shared;

    using Newtonsoft.Json;

    [Cmdlet(VerbsCommunications.Receive, "File")]
    public class ReceiveFile : DispatcherCmdlet
    {
        [Parameter(Mandatory = true)]
        public string FileName { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override async Task ProcessInternal()
        {
            await Task.Yield();

            if (SerialPortManager.Instance == null)
            {
                SerialPortManager.Initialize(CommunicationMode.Client);
            }

            using (SerialPortInstance serialPort = SerialPortInstance.Create(this))
            {
                ReceiveFileRequestMessage request = new ReceiveFileRequestMessage()
                {
                    MessageNumber = 1,
                    SequenceNumber = 1,
                    SequenceLength = 1,
                    FileName = this.FileName
                };

                while (true)
                {
                    serialPort.Manager.SendMessage(request);

                    // Request sent, now wait for the response
                    serialPort.Manager.ReceiveMessage();

                    if (OperationContext.Response.Message.MessageType != MessageType.ReceiveFileResponse)
                    {
                        throw new Exception(
                            "Unexpected response type " + OperationContext.Response.Message.MessageType);
                    }

                    if (OperationContext.Response.Message.SequenceNumber == 1)
                    {
                        MemoryStream contentStream = new MemoryStream(
                            OperationContext.Response.Message.ContentBytes);

                        int metadataLength = contentStream.ReadInt32();
                        byte[] metadataBytes = new byte[metadataLength];
                        contentStream.Read(metadataBytes, 0, metadataLength);

                        string jsonMetadataContent = Encoding.UTF8.GetString(metadataBytes);
                        ReceiveFileResponseMetadata metadata =
                            JsonConvert.DeserializeObject<ReceiveFileResponseMetadata>(jsonMetadataContent);

                        string filepath = Path.Combine(Environment.CurrentDirectory, metadata.Filename);

                        if (File.Exists(filepath) && !this.Force.ToBool())
                        {
                            throw new Exception($"File '{filepath}' exists");
                        }

                        OperationContext.Response.ContentStream = new FileStream(
                            filepath,
                            FileMode.Create,
                            FileAccess.Write);

                        byte[] remainingBytes = new byte[Message.MessageContentMaxLength];
                        int bytesRead = contentStream.Read(remainingBytes, 0, remainingBytes.Length);

                        OperationContext.Response.ContentStream.Write(remainingBytes, 0, bytesRead);
                    }
                    else
                    {
                        // Write the content from this message into the content stream
                        OperationContext.Response.ContentStream.Write(
                            OperationContext.Response.Message.ContentBytes,
                            0,
                            OperationContext.Response.Message.ContentBytes.Length);
                    }

                    if (OperationContext.Response.Message.SequenceNumber ==
                        OperationContext.Response.Message.SequenceLength)
                    {
                        // We have received all of the requests in the sequence
                        break;
                    }

                    // There are more message in the sequence, so send another request
                    request = new ReceiveFileRequestMessage();
                }

                OperationContext.Response.ContentStream.Flush();

                OperationContext.Clear();
            }
        }
    }
}