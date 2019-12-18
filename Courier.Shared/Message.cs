namespace Courier.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public enum MessageType : byte
    {
        Undefined = 0x0,
        SendFileRequest = 0x01,
        SendFileResponse = 0x02,
        ReceiveFileRequest = 0x03,
        ReceiveFileResponse = 0x04,
        ListFilesRequest = 0x05,
        ListFilesResponse = 0x06,
    }

    public abstract class Message
    {
        public const int MessageContentMaxLength = 2048;

        public ushort MessageNumber { get; set; }

        public ushort SequenceNumber { get; set; }

        public ushort SequenceLength { get; set; }

        public abstract MessageType MessageType { get; }

        public byte[] ContentBytes { get; set; }

        public abstract Stream GetMessageContentStream();

        public byte[] Write(byte[] messageContent)
        {
            List<byte> bytes = new List<byte>();

            if (this.MessageNumber == 0)
            {
                throw new Exception("Failed to set message number");
            }

            if (this.SequenceNumber == 0)
            {
                throw new Exception("Failed to set sequence number");
            }

            if (this.SequenceLength == 0)
            {
                throw new Exception("Failed to set sequence length");
            }

            // Add the BOM bytes to let us know that this is the beginning of a message
            bytes.Add(0x12);
            bytes.Add(0x34);

            // Add 2 bytes for the message number. Write these as null for now, and we will
            // update it once we have the entire message built.
            bytes.AddRange(BitConverter.GetBytes(this.MessageNumber));

            // Add 2 bytes for the message length. First remember the position in the byte array
            // where the message length is written, then write these as null for now, and we will
            // update it once we have the entire message built.
            int messageLengthOffset = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes((ushort)0));

            // Add 2 bytes for the sequence number
            bytes.AddRange(BitConverter.GetBytes(this.SequenceNumber));

            // Add 2 bytes for the sequence count
            bytes.AddRange(BitConverter.GetBytes(this.SequenceLength));

            // Add the message type
            bytes.Add((byte)this.MessageType);

            // Add 2 bytes for the message content length
            bytes.AddRange(BitConverter.GetBytes((ushort)messageContent.Length));

            // Add the bytes for the message content
            bytes.AddRange(messageContent);

            // Add 4 bytes for the CRC32, set to 0 during crc calculation
            bytes.AddRange(BitConverter.GetBytes((uint)0));

            // Convert to an array, calculate the message length, set it payload
            byte[] payload = bytes.ToArray();
            Array.Copy(
                BitConverter.GetBytes(payload.Length),
                0,
                payload,
                messageLengthOffset,
                sizeof(ushort));

            // Calculate the CRC32 for the message and copy to payload
            Crc32 crc32 = new Crc32();
            uint crc = crc32.Get(payload);
            byte[] crcBytes = BitConverter.GetBytes(crc);
            Array.Copy(crcBytes, 0, payload, payload.Length - 4, 4);

            return payload;
        }
    }
}