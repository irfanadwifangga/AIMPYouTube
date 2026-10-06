using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIMPYouTube.Configuration
{
    public class PlaylistConfig
    {
        [JsonProperty("ID")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("ChannelName")]
        public string ChannelName { get; set; } = string.Empty;

        [JsonProperty("ReferenceName")]
        public string ReferenceName { get; set; } = string.Empty;

        [JsonProperty("AIMPPlaylistId")]
        public string AimpPlaylistId { get; set; } = string.Empty;

        [JsonProperty("CanModify")]
        public bool CanModify { get; set; }

        [JsonProperty("Items")]
        public HashSet<string> Items { get; set; } = new HashSet<string>();

        [JsonIgnore]
        public string Url
        {
            get => !string.IsNullOrEmpty(Id) ? $"https://www.youtube.com/playlist?list={Id}" : string.Empty;
            set { }
        }

        public PlaylistConfig() { }

        public PlaylistConfig(string id, string title, bool canModify, string refName = "", string channelName = "")
        {
            Id = id;
            Title = title;
            CanModify = canModify;
            ReferenceName = refName;
            ChannelName = channelName;
        }
    }
}
