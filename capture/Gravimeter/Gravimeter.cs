using System;
using System.Text;
using RJCP.IO.Ports;

namespace CosineKitty.Gravimetry
{
    public delegate void LineReceivedDelegate(string line);

    public class Gravimeter : IDisposable
    {
        private SerialPortStream port;
        private const int BUFFER_LENGTH = 256;
        private byte[] buffer = new byte[BUFFER_LENGTH];
        private StringBuilder sb = new StringBuilder();

        public event LineReceivedDelegate LineReceivedEvent;

        public Gravimeter(string serialPortPath)
        {
            port = new SerialPortStream(serialPortPath, 115200, 8, Parity.None, StopBits.One);
            port.Open();
            port.DiscardInBuffer();
            port.DataReceived += OnDataReceived;
        }

        public void Dispose()
        {
            if (port != null)
            {
                port.DataReceived -= OnDataReceived;
                port.Dispose();
                port = null;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            while (port.BytesToRead > 0)
            {
                char c = (char)port.ReadByte();
                if (c == '\n')
                {
                    if (LineReceivedEvent != null)
                        LineReceivedEvent(sb.ToString());

                    sb.Clear();
                }
                else if (c != '\r')
                {
                    sb.Append(c);
                }
            }
        }

        public void Write(string s)
        {
            port.Write(s);
        }
    }
}
