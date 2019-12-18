namespace Courier.PowerShell
{
    using System;
    using System.Reflection;

    using Courier.Shared;

    public class SerialPortInstance : IDisposable
    {
        private DispatcherCmdlet cmdlet;

        public SerialPortManager Manager
            => SerialPortManager.Instance;

        private SerialPortInstance()
        {
        }

        public void Dispose()
        {
            SerialPortManager.Instance.Log -= this.cmdlet.OnLog;
        }

        public static SerialPortInstance Create(DispatcherCmdlet cmdlet)
        {
            SerialPortInstance instance = new SerialPortInstance();
            instance.cmdlet = cmdlet;

            if (!SerialPortManager.Instance.IsConnected)
            {
                SerialPortManager.Instance.Connect(
                    Assembly.GetExecutingAssembly().Location);
            }

            SerialPortManager.Instance.Log += cmdlet.OnLog;

            return instance;
        }
    }
}
