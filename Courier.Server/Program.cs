namespace Courier.Server
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Courier.Shared;

    using Newtonsoft.Json;

    class Program
    {
        static void Main()
        {
            SerialPortManager serialPortManager = new SerialPortManager(CommunicationMode.Server);

            serialPortManager.Log += (sender, eventArgs) => Console.WriteLine(eventArgs.Message);

            serialPortManager.Connect();

            ManualResetEventSlim exitEvent = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (sender, args) => exitEvent.Set();

            while (!exitEvent.IsSet)
            {
                Console.WriteLine("Waiting for message");

                serialPortManager.ReceiveMessage();

                Console.WriteLine("Message received with type " + OperationContext.Request.Message.MessageType);

                switch (OperationContext.Request.Message.MessageType)
                {
                    case MessageType.ListFilesRequest:
                        HandleListFilesRequest(serialPortManager);
                        break;
                    case MessageType.SendFileRequest:
                        HandleSendFileRequest(serialPortManager);
                        break;
                    case MessageType.ReceiveFileRequest:
                        HandleReceiveFileRequest(serialPortManager);
                        break;
                    default:
                        throw new NotImplementedException(
                            "No handler for message type " + OperationContext.Request.Message.MessageType);
                }
            }
        }

        private static void HandleReceiveFileRequest(SerialPortManager serialPortManager)
        {
            OperationContext.Request.ContentStream =
                new MemoryStream(OperationContext.Request.Message.ContentBytes);

            ReceiveFileResponseMessage response = new ReceiveFileResponseMessage
            {
                Content = new ReceiveFileResponseContent() { Success = true }
            };

            if (OperationContext.Request.Message.SequenceNumber == 1)
            {
                string jsonContent = Encoding.UTF8.GetString(
                    OperationContext.Request.Message.ContentBytes);
                ReceiveFileRequestMetadata receiveFileRequestMetadata =
                    JsonConvert.DeserializeObject<ReceiveFileRequestMetadata>(jsonContent);

                string folderPath = ConfigurationManager.AppSettings["FolderPath"];
                string filePath = Path.Combine(folderPath, receiveFileRequestMetadata.Filename);

                if (!File.Exists(filePath))
                {
                    response.Content.Success = false;
                    response.Content.Error = "File not found";
                    //serialPortManager.SendMessage(response);
                    //return;
                }
                else
                {
                    response.FilePath = filePath;
                }
            }

            serialPortManager.SendMessage(response);
        }

        private static void HandleListFilesRequest(SerialPortManager serialPortManager)
        {
            GetFilesResponseMessage response = new GetFilesResponseMessage();
            serialPortManager.SendMessage(response);
        }

        private static SendFileRequestMetadata sendFileRequestMetadata;
        private static FileStream sendFileRequestFileStream;

        private static void HandleSendFileRequest(SerialPortManager serialPortManager)
        {
            OperationContext.Request.ContentStream =
                new MemoryStream(OperationContext.Request.Message.ContentBytes);

            SendFileResponseMessage response = new SendFileResponseMessage
            {
                Content = new SendFileResponseContent() { Success = true }
            };

            using (OperationContext.Request.ContentStream)
            {
                if (OperationContext.Request.Message.SequenceNumber == 1)
                {
                    byte[] preambleLengthBytes = new byte[sizeof(int)];
                    OperationContext.Request.ContentStream.Read(
                        preambleLengthBytes,
                        0,
                        preambleLengthBytes.Length);

                    int preambleLength = BitConverter.ToInt32(preambleLengthBytes, 0);

                    byte[] preambleBytes = new byte[preambleLength];

                    OperationContext.Request.ContentStream.Read(
                        preambleBytes,
                        0,
                        preambleLength);

                    string jsonContent = Encoding.UTF8.GetString(preambleBytes);
                    sendFileRequestMetadata =
                        JsonConvert.DeserializeObject<SendFileRequestMetadata>(jsonContent);

                    string folderPath = ConfigurationManager.AppSettings["FolderPath"];
                    string filePath = Path.Combine(folderPath, sendFileRequestMetadata.Filename);

                    if (File.Exists(filePath) && !sendFileRequestMetadata.Overwrite)
                    {
                        // TODO: This needs to be returned to the caller
                        //throw new Exception("File exists");

                        response.Content.Success = false;
                        response.Content.Error = "File exists at destination";
                        serialPortManager.SendMessage(response);
                        return;
                    }

                    // This is the first message, so we need to create the response stream
                    sendFileRequestFileStream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write);

                    // Allocate a new buffer to read whatever if remaining in this message
                    byte[] initialFileBuffer = new byte[OperationContext.Request.ContentStream.Length];
                    int bytesRead =
                        OperationContext.Request.ContentStream.Read(
                            initialFileBuffer,
                            0,
                            initialFileBuffer.Length);

                    sendFileRequestFileStream.Write(initialFileBuffer, 0, bytesRead);
                }
                else
                {
                    byte[] buffer = new byte[OperationContext.Request.ContentStream.Length];
                    OperationContext.Request.ContentStream.Read(buffer, 0, buffer.Length);
                    sendFileRequestFileStream.Write(buffer, 0, buffer.Length);
                }

                if (OperationContext.Request.Message.SequenceNumber ==
                    OperationContext.Request.Message.SequenceLength)
                {
                    sendFileRequestFileStream.Close();
                }
            }

            serialPortManager.SendMessage(response);
        }
    }
}
