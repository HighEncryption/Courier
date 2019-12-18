namespace Courier.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO.Ports;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    public enum CommunicationMode
    {
        Undefined,
        Client,
        Server
    }

    public class SerialPortManager
    {
        private SerialPort serialPort;

        private string comPort;
        private string baudRate;
        private string parity;
        private string dataBits;
        private string stopBits;

        public bool IsConnected { get; private set; }

        public event EventHandler<LogEventArgs> Log;

        private List<byte> receivedBytes = new List<byte>();
        private ushort expectedMessageLength;
        private ManualResetEventSlim messageReceived;
        private CommunicationMode Mode { get; }
        private Message receivedMessage;

        public static SerialPortManager Instance { get; private set; }

        public static void Initialize(CommunicationMode mode)
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("SerialPortManager already initialized!");
            }

            Instance = new SerialPortManager(mode);
        }

        public SerialPortManager(CommunicationMode mode)
        {
            //this.receiveBuffer = new List<byte>();
            this.Mode = mode;
        }

        public void Connect(string assemblyLocation = null)
        {
            if (this.IsConnected)
            {
                throw new InvalidOperationException("Already connected");
            }

            if (assemblyLocation != null)
            {
                AppDomain.CurrentDomain.SetData(
                    "APP_CONFIG_FILE",
                    assemblyLocation + ".config");

                // ReSharper disable PossibleNullReferenceException
                typeof(ConfigurationManager)
                    .GetField("s_initState", BindingFlags.NonPublic |
                                             BindingFlags.Static)
                    .SetValue(null, 0);

                typeof(ConfigurationManager)
                    .GetField("s_configSystem", BindingFlags.NonPublic |
                                                BindingFlags.Static)
                    .SetValue(null, null);

                typeof(ConfigurationManager)
                    .Assembly.GetTypes()
                    .First(x => x.FullName ==
                                "System.Configuration.ClientConfigPaths")
                    .GetField("s_current", BindingFlags.NonPublic |
                                           BindingFlags.Static)
                    .SetValue(null, null);
                // ReSharper restore PossibleNullReferenceException
            }

            this.comPort = ConfigurationManager.AppSettings["ComPort"];
            this.baudRate = ConfigurationManager.AppSettings["BaudRate"];
            this.parity = ConfigurationManager.AppSettings["Parity"];
            this.dataBits = ConfigurationManager.AppSettings["DataBits"];
            this.stopBits = ConfigurationManager.AppSettings["StopBits"];

            WriteVerbose("COM Port:  " + this.comPort);
            WriteVerbose("Baud Rate: " + this.baudRate);
            WriteVerbose("Parity:    " + this.parity);
            WriteVerbose("Data Bits: " + this.dataBits);
            WriteVerbose("Stop Bits: " + this.stopBits);

            this.serialPort = new SerialPort(
                this.comPort,
                int.Parse(this.baudRate),
                (Parity)Enum.Parse(typeof(Parity), this.parity),
                int.Parse(this.dataBits),
                (StopBits)Enum.Parse(typeof(StopBits), this.stopBits));

            this.serialPort.Open();

            this.IsConnected = true;

            this.WriteVerbose("Connect() complete");
        }

        private void WriteVerbose(string message)
        {
            this.Log?.Invoke(this, new LogEventArgs(message));
        }

        public void SendMessage(Message message)
        {
            switch (this.Mode)
            {
                case CommunicationMode.Client:
                    this.SendRequestMessage(message);
                    break;
                case CommunicationMode.Server:
                    this.SendResponseMessage(message);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void SendRequestMessage(Message message)
        {
            if (OperationContext.Request == null)
            {
                OperationContext.Request = new MessageContext();
            }

            OperationContext.Request.Message = message;

            if (message.SequenceNumber == 1)
            {
                // This is the first message in the sequence, so we need to get the byte
                // stream for the message content. Ensure that there isn't an existing
                // byte stream (since this should have been cleaned up).
                if (OperationContext.Request.ContentStream != null)
                {
                    throw new Exception("Request content stream existed when it shouldn't!");
                }

                OperationContext.Request.ContentStream = message.GetMessageContentStream();

                if (OperationContext.Request.ContentStream == null)
                {
                    throw new Exception("GetMessageContentStream() returned null");
                }

                // Determine how many messages we will need
                ushort sequenceCount =
                    Convert.ToUInt16(
                        Math.Ceiling(
                            (double)OperationContext.Request.ContentStream.Length /
                            Message.MessageContentMaxLength));

                if (sequenceCount == 0)
                {
                    sequenceCount = 1;
                }

                if (sequenceCount > OperationContext.SequenceCount)
                {
                    OperationContext.SequenceCount = sequenceCount;
                }

                message.SequenceLength = sequenceCount;
            }
            else
            {
                message.MessageNumber = OperationContext.Response.Message.MessageNumber;
                message.SequenceNumber = (ushort)(OperationContext.Response.Message.SequenceNumber + 1);
                message.SequenceLength = OperationContext.Response.Message.SequenceLength;
            }

            // Read one message's worth of data from the content stream
            byte[] messageContent = new byte[Message.MessageContentMaxLength];
            int bytesRead = OperationContext.Request.ContentStream.Read(
                messageContent,
                0,
                Message.MessageContentMaxLength);

            // If we read less than a buffer's worth, resize the buffer
            if (bytesRead < messageContent.Length)
            {
                Array.Resize(ref messageContent, bytesRead);
            }


            byte[] requestData = message.Write(messageContent);

            this.WriteVerbose("SendRequestMessage sending " + requestData.Length + " bytes");

            this.serialPort.Write(requestData, 0, requestData.Length);

            this.WriteVerbose("SendRequestMessage finished");
        }

        private void SendResponseMessage(Message message)
        {
            if (OperationContext.Response == null)
            {
                OperationContext.Response = new MessageContext();
            }

            OperationContext.Response.Message = message;

            if (OperationContext.Request.Message.SequenceNumber == 1)
            {
                // This is the first message in the sequence, so we need to get the byte
                // stream for the message content. Ensure that there isn't an existing
                // byte stream (since this should have been cleaned up).
                if (OperationContext.Response.ContentStream != null)
                {
                    throw new Exception("Response content stream existed when it shouldn't!");
                }

                OperationContext.Response.ContentStream = message.GetMessageContentStream();

                if (OperationContext.Response.ContentStream == null)
                {
                    throw new Exception("GetMessageContentStream() returned null");
                }

                // Default to the sequence length provided in the request
                OperationContext.SequenceCount = OperationContext.Request.Message.SequenceLength;

                // Determine how many messages we will need
                ushort sequenceCount =
                    Convert.ToUInt16(
                        Math.Ceiling(
                            (double)OperationContext.Response.ContentStream.Length /
                            Message.MessageContentMaxLength));

                if (sequenceCount > OperationContext.SequenceCount)
                {
                    OperationContext.SequenceCount = sequenceCount;
                }
            }

            // Read one message's worth of data from the content stream
            byte[] messageContent = new byte[Message.MessageContentMaxLength];
            int bytesRead = OperationContext.Response.ContentStream.Read(
                messageContent,
                0,
                Message.MessageContentMaxLength);

            // If we read less than a buffer's worth, resize the buffer
            if (bytesRead < messageContent.Length)
            {
                Array.Resize(ref messageContent, bytesRead);
            }

            // If this is after the first message in a multi-message sequence, ensure that the 
            // sequence length from the request matches the expected sequence count
            if (OperationContext.Request.Message.SequenceNumber > 1 &&
                OperationContext.SequenceCount > 1 &&
                OperationContext.Request.Message.SequenceLength != OperationContext.SequenceCount)
            {
                throw new Exception(
                    string.Format(
                        "Request contains a sequence length of {0} but the expected sequence count is {1}",
                        OperationContext.Response.Message.SequenceLength,
                        OperationContext.SequenceCount));
            }

            if (OperationContext.Response.ContentStream.Position ==
                OperationContext.Response.ContentStream.Length
                &&
                OperationContext.Request.Message.SequenceNumber <
                OperationContext.SequenceCount)
            {
                // We have reached the end of the buffer, but we did not send enough messages according
                // to the expected sequence count
                throw new Exception(
                    string.Format(
                        "End of content string reached, but we are on sequence of {0} of {1}",
                        OperationContext.Request.Message.SequenceNumber,
                        OperationContext.SequenceCount));
            }

            if (OperationContext.Request.Message.SequenceNumber == OperationContext.SequenceCount
                &&
                OperationContext.Response.ContentStream.Position <
                OperationContext.Response.ContentStream.Length)
            {
                // We we sending the last message in the sequence, but we haven't read all of the data
                // from the content stream
                throw new Exception(
                    string.Format(
                        "Sending final sequence {0}, but only read {1} of {2} bytes from content stream",
                        OperationContext.Request.Message.SequenceNumber,
                        OperationContext.Response.ContentStream.Position,
                        OperationContext.Response.ContentStream.Length));
            }

            message.MessageNumber = OperationContext.Request.Message.MessageNumber;
            message.SequenceNumber = OperationContext.Request.Message.SequenceNumber;
            message.SequenceLength = OperationContext.SequenceCount;

            byte[] requestData = message.Write(messageContent);

            this.WriteVerbose("SendResponseMessage sending " + requestData.Length + " bytes");

            this.serialPort.Write(requestData, 0, requestData.Length);

            this.WriteVerbose("SendResponseMessage finished");

            if (message.SequenceNumber == message.SequenceLength)
            {
                // We are on the last message of the sequence, so clear the context
                OperationContext.Clear();
            }
        }

        public void ReceiveMessage()
        {
            // Set up the instance variables used when processing read data
            this.messageReceived = new ManualResetEventSlim(false);
            this.receivedBytes = new List<byte>();
            this.expectedMessageLength = 0;
            this.receivedMessage = null;

            this.serialPort.DataReceived += SerialPortOnDataReceived;

            this.WriteVerbose("Waiting for message received event");

            this.messageReceived.Wait();

            this.WriteVerbose("Message received complete");

            this.serialPort.DataReceived -= SerialPortOnDataReceived;

            switch (this.Mode)
            {
                case CommunicationMode.Server:
                    if (OperationContext.Request == null)
                    {
                        OperationContext.Request = new MessageContext();
                    }

                    OperationContext.Request.Message = this.receivedMessage;
                    break;
                case CommunicationMode.Client:
                    if (OperationContext.Response == null)
                    {
                        OperationContext.Response = new MessageContext();
                    }

                    OperationContext.Response.Message = this.receivedMessage;

                    if (this.receivedMessage.SequenceNumber == 1)
                    {
                        OperationContext.SequenceCount = this.receivedMessage.SequenceLength;
                    }

                    break;
                default:
                    throw new NotImplementedException("Invalid mode " + this.Mode);
            }
        }

        private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(buffer, 0, buffer.Length);

            this.receivedBytes.AddRange(buffer);

            //this.WriteVerbose("[" + buffer.Length + "] ");

            // Is the expected message length not yet know?
            if (this.expectedMessageLength == 0)
            {
                // Do we have enough bytes to determine the expected length?
                if (this.receivedBytes.Count >= 6)
                {
                    byte[] temp = this.receivedBytes.ToArray();
                    if (temp[0] != 0x12 || temp[1] != 0x34)
                    {
                        throw new Exception("Invalid BOM!");
                    }

                    // First check that the BOM bytes are correct
                    this.expectedMessageLength = BitConverter.ToUInt16(temp, 4);

                    this.WriteVerbose("Expected "+ this.expectedMessageLength + " bytes");
                }
            }

            if (this.expectedMessageLength != 0 && 
                this.receivedBytes.Count >= this.expectedMessageLength)
            {
                this.WriteVerbose("Message receive bytes complete");

                // We have all of the bytes in the message
                this.receivedMessage = MessageFactory.Deserialize(
                    this.receivedBytes.ToArray());

                this.WriteVerbose(
                    string.Format(
                        "Received message with MessageNumber={0}, SeqNum={1}, SeqLen={2}",
                        this.receivedMessage.MessageNumber,
                        this.receivedMessage.SequenceNumber,
                        this.receivedMessage.SequenceLength));

                this.messageReceived.Set();
            }
        }
    }
}