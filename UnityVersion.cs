using System;
using System.Collections.Generic;

using _UnityVersion = AssetRipper.VersionUtilities.UnityVersion;

namespace UDGB
{
    internal class UnityVersion
    {
        internal static List<UnityVersion> VersionTbl = new List<UnityVersion>();
        internal static string UnityURL = "https://unity3d.com/get-unity/download/archive";
        internal _UnityVersion Version = _UnityVersion.MinVersion;
        // internal string VersionStr = null;
        // internal string FullVersionStr = null;
        internal string DownloadURL = null;
        internal string HashStr = null;
        internal bool UsePayloadExtraction = false;
        private static string QuoteStr = "\"";

        [Obsolete("VersionStr is deprecated, please use Version.ToStringWithoutType() instead.")]

        internal string VersionStr => Version.ToStringWithoutType();
        [Obsolete("FullVersionStr is deprecated, please use Version.ToString() instead.")]
        internal string FullVersionStr => Version.ToString();

        internal UnityVersion(string fullversion, string downloadurl)
        {
            Version = _UnityVersion.Parse(fullversion);
            // VersionStr = version;
            // FullVersionStr = fullversion;
            // DownloadURL = downloadurl;
            //
            // string[] versiontbl = version.Split('.');
            // for (int i = 0; i < versiontbl.Length; i++)
            // {
            //     int output = 0;
            //     if (!int.TryParse(versiontbl[i], out output))
            //         continue;
            //     Version[i] = output;
            // }
            // Version.ToString()


            string[] downloadurl_splices = downloadurl.Split('/');
            if (Version < _UnityVersion.Parse("5.3.99") || downloadurl_splices[4].EndsWith(".exe"))
            {
                Logger.DebugMsg($"{Version.ToStringWithoutType()} - {DownloadURL}");
                return;
            }

            UsePayloadExtraction = true;
            HashStr = downloadurl_splices[4];
            DownloadURL = $"https://download.unity3d.com/download_unity/{HashStr}/MacEditorTargetInstaller/UnitySetup-Windows-";
            if (Version >= _UnityVersion.Parse("2018.0.0"))
                DownloadURL += "Mono-";
            DownloadURL += $"Support-for-Editor-{Version.ToString()}.pkg";

            Logger.DebugMsg($"{Version.ToStringWithoutType()} - {HashStr} - {DownloadURL}");
        }

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

                VersionTbl.Add(new UnityVersion(fullversion, found_url));
            }
            
            VersionTbl.Reverse();
        }
    }
}
