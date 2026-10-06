using System;
using Newtonsoft.Json;

namespace AIMPYouTube.Configuration
{
    public class TrackInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("N")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("P")]
        public string Permalink { get; set; } = string.Empty;

        [JsonProperty("A")]
        public string Artwork { get; set; } = string.Empty;

        [JsonProperty("D")]
        public double Duration { get; set; }

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public string Author { get; set; } = string.Empty;

        [JsonIgnore]
        public string StreamUrl { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTime StreamUrlExpires { get; set; } = DateTime.MinValue;

        public TrackInfo() { }

        public TrackInfo(string title, string id, string permalink, string artwork, double duration, string author = "")
        {
            Title = title;
            Id = id;
            Permalink = permalink;
            Artwork = artwork;
            Duration = duration;
            Author = author;
        }
    }
}
