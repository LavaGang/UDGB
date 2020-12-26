using System.Collections.Generic;

namespace UDGB
{
    internal class UnityVersion
    {
        internal static List<UnityVersion> VersionTbl = new List<UnityVersion>();
        internal static string UnityURL = "https://unity3d.com/get-unity/download/archive";
        internal string Version = null;
        internal string DownloadURL = null;

        internal UnityVersion(string version, string downloadurl) { Version = version; DownloadURL = downloadurl; }

        internal static void Refresh()
        {
            if (VersionTbl.Count > 0)
                VersionTbl.Clear();
            string pageSource = Program.webClient.DownloadString(UnityURL);
            if (string.IsNullOrEmpty(pageSource))
                return;
            string[] pageSource_Lines = pageSource.Split(new[] { '\r', '\n' });
            if (pageSource_Lines.Length <= 0)
                return;
            bool is_looking_for_whats_new = true;
            foreach (string sourceline in pageSource_Lines)
            {
                if (string.IsNullOrEmpty(sourceline))
                    continue;
                if (is_looking_for_whats_new)
                {
                    string whats_new_identifier = "/unity/whats-new/";
                    if (sourceline.Contains(whats_new_identifier))
                        is_looking_for_whats_new = false;
                    continue;
                }
                string setup_identifier = "UnitySetup64";
                if (!sourceline.Contains(setup_identifier))
                    continue;
                string href_identifier = "<li><a href=\"";
                string subsourceline = sourceline.Substring(sourceline.IndexOf(href_identifier) + href_identifier.Length);
                if (string.IsNullOrEmpty(subsourceline))
                    continue;
                string found_url = subsourceline.Substring(0, subsourceline.IndexOf("\""));
                if (string.IsNullOrEmpty(found_url))
                    continue;
                string found_version = found_url.Substring(found_url.LastIndexOf("UnitySetup64-") + setup_identifier.Length + 1);
                found_version = found_version.Substring(0, found_version.LastIndexOf("f"));
                VersionTbl.Add(new UnityVersion(found_version, found_url));
                is_looking_for_whats_new = false;
            }
            VersionTbl.Reverse();
        }
    }
}
