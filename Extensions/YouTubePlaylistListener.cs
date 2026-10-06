using System;
using System.Linq;
using AIMP.SDK;
using AIMP.SDK.Playlist.Extensions;
using AIMP.SDK.Playlist.Objects;
using AIMPYouTube.Configuration;

namespace AIMPYouTube.Extensions
{
    public class YouTubePlaylistListener : IAimpExtensionPlaylistManagerListener, IAimpExtension
    {
        public AimpActionResult OnPlaylistActivated(IAimpPlaylist playlist)
        {
            return new AimpActionResult(ActionResultType.OK);
        }

        public AimpActionResult OnPlaylistAdded(IAimpPlaylist playlist)
        {
            return new AimpActionResult(ActionResultType.OK);
        }

        public AimpActionResult OnPlaylistRemoved(IAimpPlaylist playlist)
        {
            if (playlist != null && !string.IsNullOrEmpty(playlist.Id))
            {
                string id = playlist.Id;
                Config.MonitorUrls.RemoveAll(m => m.PlaylistId == id);
                Config.SaveExtendedConfig();
            }
            return new AimpActionResult(ActionResultType.OK);
        }
    }
}
