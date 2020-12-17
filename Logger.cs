using System;
using System.IO;

namespace UDGB
{
    public class Logger
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
        public static void Log(string str)
        {
            Console.WriteLine(str);
            if (!ShouldLogToFile || (sr == null))
                return;
            sr.WriteLine(str);
            sr.Flush();
        }
        
        public static void LogError(string str)
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

        public static void Spacer()
        {
            Console.Write("\n");
            if (!ShouldLogToFile || (sr == null))
                return;
            sr.Write("\n");
            sr.Flush();
        }

        public static void Flush() { if (ShouldLogToFile && (sr != null)) sr.Flush(); }
    }
}