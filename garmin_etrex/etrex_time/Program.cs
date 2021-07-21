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
                    // Grab current date and time according to the computer.
                    // On my system, this is kept in sync by ntp.
                    DateTime now = DateTime.UtcNow;

                    line = line.Trim();

                    // Parse date and time from the string.
                    DateTime gpsTime = ParseDateTime(line);

                    // Print the discrepancy.
                    TimeSpan dt = gpsTime - now;
                    Console.WriteLine("{0} {1}", line, dt);
                }
            }
            return 0;
        }

        static DateTime ParseDateTime(string line)
        {
            if (line.Length != 55 || line[0] != '@')
                throw new ArgumentException("Invalid Garmin eTrex string format");

            int year = 2000 + int.Parse(line.Substring(1, 2));
            int month = int.Parse(line.Substring(3, 2));
            int day = int.Parse(line.Substring(5, 2));
            int hour = int.Parse(line.Substring(7, 2));
            int minute = int.Parse(line.Substring(9, 2));
            int second = int.Parse(line.Substring(11, 2));
            var gpsTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            return gpsTime;
        }
    }
}
