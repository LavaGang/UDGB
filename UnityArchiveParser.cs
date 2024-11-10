using AssetRipper.Primitives;
using n1685.Utilities.Extensions;

namespace UDGB
{
    internal class UnityArchiveParser
    {
        private const string URL = "https://unity.com/releases/editor/archive";
        private const string URL_WHATS_NEW = "https://unity.com/releases/editor/whats-new/";
        private const string URL_DOWNLOAD = "https://download.unity3d.com/download_unity/";

        internal static UnityVersion[]? GetVersions()
        {
            string? pageSource = Program.webClient.TryGetString(URL);
            if (string.IsNullOrEmpty(pageSource))
                return null;

            string target = "unityhub://";
            HashSet<UnityVersion> returnVal = new();

            string[] pageLines = pageSource.Split('\n');
            foreach (string line in pageLines)
            {
                if (string.IsNullOrEmpty(line)
                    || string.IsNullOrEmpty(line)
                    || !line.Contains(target))
                    continue;

                int indexOf = line.IndexOf(target);
                string subLine = line.Substring(indexOf + target.Length);
                int end = pageSource.IndexOf("\\");
                string foundVersion = line.Substring(0, end);

                UnityVersion version = UnityVersion.Parse(foundVersion);
                if (returnVal.Contains(version))
                    continue;

                returnVal.Add(version);
            }

            return returnVal.ToArray();
        }

        internal enum ePlatform
        {
            ALL,
            WINDOWS,
            MACOS,
            LINUX
        }
        internal static string[]? GetDownloads(UnityVersion version,
            ePlatform platform = ePlatform.ALL)
        {
            string? pageSource = Program.webClient.TryGetString($"{URL_WHATS_NEW}{version}#installs");
            if (string.IsNullOrEmpty(pageSource))
                pageSource = Program.webClient.TryGetString($"{URL_WHATS_NEW}{version.ToStringWithoutType()}#installs");

            if (string.IsNullOrEmpty(pageSource))
                return null;

            HashSet<string> returnVal = new();
            string target = $"\"{URL_DOWNLOAD}";
            int next;
            while ((next = pageSource.IndexOf(target)) != -1)
            {
                pageSource = pageSource.Substring(next + target.Length);
                int end = pageSource.IndexOf("\"");

                if (end == -1)
                    continue;

                string url = $"{URL_DOWNLOAD}{pageSource.Substring(0, end)}";
                if (url.EndsWith("\\"))
                    url = url.Substring(0, url.Length - 1);

                if (platform != ePlatform.ALL)
                {
                    if (platform == ePlatform.WINDOWS
                        && !url.EndsWith(".exe"))
                        continue;

                    if (platform == ePlatform.MACOS
                        && !url.EndsWith(".pkg"))
                        continue;

                    if (platform == ePlatform.LINUX
                        && !url.EndsWith(".tar.gz"))
                        continue;
                }

                returnVal.Add(url);
            }

            return returnVal.ToArray();
        }
    }
}
