using System.Collections.Generic;

namespace UDGB
{
    internal class UnityVersion
    {
        internal static List<UnityVersion> VersionTbl = new List<UnityVersion>();
        internal static string UnityURL = "https://unity3d.com/get-unity/download/archive";
        internal int[] Version = { 0, 0, 0, 0 };
        internal string VersionStr = null;
        internal string FullVersionStr = null;
        internal string DownloadURL = null;
        internal string HashStr = null;
        internal bool UsePayloadExtraction = false;

        internal UnityVersion(string version, string fullversion, string downloadurl)
        {
            VersionStr = version;
            FullVersionStr = fullversion;
            DownloadURL = downloadurl;

            string[] versiontbl = version.Split('.');
            for (int i = 0; i < versiontbl.Length; i++)
            {
                int output = 0;
                if (!int.TryParse(versiontbl[i], out output))
                    continue;
                Version[i] = output;
            }


            string[] downloadurl_splices = downloadurl.Split('/');
            if ((Version[0] < 5)
                || ((Version[0] == 5) && (Version[1] < 3))
                || downloadurl_splices[4].EndsWith(".exe"))
            {
                Logger.DebugMsg($"{VersionStr} - {DownloadURL}");
                return;
            }

            UsePayloadExtraction = true;
            HashStr = downloadurl_splices[4];
            DownloadURL = $"https://download.unity3d.com/download_unity/{HashStr}/MacEditorTargetInstaller/UnitySetup-Windows-";
            if (Version[0] >= 2018)
                DownloadURL += "Mono-";
            DownloadURL += $"Support-for-Editor-{FullVersionStr}.pkg";

            Logger.DebugMsg($"{VersionStr} - {HashStr} - {DownloadURL}");
        }

        internal static void Refresh()
        {
            if (VersionTbl.Count > 0)
                VersionTbl.Clear();

            string pageSource = Program.webClient.DownloadString(UnityURL);
            if (string.IsNullOrEmpty(pageSource))
                return;

            string target = "unityHubDeepLink\\\":\\\"unityhub://";

            int next;
            while ((next = pageSource.IndexOf(target)) != -1)
            {
                pageSource = pageSource.Substring(next + target.Length);
                int end = pageSource.IndexOf("\\\"");

                if (end == -1)
                    continue;

                string url = pageSource.Substring(0, end);

                string[] parts = url.Split('/');
                string foundVersion = parts[0];
                string hash = parts[1];

                string fullVersion = foundVersion;
                if (foundVersion.Contains("f"))
                    foundVersion = foundVersion.Substring(0, foundVersion.IndexOf("f"));

                string foundUrl = $"https://download.unity3d.com/download_unity/{hash}/Windows64EditorInstaller/UnitySetup64-{fullVersion}.exe";

                VersionTbl.Add(new UnityVersion(foundVersion, fullVersion, foundUrl));
            }

            VersionTbl.Reverse();
        }
    }
}
