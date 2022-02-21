using System;
using System.IO.Ports;

using NLog;

using EnergyMeter.Interfaces;

namespace EnergyMeter.Transport
{
    public class SerialTransport : ITransport
    {
        public Action<byte[]> OnData { set; protected get; }

        public event EventHandler Error;

        public bool IsOpen => port.IsOpen;
        public string Id { get; }
        public string Name { get; }

        public int BaseTimeout { get; set; } = 1000;
        public int CharTimeout { get; set; } = 1000;

        protected static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static string[] GetPortNames() => SerialPort.GetPortNames();

        public SerialTransport(string portName, int baudRate) : this(portName, baudRate, Parity.None, 8, StopBits.One)
        { }

        public SerialTransport(string portName, int baudRate, Parity parity, int bits, StopBits stopBits)
        {
            port = new SerialPort();
            port.PortName = portName;
            port.BaudRate = baudRate;
            port.Parity = parity;
            port.DataBits = bits;
            port.StopBits = stopBits;

            Name = portName;
            Id = portName;
        }

        public void Close()
        {
            if (port.IsOpen)
                port.Close();
        }

        public void Dispose()
        {
            Close();
            port.Dispose();
        }

        public void Open()
        {
            if (!port.IsOpen)
                port.Open();
        }

        public virtual byte[] Read(int bytesToRead = 0, int timeoutOffset = 0)
        {
            int bufferLen = bytesToRead <= 0 ? 1024 : bytesToRead;
            byte[] buffer = new byte[bufferLen];

            int l = port.Read(buffer, 0, bufferLen, BaseTimeout + (int)timeoutOffset, CharTimeout);

            byte[] received;
            if (l != bytesToRead)
            {
                received = new byte[l];
                Array.Copy(buffer, received, l);
            }
            else
                received = buffer;

            OnData?.Invoke(received);
            return received;
        }

        public bool Write(byte[] data, int timeoutOffset = 0)
        {
            port.Write(data, 0, data.Length);
            return true;
        }

        protected SerialPort port;

        protected void RaiseError()
            => Error?.Invoke(this, EventArgs.Empty);
    }
}

