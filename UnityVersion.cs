using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDGB
{
    public class UnityVersion
    {
        public static List<UnityVersion> VersionTbl = new List<UnityVersion>();
        public string Version = null;
        public string DownloadURL = null;

        public UnityVersion(string version, string downloadurl) { Version = version; DownloadURL = downloadurl; }

        public static void Refresh()
        {
            if (VersionTbl.Count > 0)
                VersionTbl.Clear();
            string unity_url = "https://unity3d.com/get-unity/download/archive";
            string pageSource = Program.webClient.DownloadString(unity_url);
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
                //Logger.Log(found_version + "  -  " + found_url);
                VersionTbl.Add(new UnityVersion(found_version, found_url));
                is_looking_for_whats_new = false;
            }
            //Logger.Spacer();
        }
    }
}
