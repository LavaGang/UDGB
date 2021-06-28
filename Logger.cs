using System;
using System.IO;

namespace UDGB
{
    internal class Logger
    {
#if DEBUG
        private static bool DEBUG = true;
#else
        private static bool DEBUG = false;
#endif
        private static bool ShouldLogToFile = true;
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

        internal static void Warning(string str)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(str);
            Console.ForegroundColor = oldColor;
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

        internal static void DebugMsg(string str)
        {
            if (!DEBUG)
                return;
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            str = $"[DEBUG] {str}";
            Console.WriteLine(str);
            Console.ForegroundColor = oldColor;
            if (!ShouldLogToFile || (sr == null))
                return;
            sr.WriteLine(str);
            sr.Flush();
        }
    }
}