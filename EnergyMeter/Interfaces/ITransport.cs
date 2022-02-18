using System;

namespace EnergyMeter.Interfaces
{
    public interface ITransport : IDisposable
    {
        bool IsOpen { get; }

        event EventHandler Error;

        void Open();
        void Close();

        bool Write(byte[] data, int timeoutOffset = 0);
        byte[] Read(int bytesToRead = 0, int timeoutOffset = 0);

        Action<byte[]> OnData { set; }
    }
}
