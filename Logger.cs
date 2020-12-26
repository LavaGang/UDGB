using System;
using System.IO;

namespace UDGB
{
    internal class Logger
    {
        private static bool ShouldLogToFile = false;
        private static FileStream fs = null;
        private static StreamWriter sr = null;

        static Logger()
        {
            if (!ShouldLogToFile)
                return;
            if (File.Exists("output.log"))
                File.Delete("output.log");
            fs = File.OpenWrite("output.log");
            sr = new StreamWriter(fs);
        }

        internal static void Msg(string str)
        {
            Console.WriteLine(str);
            if (!ShouldLogToFile || (sr == null))
                return;
            sr.WriteLine(str);
            sr.Flush();
        }

        internal static void Error(string str)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ForegroundColor = oldColor;
            if (!ShouldLogToFile || (sr == null))
                return;
            sr.WriteLine(str);
            sr.Flush();
        }
    }
}