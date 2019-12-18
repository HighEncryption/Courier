namespace Courier.Shared
{
    using System.IO;

    public static class OperationContext
    {
        public static MessageContext Request { get; set; }

        public static MessageContext Response { get; set; }

        /// <summary>
        /// The number of messages we expect to be in the sequence
        /// </summary>
        public static ushort SequenceCount { get; set; }

        public static void Clear()
        {
            if (Request.ContentStream != null)
            {
                Request.ContentStream.Dispose();
                Request.ContentStream = null;
            }

            if (Response.ContentStream != null)
            {
                Response.ContentStream.Dispose();
                Response.ContentStream = null;
            }

            Request.Message = null;
            Response.Message = null;
            SequenceCount = 0;
        }
    }

    public class MessageContext
    {
        public Message Message { get; set; }

        public Stream ContentStream { get; set; }
    }
}