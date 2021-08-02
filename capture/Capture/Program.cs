using System;
using System.Threading;

namespace CosineKitty.Gravimetry
{
    class Program
    {
        const string UsageText = @"
USAGE:

Capture /dev/{serialport} terminal
    Opens a raw terminal session where the user can send
    commands and receive responses from the gravimeter.

Capture /dev/{serialport} timing interval_seconds repeat_count
";

        static int Main(string[] args)
        {
            if (args.Length == 2 && args[1] == "terminal")
                return RawTerminalSession(args[0]);

            if (args.Length == 4 && args[1] == "timing"
                && int.TryParse(args[2], out int interval_seconds)
                && int.TryParse(args[3], out int repeat_count))
                return TimingTest(args[0], interval_seconds, repeat_count);

            Console.WriteLine("{0}", UsageText);
            return 1;
        }

        static int RawTerminalSession(string serialPortPath)
        {
            using (var gravimeter = new Gravimeter(serialPortPath))
            {
                gravimeter.LineReceivedEvent += OnRawTerminalLineReceived;
                gravimeter.CommFailureEvent += OnRawTerminalCommFailure;
                Console.WriteLine("READY");
                while (true)
                {
                    string line = Console.ReadLine();
                    if (line == null)
                        break;
                    gravimeter.Write(line);
                }
            }
            return 0;
        }

        static void OnRawTerminalLineReceived(string line)
        {
            Console.WriteLine("{0}", line);
        }

        static void OnRawTerminalCommFailure()
        {
            Console.WriteLine("!!! COMM FAILURE DETECTED !!!");
        }

        static int TimingTest(string serialPortPath, int interval_seconds, int repeat_count)
        {
            using (var gravimeter = new Gravimeter(serialPortPath))
            {
                bool failure = false;
                gravimeter.TimingEvent += delegate(DateTime arrival, uint gps_clock, uint min_us_elapsed, uint max_us_elapsed)
                {
                    Console.WriteLine("{0} {1,10} {2,10} {3,10}", arrival.ToString("o"), gps_clock, min_us_elapsed, max_us_elapsed);
                };

                gravimeter.CommFailureEvent += delegate()
                {
                    failure = true;
                    Console.WriteLine("!!! COMM FAILURE DETECTED !!!");
                };

                Thread.Sleep(1000);
                for (int i = 0; i < repeat_count && !failure; ++i)
                {
                    gravimeter.RequestTiming();
                    Thread.Sleep(interval_seconds * 1000);
                }
            }
            return 0;
        }
    }
}
