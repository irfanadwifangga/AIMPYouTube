using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using AIMPYouTube.Configuration;

namespace AIMPYouTube.Core
{
    public static class Tools
    {
        private static readonly Regex VideoIdRegex = new Regex(@"^[a-zA-Z0-9_-]{11}$", RegexOptions.Compiled);
        private static readonly Regex YouTubeUrlRegex = new Regex(@"(?:youtube\.com\/(?:watch\?.*v=|v\/|embed\/|shorts\/)|youtu\.be\/|youtube:\/\/)([a-zA-Z0-9_-]{11})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PlaylistUrlRegex = new Regex(@"[?&]list=([a-zA-Z0-9_-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ChannelUrlRegex = new Regex(@"youtube\.com\/(channel\/([a-zA-Z0-9_-]+)|c\/([a-zA-Z0-9_.-]+)|user\/([a-zA-Z0-9_.-]+)|@([a-zA-Z0-9_.-]+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string TrackIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            url = url.Trim();

            if (VideoIdRegex.IsMatch(url))
                return url;

            var match = YouTubeUrlRegex.Match(url);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            // Check if query string contains v=
            if (url.Contains("?"))
            {
                var uri = new Uri(url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : "https://" + url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                string v = query["v"];
                if (!string.IsNullOrEmpty(v) && VideoIdRegex.IsMatch(v))
                    return v;
            }

            return string.Empty;
        }

        public static string PlaylistIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            var match = PlaylistUrlRegex.Match(url);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        public static TrackInfo GetTrackInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (Config.TrackInfos.TryGetValue(id, out var info))
                return info;

            return null;
        }

        public static TrackInfo GetTrackInfoFromFileName(string fileName)
        {
            string id = TrackIdFromUrl(fileName);
            return GetTrackInfo(id);
        }

        public static double ParseIsoDuration(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
                return 0;

            try
            {
                TimeSpan ts = XmlConvert.ToTimeSpan(iso);
                return ts.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
