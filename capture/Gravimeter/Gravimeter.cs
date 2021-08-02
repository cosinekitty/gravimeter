using System;
using System.Globalization;
using System.Text;
using RJCP.IO.Ports;

namespace CosineKitty.Gravimetry
{
    public delegate void LineReceivedDelegate(string line);
    public delegate void CommFailureDelegate();

    public class Gravimeter : IDisposable
    {
        private SerialPortStream port;
        private const int BUFFER_LENGTH = 256;
        private byte[] buffer = new byte[BUFFER_LENGTH];
        private StringBuilder sb = new StringBuilder();

        public event LineReceivedDelegate LineReceivedEvent;
        public event CommFailureDelegate CommFailureEvent;

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
                if (c == '\n' || c == '\r')
                {
                    string line = sb.ToString().TrimEnd();
                    if (line.Length > 0)
                    {
                        bool valid = false;
                        int index = line.IndexOf('#');
                        if (index >= 0)
                        {
                            // Verify Fletcher checksum.
                            // @e 0 0000079275#4ee9
                            string payload = line.Substring(0, index);
                            string sumtext = line.Substring(index + 1);
                            if (int.TryParse(sumtext, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int sentsum))
                            {
                                int calcsum = CalculateChecksum(payload);
                                if (sentsum == calcsum)
                                {
                                    valid = true;
                                    if (LineReceivedEvent != null)
                                        LineReceivedEvent(payload);
                                }
                                else
                                {
                                    //Console.WriteLine($"!!! sentsum={sentsum:X}, calcsum={calcsum:X}, line={line}");
                                }
                            }
                        }

                        if (!valid)
                        {
                            if (CommFailureEvent != null)
                                CommFailureEvent();
                        }
                    }
                    sb.Clear();
                }
                else if (c != '\r')
                {
                    sb.Append(c);
                }
            }
        }

        private static int CalculateChecksum(string text)
        {
            int sum1 = 0xd3;
            int sum2 = 0x95;
            foreach (char c in text)
            {
                sum1 = (sum1 + (int)c) % 255;
                sum2 = (sum2 + sum1) % 255;
            }
            return (sum2 << 8) | sum1;
        }

        public void Write(string s)
        {
            port.Write(s);
        }
    }
}
