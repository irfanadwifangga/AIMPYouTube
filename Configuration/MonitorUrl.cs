using Newtonsoft.Json;

namespace AIMPYouTube.Configuration
{
    public class MonitorUrl
    {
        [JsonProperty("URL")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("PlaylistID")]
        public string PlaylistId { get; set; } = string.Empty;

        [JsonProperty("Flags")]
        public int Flags { get; set; }

        [JsonProperty("GroupName")]
        public string GroupName { get; set; } = string.Empty;

        public MonitorUrl() { }

        public MonitorUrl(string url, string playlistId, int flags, string groupName = "")
        {
            Url = url;
            PlaylistId = playlistId;
            Flags = flags;
            GroupName = groupName;
        }
    }
}
