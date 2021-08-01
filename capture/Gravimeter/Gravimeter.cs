using System;
using RJCP.IO.Ports;

namespace CosineKitty.Gravimetry
{
    public class Gravimeter : IDisposable
    {
        private SerialPortStream port;

        public Gravimeter(string serialPortPath)
        {
            port = new SerialPortStream(serialPortPath, 115200, 8, Parity.None, StopBits.One);
            port.Open();
            SkipOverInput();
        }

        public void SkipOverInput()
        {
            while (port.BytesToRead > 0)
            {
                port.ReadByte();
            }
        }

        public string ReadLine()
        {
            return port.ReadLine().Trim();
        }

        public void Write(string s)
        {
            port.Write(s);
        }

        public void Dispose()
        {
            if (port != null)
            {
                port.Dispose();
                port = null;
            }
        }
    }
}
