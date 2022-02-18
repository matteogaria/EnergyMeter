
namespace System.IO.Ports
{
    static class SerialPort_Extensions
    {
        public static int Read(this SerialPort port, byte[] buffer, int offset, int count, int initialTimeout, int byteTimeout = 100)
        {
            port.ReadTimeout = initialTimeout;

            int p = 0;
            int n;
            while (p < count)
            {
                try
                {
                    n = port.Read(buffer, p + offset, buffer.Length - p);
                }
                catch (TimeoutException)
                {
                    break;
                }
                p += n;
                port.ReadTimeout = byteTimeout;
            }

            return p;
        }
    }
}
