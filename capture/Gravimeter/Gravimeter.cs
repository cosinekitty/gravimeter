using System;
using System.Globalization;
using System.Text;
using RJCP.IO.Ports;

namespace CosineKitty.Gravimetry
{
    public delegate void CommFailureDelegate();
    public delegate void LineReceivedDelegate(string line);
    public delegate void TimingDelegate(DateTime arrival, uint gps_clock, uint min_us_elapsed, uint max_us_elapsed);

    public class Gravimeter : IDisposable
    {
        private SerialPortStream port;
        private StringBuilder sb = new StringBuilder();

        public event LineReceivedDelegate LineReceivedEvent;
        public event CommFailureDelegate CommFailureEvent;
        public event TimingDelegate TimingEvent;

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
                    DateTime arrival = DateTime.Now;    // get the most accurate arrival time possible
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

                                    // Time stamp the message, parse it, and process it.
                                    ProcessMessage(arrival, payload);
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

        private void ProcessMessage(DateTime arrival, string payload)
        {
            string[] token;

            if (payload.StartsWith("r "))
            {
                // r 0001882766 0000000092 0000000108
                //   gps_clock  min_us     max_us
                token = payload.Substring(2).Trim().Split();
                if (token.Length == 3)
                {
                    if (uint.TryParse(token[0], out uint gps_clock))
                        if (uint.TryParse(token[1], out uint min_us_elapsed))
                            if (uint.TryParse(token[2], out uint max_us_elapsed))
                                if (TimingEvent != null)
                                    TimingEvent(arrival, gps_clock, min_us_elapsed, max_us_elapsed);
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

        public void RequestTiming()
        {
            Write("r");
        }
    }
}
