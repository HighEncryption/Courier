namespace Courier.PowerShell
{
    using System;
    using System.Collections.Concurrent;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Courier.Shared;

    public abstract class DispatcherCmdlet : PSCmdlet
    {
        private readonly BlockingCollection<DispatcherRecord> recordQueue
            = new BlockingCollection<DispatcherRecord>();

        protected override void ProcessRecord()
        {
            Task task = Task.Run(async () =>
            {
                await ProcessInternal().ConfigureAwait(false);
            });

            task.ContinueWith(t => { this.recordQueue.CompleteAdding(); });

            while (!this.recordQueue.IsCompleted)
            {
                if (!this.recordQueue.TryTake(out DispatcherRecord record, -1))
                {
                    break;
                }

                if (record is WriteErrorRecord writeErrorRecord)
                {
                    this.WriteError(
                        new ErrorRecord(
                            new Exception(writeErrorRecord.Text),
                            string.Empty,
                            ErrorCategory.NotSpecified,
                            null));
                }
                else if (record is WriteVerboseRecord writeVerboseRecord)
                {
                    this.WriteVerbose(writeVerboseRecord.Text);
                }
                else if (record is WriteObjectRecord writeObjectRecord)
                {
                    this.WriteObject(writeObjectRecord.Object);
                }
                else
                {
                    throw new NotImplementedException(record.GetType().ToString());
                }
            }

            task.Wait();
        }

        protected abstract Task ProcessInternal();

        protected void DispatcherWriteError(string text)
        {
            this.recordQueue.Add(new WriteErrorRecord(text));
        }

        protected void DispatcherWriteVerbose(string text)
        {
            this.recordQueue.Add(new WriteVerboseRecord(text));
        }

        protected void DispatcherWriteObject(object obj)
        {
            this.recordQueue.Add(new WriteObjectRecord(obj));
        }

        public void OnLog(object sender, LogEventArgs e)
        {
            this.DispatcherWriteVerbose(e.Message);
        }
    }

    public abstract class DispatcherRecord
    {
    }

    public class WriteErrorRecord : DispatcherRecord
    {
        public string Text { get; }

        public WriteErrorRecord(string text)
        {
            this.Text = text;
        }
    }

    public class WriteVerboseRecord : DispatcherRecord
    {
        public string Text { get; }

        public WriteVerboseRecord(string text)
        {
            this.Text = text;
        }
    }

    public class WriteObjectRecord : DispatcherRecord
    {
        public object Object { get; }

        public WriteObjectRecord(object obj)
        {
            this.Object = obj;
        }
    }
}