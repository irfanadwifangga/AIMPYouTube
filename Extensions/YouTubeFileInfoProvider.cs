using System;
using AIMP.SDK;
using AIMP.SDK.FileManager.Extensions;
using AIMP.SDK.FileManager.Objects;
using AIMP.SDK.Objects;
using AIMPYouTube.Configuration;
using AIMPYouTube.Core;

namespace AIMPYouTube.Extensions
{
    /// <summary>
    /// Provides file info (title, artist, duration) for youtube:// URLs.
    /// AIMP calls this when it needs metadata for playlist items with custom schemes.
    /// </summary>
    public class YouTubeFileInfoProvider : IAimpExtensionFileInfoProvider, IAimpExtension
    {
        private readonly IAimpPlayer _player;

        public YouTubeFileInfoProvider(IAimpPlayer player)
        {
            _player = player;
        }

        public AimpActionResult GetFileInfo(string fileUri, ref IAimpFileInfo fileInfo)
        {
            if (string.IsNullOrWhiteSpace(fileUri))
                return new AimpActionResult(ActionResultType.Fail);

            // Only handle youtube:// scheme URLs
            if (fileUri.IndexOf("youtube://", StringComparison.OrdinalIgnoreCase) < 0 &&
                fileUri.IndexOf("youtube.com", StringComparison.OrdinalIgnoreCase) < 0 &&
                fileUri.IndexOf("youtu.be", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return new AimpActionResult(ActionResultType.Fail);
            }

            string videoId = Tools.TrackIdFromUrl(fileUri);
            if (string.IsNullOrEmpty(videoId))
                return new AimpActionResult(ActionResultType.Fail);

            var trackInfo = Tools.GetTrackInfo(videoId);
            if (trackInfo == null)
                return new AimpActionResult(ActionResultType.Fail);

            try
            {
                if (fileInfo == null)
                {
                    var createResult = _player.Core.CreateObject(AimpObjectType.AimpFileInfo);
                    if (createResult.ResultType != ActionResultType.OK || !(createResult.Result is IAimpFileInfo created))
                        return new AimpActionResult(ActionResultType.Fail);
                    fileInfo = created;
                }

                fileInfo.FileName = fileUri;
                fileInfo.Title = trackInfo.Title ?? string.Empty;
                fileInfo.Artist = trackInfo.Author ?? string.Empty;

                if (trackInfo.Duration > 0)
                {
                    fileInfo.Duration = trackInfo.Duration;
                }

                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] FileInfoProvider resolved: {trackInfo.Title} ({videoId})");
                return new AimpActionResult(ActionResultType.OK);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] FileInfoProvider error: {ex}");
                return new AimpActionResult(ActionResultType.Fail);
            }
        }

        public AimpActionResult GetFileInfo(IAimpStream stream, ref IAimpFileInfo fileInfo)
        {
            // Not applicable for stream-based lookup
            return new AimpActionResult(ActionResultType.Fail);
        }
    }
}
