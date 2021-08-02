using System;

namespace CosineKitty.Gravimetry
{
    class Program
    {
        const string UsageText = @"
USAGE:

Capture /dev/{serialport} terminal
    Opens a raw terminal session where the user can send
    commands and receive responses from the gravimeter.
";

        static int Main(string[] args)
        {
            if (args.Length == 2 && args[1] == "terminal")
                return RawTerminalSession(args[0]);

            Console.WriteLine("{0}", UsageText);
            return 1;
        }

        static int RawTerminalSession(string serialPortPath)
        {
            using (var gravimeter = new Gravimeter(serialPortPath))
            {
                gravimeter.LineReceivedEvent += OnRawTerminalLineReceived;
                string line;
                while (null != (line = Console.ReadLine()))
                {
                    gravimeter.Write(line);
                }
            }
            return 0;
        }

        static void OnRawTerminalLineReceived(string line)
        {
            Console.WriteLine("{0}", line);
        }
    }
}
