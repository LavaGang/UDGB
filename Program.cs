using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;

namespace UDGB
{
    public static class Program
    {
        internal static WebClient webClient = new WebClient();
        private static string cache_path = null;
        private static string temp_folder_path = null;
        private static bool allmode_break_on_error = false;
        private static int allmode_refresh_interval = 5; // In Seconds
        private static bool allmode_single_variation = false;
        private static string allmode_variation = "2018.3";

        public static int Main(string[] args)
        {
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = ServicePointManager.DefaultPersistentConnectionLimit;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            webClient.Headers.Add("User-Agent", "Unity web player");

            temp_folder_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            if (Directory.Exists(temp_folder_path))
                Directory.Delete(temp_folder_path, true);
            cache_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache.tmp");
            if (File.Exists(cache_path))
                File.Delete(cache_path);

            if ((args.Length < 1) || (args.Length > 1) || string.IsNullOrEmpty(args[0]))
            {
                Logger.Error("Bad arguments for extractor process; expected arguments: <unityVersion>");
                return -1;
            }

            UnityVersion.Refresh();
            if (UnityVersion.VersionTbl.Count <= 0)
            {
                Logger.Error("Failed to Get Unity Versions List from " + UnityVersion.UnityURL);
                return -1;
            }

            string requested_version = args[0];
            if (requested_version.Equals("--all"))
                return All();
            return Normal(requested_version);
        }

        private static int Normal(string requested_version)
        {
            UnityVersion version = UnityVersion.VersionTbl.Find(x => x.Version.Equals(requested_version));
            if (version == null)
            {
                Logger.Error("Failed to find requested Unity Version: " + requested_version);
                return -1;
            }

            if (version.Version.StartsWith("2020") && !version.Version.StartsWith("2020.1"))
            {
                Logger.Error(version.Version + " is Incompatible with Extraction Method!");
                return -1;
            }

            string zip_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (version.Version + ".zip"));
            if (File.Exists(zip_path))
            {
                Logger.Error(version.Version + " Zip Already Exists!");
                return -1;
            }
            
            Logger.Msg("Downloading " + version.DownloadURL);
            bool was_error = false;
            try
            {
                webClient.DownloadFile(version.DownloadURL, cache_path);
                if (!ExtractDependencies(version))
                    was_error = true;
                else
                    CreateZip(zip_path);
            }
            catch (Exception x)
            {
                Logger.Error(x.ToString());
                was_error = true;
            }

            Logger.Msg("Cleaning up...");
            if (Directory.Exists(temp_folder_path))
                Directory.Delete(temp_folder_path, true);
            if (File.Exists(cache_path))
                File.Delete(cache_path);

            if (was_error)
                return 1;
            Logger.Msg(requested_version + " Zip Successfully Created!");
            return 0;
        }

        private static int All()
        {
            List<UnityVersion> sortedversiontbl = new List<UnityVersion>();

            foreach (UnityVersion version in UnityVersion.VersionTbl)
            {
                if (allmode_single_variation && !version.Version.StartsWith(allmode_variation))
                {
                    Logger.Warning(version.Version + " is not a " + allmode_variation + " Variation! Skipping...");
                    continue;
                }

                if (version.Version.StartsWith("2020") && !version.Version.StartsWith("2020.1"))
                {
                    Logger.Warning(version.Version + " is Incompatible with Current Extraction Method! Skipping...");
                    continue;
                }

                string zip_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (version.Version + ".zip"));
                if (File.Exists(zip_path))
                {
                    Logger.Warning(version.Version + " Zip Already Exists! Skipping...");
                    continue;
                }

                Logger.Msg(version.Version + " Zip Doesn't Exist! Adding to Download List...");
                sortedversiontbl.Add(version);
            }

            int error_count = 0;
            if (sortedversiontbl.Count >= 1)
            {
                int success_count = 0;
                foreach (UnityVersion version in sortedversiontbl)
                {
                    bool was_error = false;
                    Logger.Msg("Downloading " + version.DownloadURL);
                    try
                    {
                        webClient.DownloadFile(version.DownloadURL, cache_path);
                        if (!ExtractDependencies(version))
                            was_error = true;
                        else
                        {
                            string zip_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (version.Version + ".zip"));
                            CreateZip(zip_path);
                        }
                    }
                    catch (Exception x)
                    {
                        Logger.Error(x.ToString());
                        was_error = true;
                    }

                    Logger.Msg("Cleaning up...");
                    if (Directory.Exists(temp_folder_path))
                        Directory.Delete(temp_folder_path, true);
                    if (File.Exists(cache_path))
                        File.Delete(cache_path);

                    if (!was_error)
                    {
                        success_count++;
                        Logger.Msg(version.Version + " Zip Successfully Created!");
                    }
                    else
                    {
                        error_count++;
                        if (allmode_break_on_error)
                            break;
                        Logger.Warning("Failure Detected! Skipping to Next Version...");
                    }

                    Logger.Msg("Cooldown Active for " + allmode_refresh_interval.ToString() + " seconds...");
                    Thread.Sleep(allmode_refresh_interval * 1000);
                }

                if (!allmode_break_on_error)
                {
                    if (error_count > 0)
                        Logger.Error(error_count.ToString() + " Failures");
                    if (success_count > 0)
                        Logger.Msg(success_count.ToString() + " Successful Zip Creations");
                }
            }
            else
                Logger.Msg("All Unity Dependencies Successfully Checked and Downloaded!");

            return ((error_count <= 0) ? -1 : 0);
        }

        private static bool ExtractDependencies(UnityVersion version)
        {
            Logger.Msg("Extracting Dependencies...");

            string folder_path = "Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment_mono/Data/Managed/";
            if (version.Version.StartsWith("3."))
                folder_path = "Data/PlaybackEngines/windows64standaloneplayer/Managed/";
            else if (version.Version.StartsWith("4."))
            {
                //folder_path = "Data/PlaybackEngines/windowsstandalonesupport/Managed/";
                folder_path = "Data/PlaybackEngines/windows64standaloneplayer/Managed/";
            }

            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z/7z.exe");
            p.Arguments = "e \"" + cache_path + "\" -o\"" + temp_folder_path + "\" \"" + folder_path + "*.dll\"";

            Process x = Process.Start(p);
            x.WaitForExit();
            if (x.ExitCode != 0)
            {
                Logger.Error("Failed to Extract Dependencies!");
                return false;
            }
            Thread.Sleep(1000);
            return true;
        }

        private static void CreateZip(string output_file)
        {
            Logger.Msg("Creating " + output_file);
            if (File.Exists(output_file))
                File.Delete(output_file);
            ZipFile.CreateFromDirectory(temp_folder_path, output_file);
            Thread.Sleep(1000);
        }
    }
}