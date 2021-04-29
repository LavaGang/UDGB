using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace UDGB
{
    internal static class ArchiveHandler
    {
        internal static bool ExtractFiles(string output_path, string zip_path, string internal_path, bool keep_file_path = false)
        {
            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z/7z.exe");
            p.Arguments = $"{(keep_file_path ? "x" : "e")} \"{zip_path}\" -o\"{output_path}\" \"{internal_path}\"";
            Process x = Process.Start(p);
            x.WaitForExit();
            if (x.ExitCode != 0)
                return false;
            return true;
        }

        internal static void CreateZip(string input_folder, string output_file)
        {
            if (File.Exists(output_file))
                File.Delete(output_file);
            ZipFile.CreateFromDirectory(input_folder, output_file);
        }
    }
}
