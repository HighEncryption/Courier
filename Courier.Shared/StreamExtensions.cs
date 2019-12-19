namespace Courier.Shared
{
    using System;
    using System.IO;

    public static class StreamExtensions
    {
        public static ushort ReadUInt16(this Stream stream)
        {
            byte[] bytes = new byte[2];
            stream.Read(bytes, 0, 2);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}