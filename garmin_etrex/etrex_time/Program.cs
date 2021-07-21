using System;
using RJCP.IO.Ports;

namespace etrex_time
{
    class Program
    {
        static int Main(string[] args)
        {
            using (var port = new SerialPortStream("/dev/ttyUSB0", 9600, 8, Parity.None, StopBits.One))
            {
                port.Open();
                string line;
                while (null != (line = port.ReadLine()))
                {
                    line = line.Trim();
                    Console.WriteLine(line);
                    // @210721210215N2842048W08107262G007+00013E0000N0000D0001
                    // @yymmddhhmmssNlat----Wlon-----G007+00013E0000N0000D0001
                }
            }
            return 0;
        }
    }
}
