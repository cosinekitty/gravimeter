using System;

namespace CosineKitty.Gravimetry
{
    class Program
    {
        const string UsageText = @"
USAGE:

GravimeterCapture /dev/{serialport} terminal
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

        static int RawTerminalSession(string serialport)
        {
            return 0;
        }
    }
}
