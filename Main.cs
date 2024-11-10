using AssetRipper.Primitives;
using System.Net;

namespace UDGB
{
    public static class Program
    {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
        internal static WebClient webClient = new WebClient();
#pragma warning restore SYSLIB0014 // Type or member is obsolete

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
            webClient.Headers.Add("User-Agent", "Unity web player");

            temp_folder_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            if (Directory.Exists(temp_folder_path))
                Directory.Delete(temp_folder_path, true);
            cache_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache.tmp");
            if (File.Exists(cache_path))
                File.Delete(cache_path);

            if ((args.Length < 1) || (args.Length > 2) || string.IsNullOrEmpty(args[0]))
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

            if ((args.Length == 2) && !string.IsNullOrEmpty(args[1]))
            {
                UnityVersion? version = GetUnityVersionFromString(requested_version);
                if (version == null)
                {
                    Logger.Error($"Failed to Find Unity Version [{requested_version}] in List!");
                    return -1;
                }
                cache_path = args[1];
                try { return ExtractFilesFromArchive(cache_path, version.Value) ? 0 : -1; }
                catch (Exception x) { Logger.Error(x.ToString()); return -1; }
            }

            return (ProcessSpecific(requested_version) ? 0 : -1);
        }

        private static UnityVersion? GetUnityVersionFromString(string requested_version)
            => UnityVersion.TryParse(requested_version, out UnityVersion returnVal, out _) ? returnVal : null;

        private static bool ProcessSpecific(string requested_version)
        {
            UnityVersion? version = GetUnityVersionFromString(requested_version);
            if (version == null)
            {
                Logger.Error($"Failed to Find Unity Version [{requested_version}] in List!");
                return false;
            }
            return ProcessUnityVersion(version.Value);
        }

        private static bool VersionFilter(UnityVersion version, bool should_error = true)
        {
            if ((OperationMode == OperationModes.Android_Il2Cpp)
                || (OperationMode == OperationModes.Android_Mono))
            {
                if (((version.Major == 5) && (version.Minor <= 2))
                    || (version.Major < 5))
                {
                    if (should_error)
                        Logger.Error($"{version.ToStringWithoutType()} Has No Android Support Installer!");
                    else
                        Logger.Warning($"{version.ToStringWithoutType()} Has No Android Support Installer!");
                    return false;
                }
            }

            return true;
        }

        private static bool ProcessAll()
        {
            /*
            List<UnityVersion> sortedversiontbl = new List<UnityVersion>();
            foreach (UnityVersion version in versions)
            {
                if (!VersionFilter(version, false))
                    continue;

                string zip_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (version.ToStringWithoutType() + ".zip"));
                if (File.Exists(zip_path))
                {
                    Logger.Warning($"{version.ToStringWithoutType()} Zip Already Exists! Skipping...");
                    continue;
                }
                Logger.Msg($"{version.ToStringWithoutType()} Zip Doesn't Exist! Adding to Download List...");
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
                    Logger.Msg($"Cooldown Active for {cooldown_interval} seconds...");
                    Thread.Sleep(cooldown_interval * 1000);
                }
                if (error_count > 0)
                    Logger.Error($"{error_count} Failures");
                if (success_count > 0)
                    Logger.Msg($"{success_count} Successful Zip Creations");
            }
            return (error_count <= 0);
            */
            return false;
        }

        private static bool ProcessUnityVersion(UnityVersion version)
        {
            if (!VersionFilter(version))
                return false;

            string zip_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{version.ToStringWithoutType()}.zip");
            if (File.Exists(zip_path))
                File.Delete(zip_path);

            string[]? downloadurls = UnityArchiveParser.GetDownloads(version, UnityArchiveParser.ePlatform.WINDOWS) ?? null;
            if (downloadurls == null)
            {
                Logger.Error($"Failed to get Downloads from Unity Version {version.ToStringWithoutType()}");
                return false;
            }
            string downloadUrl = downloadurls[0];
            if ((OperationMode == OperationModes.Android_Il2Cpp)
                || (OperationMode == OperationModes.Android_Mono))
                downloadUrl = downloadurls.First((x) => x.Contains("UnitySetup-Android-Support-for-Editor"));

            Logger.Msg($"Downloading {downloadUrl}");
            bool was_error = false;
            try
            {
                webClient.DownloadFile(downloadUrl, cache_path);
                was_error = !ExtractFilesFromArchive(downloadUrl, version);
                Thread.Sleep(1000);
                if (!was_error)
                    ArchiveHandler.CreateZip(temp_folder_path, zip_path);
            }
            catch (Exception x)
            {
                Logger.Error(x.ToString());
                was_error = true;
            }

#if !DEBUG
            Logger.Msg("Cleaning up...");
            if (Directory.Exists(temp_folder_path))
                Directory.Delete(temp_folder_path, true);
            if (File.Exists(cache_path))
                File.Delete(cache_path);

            string payload_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Payload~");
            if (File.Exists(payload_path))
                File.Delete(payload_path);
#endif

            if (was_error)
                return false;
            Logger.Msg($"{version.ToStringWithoutType()} Zip Successfully Created!");
            return true;
        }

        private static bool ExtractFilesFromArchive(string downloadUrl, UnityVersion version)
        {
            string internal_path = null;
            string archive_path = cache_path;
            switch (OperationMode)
            {
                // Unity Dependencies for Unstripping Only
                case OperationModes.Normal:
                default:
                    if (version.Major == 3)
                        internal_path = "Data/PlaybackEngines/windows64standaloneplayer/";
                    else if (version.Major == 4)
                    {
                        if (version.Minor >= 5)
                            internal_path = "Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment/Data/";
                        else
                            internal_path = "Data/PlaybackEngines/windows64standaloneplayer/";
                    }
                    else if ((version.Major == 5) && (version.Minor <= 2))
                        internal_path = "./Unity/Unity.app/Contents/PlaybackEngines/WindowsStandaloneSupport/Variations/win64_nondevelopment_mono/Data/";
                    else if (version.Major > 2021 || (version.Major == 2021 && version.Minor >= 2))
                        internal_path = "./Variations/win64_player_nondevelopment_mono/Data/";
                    else
                        internal_path = "./Variations/win64_nondevelopment_mono/Data/";

                    if ((version.Major < 5)
                        || ((version.Major == 5) && (version.Minor < 3))
                        || downloadUrl.EndsWith(".exe"))
                    {
                        Logger.Msg($"Extracting Payload...");
                        if (!ArchiveHandler.ExtractFiles(AppDomain.CurrentDomain.BaseDirectory, archive_path, "Payload~"))
                            return false;
                        archive_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Payload~");
                    }

                    Logger.Msg("Extracting DLLs from Archive...");
                    return ArchiveHandler.ExtractFiles(temp_folder_path, archive_path, internal_path + "Managed/*.dll");


                // Full Android Libraries
                case OperationModes.Android_Il2Cpp:
                case OperationModes.Android_Mono:
                    string rootpath = "$INSTDIR$*";
                    string basefolder = $"{rootpath}/Variations/{((OperationMode == OperationModes.Android_Il2Cpp) ? "il2cpp" : "mono")}/";
                    string libfilename = "libunity.so";

                    Logger.Msg($"Extracting {libfilename} from Archive...");
                    if (!ArchiveHandler.ExtractFiles(temp_folder_path, archive_path, $"{basefolder}Release/Libs/*/{libfilename}", true))
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

                    return ArchiveHandler.ExtractFiles(newmanagedfolder, archive_path, basefolder + "Managed/*.dll");
            }
        }
    }
}