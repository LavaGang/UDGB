using System.Net;

namespace n1685.Utilities.Extensions
{
    internal static class WebClientExtensions
    {
        internal static string? TryGetString(this WebClient client, string url)
        {
            if (string.IsNullOrEmpty(url)
                || string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            try
            {
                return client.DownloadString(url);
            }
            catch
            {
                return null;
            }
        }
    }
}
