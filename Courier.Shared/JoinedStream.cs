namespace Courier.Shared
{
    using System;
    using System.IO;

    public class JoinedStream : Stream
    {
        private readonly Stream preambleStream;
        private readonly Stream innerStream;
        private readonly long length;

        public JoinedStream(
            byte[] preamble,
            Stream innerStream)
        {
            this.length = sizeof(int) + preamble.Length + innerStream.Length;
            byte[] preambleBuffer = new byte[sizeof(int) + preamble.Length];

            Array.Copy(
                BitConverter.GetBytes(preamble.Length),
                preambleBuffer,
                sizeof(int));

            Array.Copy(
                preamble,
                0,
                preambleBuffer,
                4,
                preamble.Length);

            this.preambleStream = new MemoryStream(preambleBuffer);
            this.innerStream = innerStream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            if (this.Position < this.preambleStream.Length)
            {
                // Try to read up to count bytes from the preamble
                bytesRead = this.preambleStream.Read(buffer, offset, count);

                // Move the position forward
                this.Position += bytesRead;
            }

            if (bytesRead < count)
            {
                bytesRead += this.innerStream.Read(buffer, offset + bytesRead, count - bytesRead);

                // Move the position forward
                this.Position += bytesRead;
            }

            return bytesRead;
        }

        #region Stream Members

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => this.length;
        public override long Position { get; set; }

        #endregion

        protected override void Dispose(bool disposing)
        {
            this.preambleStream?.Dispose();
            this.innerStream?.Dispose();

            base.Dispose(disposing);
        }
    }
}