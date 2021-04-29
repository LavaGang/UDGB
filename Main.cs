using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace UDGB
{
    public static class Program
    {
        internal static WebClient webClient = new WebClient();
        private static string cache_path = null;
        private static string temp_folder_path = null;
        private static int cooldown_interval = 5; // In Seconds

        internal enum OperationModes
        {
            Normal,
            Android_Il2Cpp,
            Android_Mono
        }
        private static OperationModes OperationMode = OperationModes.Normal;

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

            string requested_version = args[0];

            OperationMode = (requested_version.EndsWith(";android") 
                || requested_version.EndsWith(";android_il2cpp"))
                ? OperationModes.Android_Il2Cpp
                : (requested_version.EndsWith(";android_mono") 
                ? OperationModes.Android_Mono 
                : OperationModes.Normal);

            if (OperationMode != OperationModes.Normal)
                requested_version = requested_version.Substring(0, requested_version.LastIndexOf(";"));

            UnityVersion.Refresh();
            if (UnityVersion.VersionTbl.Count <= 0)
            {
                Logger.Error("Failed to Get Unity Versions List from " + UnityVersion.UnityURL);
                return -1;
            }

            return (requested_version.StartsWith("--all") ? (ProcessAll() ? 0 : -1) : (ProcessSpecific(requested_version) ? 0 : -1));
        }

        private static UnityVersion GetUnityVersionFromString(string requested_version) =>
            UnityVersion.VersionTbl.FirstOrDefault(x => x.Version.Equals(requested_version));

        private static bool ProcessSpecific(string requested_version)
        {
            UnityVersion version = GetUnityVersionFromString(requested_version);
            if (version == null)
            {
                Logger.Error($"Failed to Find Unity Version [{requested_version}] in List!");
                return false;
            }
            return ProcessUnityVersion(version);
        }

        private static bool ProcessAll()
        {
            List<UnityVersion> sortedversiontbl = new List<UnityVersion>();
            foreach (UnityVersion version in UnityVersion.VersionTbl)
            {
                if ((version.Version.StartsWith("2020")
                    && !version.Version.StartsWith("2020.1"))
                    || version.Version.StartsWith("2021"))
                {
                    Logger.Warning(version.Version + " is Incompatible with Current Extraction Method! Skipping...");
                    continue;
                }

                if ((OperationMode == OperationModes.Android_Il2Cpp)
                    || (OperationMode == OperationModes.Android_Mono))
                {
                    if (version.Version.StartsWith("5.2")
                        || version.Version.StartsWith("5.1")
                        || version.Version.StartsWith("5.0")
                        || version.Version.StartsWith("4")
                        || version.Version.StartsWith("3"))
                    {
                        Logger.Warning(version.Version + " Has No Android Support Installer! Skipping...");
                        continue;
                    }
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
                    if (ProcessUnityVersion(version))
                        success_count += 1;
                    else
                    {
                        Logger.Warning("Failure Detected! Skipping to Next Version...");
                        error_count += 1;
                    }
                    Logger.Msg("Cooldown Active for " + cooldown_interval.ToString() + " seconds...");
                    Thread.Sleep(cooldown_interval * 1000);
                }
                if (error_count > 0)
                    Logger.Error(error_count.ToString() + " Failures");
                if (success_count > 0)
                    Logger.Msg(success_count.ToString() + " Successful Zip Creations");
            }
            return (error_count <= 0);
        }

        private static bool ProcessUnityVersion(UnityVersion version)
        {
            if (version.Version.StartsWith("2020") && !version.Version.StartsWith("2020.1"))
            {
                Logger.Error(version.Version + " is Incompatible with Extraction Method!");
                return false;
            }

            if ((OperationMode == OperationModes.Android_Il2Cpp)
                || (OperationMode == OperationModes.Android_Mono))
            {
                if (version.Version.StartsWith("5.2")
                    || version.Version.StartsWith("5.1")
                    || version.Version.StartsWith("5.0")
                    || version.Version.StartsWith("4")
                    || version.Version.StartsWith("3"))
                {
                    Logger.Error(version.Version + " Has No Android Support Installer!");
                    return false;
                }
            }

            string zip_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (version.Version + ".zip"));
            if (File.Exists(zip_path))
                File.Delete(zip_path);

            string downloadurl = version.DownloadURL;
            if ((OperationMode == OperationModes.Android_Il2Cpp)
                || (OperationMode == OperationModes.Android_Mono))
            {
                downloadurl = downloadurl.Substring(0, downloadurl.LastIndexOf("/"));
                downloadurl = $"{downloadurl.Substring(0, downloadurl.LastIndexOf("/"))}/TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-{version.FullVersion}.exe";
            }

            Logger.Msg("Downloading " + downloadurl);
            bool was_error = false;
            try
            {
                webClient.DownloadFile(downloadurl, cache_path);
                was_error = !ExtractFilesFromArchive(version);
                Thread.Sleep(1000);
                if (!was_error)
                    ArchiveHandler.CreateZip(temp_folder_path, zip_path);
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
                return false;
            Logger.Msg(version.Version + " Zip Successfully Created!");
            return true;
        }

        private static bool ExtractFilesFromArchive(UnityVersion version)
        {
            switch (OperationMode)
            {
                // Unity Dependencies for Unstripping Only
                case OperationModes.Normal:
                default:
                    string internal_path = "Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment_mono/Data/";
                    if (version.Version.StartsWith("3."))
                        internal_path = "Data/PlaybackEngines/windows64standaloneplayer/";
                    else if (version.Version.StartsWith("4."))
                    {
                        if (version.Version.StartsWith("4.5")
                            || version.Version.StartsWith("4.6")
                            || version.Version.StartsWith("4.7"))
                            internal_path = "Data/PlaybackEngines/windowsstandalonesupport/";
                        else
                            internal_path = "Data/PlaybackEngines/windows64standaloneplayer/";
                    }
                    else if (version.Version.StartsWith("5.3"))
                        internal_path = "Editor/Data/PlaybackEngines/WebPlayer/";

                    Logger.Msg("Extracting DLLs from Archive...");
                    return ArchiveHandler.ExtractFiles(temp_folder_path, cache_path, internal_path + "Managed/*.dll");


                // Full Android Libraries
                case OperationModes.Android_Il2Cpp:
                case OperationModes.Android_Mono:
                    string rootpath = "$INSTDIR$*";
                    string basefolder = $"{rootpath}/Variations/{((OperationMode == OperationModes.Android_Il2Cpp) ? "il2cpp" : "mono")}/";
                    string libfilename = "libunity.so";

                    Logger.Msg($"Extracting {libfilename} from Archive...");
                    if (!ArchiveHandler.ExtractFiles(temp_folder_path, cache_path, $"{basefolder}Release/Libs/*/{libfilename}", true))
                        return false;

                    Logger.Msg("Fixing Folder Structure...");
                    string libsfolderpath = Path.Combine(temp_folder_path, "Libs");
                    if (!Directory.Exists(libsfolderpath))
                        Directory.CreateDirectory(libsfolderpath);
                    foreach (string filepath in Directory.GetFiles(temp_folder_path, libfilename, SearchOption.AllDirectories))
                    {
                        Logger.Msg($"Moving {filepath}");
                        DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(filepath));

                        string newpath = Path.Combine(libsfolderpath, dir.Name);
                        if (!Directory.Exists(newpath))
                            Directory.CreateDirectory(newpath);
                        File.Move(filepath, Path.Combine(newpath, Path.GetFileName(filepath)));
                    }

                    string rootfolder = Directory.GetDirectories(temp_folder_path, rootpath).First();
                    Logger.Msg($"Removing {rootfolder}");
                    Directory.Delete(rootfolder, true);

                    Logger.Msg($"Extracting Managed Folder...");
                    string newmanagedfolder = Path.Combine(temp_folder_path, "Managed");
                    if (!Directory.Exists(newmanagedfolder))
                        Directory.CreateDirectory(newmanagedfolder);

                    return ArchiveHandler.ExtractFiles(newmanagedfolder, cache_path, basefolder + "Managed/*.dll");
            }
        }
    }
}