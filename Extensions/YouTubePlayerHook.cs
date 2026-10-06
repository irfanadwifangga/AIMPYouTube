using System;
using AIMP.SDK;
using AIMP.SDK.Player.Extensions;
using AIMPYouTube.Core;
using AIMPYouTube.Services;

namespace AIMPYouTube.Extensions
{
    public class YouTubePlayerHook : IAimpExtensionPlayerHook, IAimpExtension
    {
        public bool OnCheckURL(ref string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (url.IndexOf("youtube.com", StringComparison.OrdinalIgnoreCase) >= 0 ||
                url.IndexOf("youtube://", StringComparison.OrdinalIgnoreCase) >= 0 ||
                url.IndexOf("youtu.be", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string videoId = Tools.TrackIdFromUrl(url);
                if (!string.IsNullOrEmpty(videoId))
                {
                    string streamUrl = YtDlpService.GetStreamUrl(videoId);
                    if (!string.IsNullOrEmpty(streamUrl))
                    {
                        if (!streamUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) && !streamUrl.Contains("&file="))
                        {
                            if (streamUrl.Contains("audio%2Fwebm") || streamUrl.Contains("audio/webm") || streamUrl.Contains("itag=251") || streamUrl.Contains("itag=250") || streamUrl.Contains("itag=249"))
                            {
                                streamUrl += "&file=audio.opus";
                            }
                            else
                            {
                                streamUrl += "&file=audio.m4a";
                            }
                        }
                        url = streamUrl;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
