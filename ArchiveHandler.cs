using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace UDGB
{
    internal static class ArchiveHandler
    {
        private static AutoResetEvent ResetEvent_Output = new AutoResetEvent(false);
        private static AutoResetEvent ResetEvent_Error = new AutoResetEvent(false);

        internal static void CreateZip(string input_folder, string output_file)
        {
            if (File.Exists(output_file))
                File.Delete(output_file);
            ZipFile.CreateFromDirectory(input_folder, output_file);
        }

        internal static bool ExtractFiles(string output_path, string archive_path, string internal_path, bool keep_file_path = false)
        {
            ResetEvent_Output.Reset();
            ResetEvent_Error.Reset();

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo("7zip")
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z/7z.exe"),
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Arguments = string.Join(" ", new string[] {
                        keep_file_path ? "x" : "e",
                        $"\"{archive_path}\"",
                        "-y",
                        $"-o\"{output_path}\"",
                        $"\"{internal_path}\"",
                    }.Where(s => !string.IsNullOrEmpty(s)).Select(it => Regex.Replace(it, @"(\\+)$", @"$1$1"))),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            Logger.DebugMsg("\"" + process.StartInfo.FileName + "\" " + process.StartInfo.Arguments);

            process.OutputDataReceived += OutputStream;
            process.ErrorDataReceived += ErrorStream;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            ResetEvent_Output.WaitOne();
            ResetEvent_Error.WaitOne();

            return (process.ExitCode == 0);
        }

        private static void OutputStream(object sender, DataReceivedEventArgs e) { if (e.Data == null) ResetEvent_Output.Set(); else Logger.Msg(e.Data); }
        private static void ErrorStream(object sender, DataReceivedEventArgs e) { if (e.Data == null) ResetEvent_Error.Set(); else Logger.Error(e.Data); }
    }
}
