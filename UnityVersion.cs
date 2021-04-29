using System.Collections.Generic;

namespace UDGB
{
    internal class UnityVersion
    {
        internal static List<UnityVersion> VersionTbl = new List<UnityVersion>();
        internal static string UnityURL = "https://unity3d.com/get-unity/download/archive";
        internal string Version = null;
        internal string FullVersion = null;
        internal string DownloadURL = null;
        private static string QuoteStr = "\"";

        internal UnityVersion(string version, string fullversion, string downloadurl) { Version = version; FullVersion = fullversion; DownloadURL = downloadurl; }

        internal static void Refresh(bool so_mode)
        {
            if (VersionTbl.Count > 0)
                VersionTbl.Clear();
            
            string pageSource = Program.webClient.DownloadString(UnityURL);
            if (string.IsNullOrEmpty(pageSource))
                return;

            string[] pageSource_Lines = pageSource.Split(new[] { '\r', '\n' });
            if (pageSource_Lines.Length <= 0)
                return;
            
            foreach (string sourceline in pageSource_Lines)
            {
                if (string.IsNullOrEmpty(sourceline) || sourceline.Contains("Samsung"))
                    continue;

                string href_identifier = $"<a href={QuoteStr}https://download.unity3d.com/";
                if (!sourceline.Contains(href_identifier))
                    continue;

                href_identifier = $"<a href={QuoteStr}";
                int href_identifier_index = sourceline.IndexOf(href_identifier);
                if (href_identifier_index <= 0)
                    continue;

                string setup_identifier = "UnitySetup64-";
                if (!sourceline.Contains(setup_identifier))
                {
                    setup_identifier = "UnitySetup-";
                    if (!sourceline.Contains(setup_identifier))
                        continue;
                }

                string subsourceline = sourceline.Substring(href_identifier_index + href_identifier.Length);
                if (string.IsNullOrEmpty(subsourceline))
                    continue;

                int quote_index = subsourceline.IndexOf(QuoteStr);
                if (quote_index <= 0)
                    continue;

                string found_url = subsourceline.Substring(0, quote_index);
                if (string.IsNullOrEmpty(found_url))
                    continue;

                int setup_identifier_index = found_url.LastIndexOf(setup_identifier);
                if (setup_identifier_index <= 0)
                    continue;

                string subsourceline2 = found_url.Substring(setup_identifier_index + setup_identifier.Length);
                if (string.IsNullOrEmpty(subsourceline2))
                    continue;

                string extension_identifier = ".exe";
                if (!subsourceline2.Contains(extension_identifier))
                    continue;

                int extension_identifier_index = subsourceline2.IndexOf(extension_identifier);
                if (extension_identifier_index <= 0)
                    continue;

                string found_version = subsourceline2.Substring(0, extension_identifier_index);
                if (string.IsNullOrEmpty(found_version))
                    continue;

                string fullversion = found_version;
                if (found_version.Contains("f"))
                    found_version = found_version.Substring(0, found_version.IndexOf("f"));

                VersionTbl.Add(new UnityVersion(found_version, fullversion, found_url));
            }
            
            VersionTbl.Reverse();
        }
    }
}
