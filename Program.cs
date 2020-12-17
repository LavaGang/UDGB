using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace UDGB
{
    public static class Program
    {
        internal static WebClient webClient = new WebClient();
        internal static int refresh_interval = 60; // In Seconds

        public static int Main(string[] args)
        {
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = ServicePointManager.DefaultPersistentConnectionLimit;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            webClient.Headers.Add("User-Agent", "Unity web player");
            if ((args.Length < 1) || (args.Length > 1))
            {
                Logger.LogError("Bad arguments for extractor process; expected arguments: <unityVersion>");
                return -1;
            }
            string requested_version = args[0];
            UnityVersion.Refresh();
            UnityVersion version = UnityVersion.VersionTbl.Find(x => x.Version.Equals(requested_version));
            if (version == null)
            {
                Logger.LogError("Failed to find requested Unity Version: " + requested_version);
                return -1;
            }
            string installer_filepath = TempFileCache.CreateFile();
            string zip_path = requested_version + ".zip";
            Logger.Log("Downloading " + version.DownloadURL);
            try
            {
                webClient.DownloadFile(version.DownloadURL, installer_filepath);
                Logger.Log("Extracting...");
                string folder_path = "Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment_mono/Data/Managed/*.dll";
                ProcessStartInfo p = new ProcessStartInfo();
                p.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z/7z.exe");
                p.Arguments = "e \"" + installer_filepath + "\" -otmp \"" + folder_path + "\"";
                Process x = Process.Start(p);
                x.WaitForExit();
                Logger.Log("Creating Zip...");
                if (File.Exists(zip_path))
                    File.Delete(zip_path);
                ZipFile.CreateFromDirectory("tmp", zip_path);
            }
            catch (Exception x)
            {
                Logger.LogError(x.ToString());
                return 1;
            }
            Logger.Log("Cleaning up...");
            Directory.Delete("tmp", true);
            TempFileCache.ClearCache();
            Logger.Log("Press any key to exit!");
            Console.ReadKey();
            return 0;
        }
    }
}