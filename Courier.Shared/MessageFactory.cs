namespace Courier.Shared
{
    using System;
    using System.IO;

    public static class MessageFactory
    {
        public static Message Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);

            if (stream.ReadByte() != 0x12 ||
                stream.ReadByte() != 0x34)
            {
                throw new Exception("Invalid BOM!");
            }

            ushort messageNumber = stream.ReadUInt16();
            ushort messageLength = stream.ReadUInt16();
            ushort sequenceNumber = stream.ReadUInt16();
            ushort sequenceLength = stream.ReadUInt16();
            byte messageTypeBytes = (byte)stream.ReadByte();
            ushort contentLength = stream.ReadUInt16();

            byte[] contentBytes = new byte[contentLength];
            stream.Read(contentBytes, 0, contentLength);

            uint crc = stream.ReadUInt32();

            // Verify length
            if (data.Length != messageLength)
            {
                throw new Exception(
                    string.Format(
                        "Message of length {0} does not match expected length {1}",
                        data.Length,
                        messageLength));
            }

            // Did we read all of the stream?
            if (stream.Position != stream.Length)
            {
                throw new Exception(
                    string.Format(
                        "Unexpected data in stream with length {0}. Read {1} bytes",
                        data.Length,
                        stream.Position));
            }

            // Duplicate the original buffer for verifying CRC32
            byte[] buffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);

            // Set the last 4 bytes to null
            for (int i = buffer.Length - 4; i < buffer.Length; i++)
            {
                buffer[i] = 0x00;
            }

            Crc32 crc32 = new Crc32();
            uint computedCrc = crc32.Get(buffer);

            if (computedCrc != crc)
            {
                throw new Exception("Failed CRC32!");
            }

            MessageType messageType = (MessageType) messageTypeBytes;
            Message message;

            switch (messageType)
            {
                case MessageType.ListFilesRequest:
                    message = new GetFilesRequestMessage();
                    break;
                case MessageType.ListFilesResponse:
                    message = new GetFilesResponseMessage();
                    break;
                case MessageType.SendFileRequest:
                    message = new SendFileRequestMessage();
                    break;
                case MessageType.SendFileResponse:
                    message = new SendFileResponseMessage();
                    break;
                case MessageType.ReceiveFileRequest:
                    message = new ReceiveFileRequestMessage();
                    break;
                case MessageType.ReceiveFileResponse:
                    message = new ReceiveFileResponseMessage();
                    break;
                default:
                    throw new NotImplementedException("Unknown message type " + messageType);
            }

            message.MessageNumber = messageNumber;
            message.SequenceNumber = sequenceNumber;
            message.SequenceLength = sequenceLength;

            message.ContentBytes = contentBytes;

            return message;
        }
    }

    public sealed class LogEventArgs : EventArgs
    {
        public string Message { get; }

        public LogEventArgs(string message)
        {
            this.Message = message;
        }
    }
}