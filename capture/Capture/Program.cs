using System;
using System.Collections.Generic;
using System.IO;
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
            var list = new List<TimingPoint>();
            using (var gravimeter = new Gravimeter(serialPortPath))
            {
                bool failure = false;

                gravimeter.CommFailureEvent += delegate()
                {
                    failure = true;
                    Console.WriteLine("!!! COMM FAILURE DETECTED !!!");
                };

                // Reset the time before we begin capturing data.
                Thread.Sleep(1000);
                gravimeter.Write("z");
                Console.WriteLine("Reset completed.");
                Thread.Sleep(1000);

                gravimeter.TimingEvent += delegate(DateTime arrival, uint gps_clock, uint min_us_elapsed, uint max_us_elapsed)
                {
                    Console.WriteLine("{0} {1,10} {2,10} {3,10}", arrival.ToString("o"), gps_clock, min_us_elapsed, max_us_elapsed);
                    list.Add(new TimingPoint
                    {
                        Arrival = arrival,
                        GpsClock = gps_clock,
                        MinElapsedMicros = min_us_elapsed,
                        MaxElapsedMicros = max_us_elapsed,
                    });

                    if (list.Count > 1)
                    {
                        double elapsedSeconds = (list[list.Count - 1].Arrival - list[0].Arrival).TotalSeconds;
                        uint gpsTicks = list[list.Count - 1].GpsClock - list[0].GpsClock;
                        Console.WriteLine("ticks/second = {0:F6}", gpsTicks / elapsedSeconds);
                    }
                };

                for (int i = 0; i < repeat_count && !failure; ++i)
                {
                    gravimeter.RequestTiming();
                    Thread.Sleep(interval_seconds * 1000);
                }
            }

            using (StreamWriter output = File.CreateText("capture.csv"))
            {
                output.WriteLine("\"utc\",\"seconds\",\"gps_clock\",\"min_elapsed_us\",\"max_elapsed_us\"");
                foreach (TimingPoint p in list)
                {
                    double seconds = (p.Arrival - list[0].Arrival).TotalSeconds;
                    output.WriteLine($"\"{p.Arrival:o}\",{seconds:F6},{p.GpsClock},{p.MinElapsedMicros},{p.MaxElapsedMicros}");
                }
            }

            return 0;
        }
    }

    internal class TimingPoint
    {
        public DateTime Arrival;
        public uint GpsClock;
        public uint MinElapsedMicros;
        public uint MaxElapsedMicros;
    }
}
