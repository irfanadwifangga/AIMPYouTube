using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AIMP.SDK;
using AIMP.SDK.FileManager.Objects;
using AIMP.SDK.Playlist.Objects;
using AIMPYouTube.Configuration;
using AIMPYouTube.Core;
using Newtonsoft.Json.Linq;

namespace AIMPYouTube.Services
{
    public static class YouTubeService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static async Task ApplyAuthHeadersAsync(HttpRequestMessage request)
        {
            if (Config.IsConnected && Config.UseAccount)
            {
                await OAuthService.RefreshTokenIfNeededAsync().ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Config.AccessToken);
            }
        }

        public static async Task<List<PlaylistConfig>> GetUserPlaylistsAsync()
        {
            var playlists = new List<PlaylistConfig>();
            if (!Config.IsConnected)
                return playlists;

            try
            {
                string pageToken = "";
                while (playlists.Count < 100)
                {
                    string url = $"https://www.googleapis.com/youtube/v3/playlists?part=snippet,contentDetails&mine=true&maxResults=50";
                    if (!string.IsNullOrEmpty(pageToken))
                    {
                        url += $"&pageToken={pageToken}";
                    }

                    using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        await ApplyAuthHeadersAsync(req).ConfigureAwait(false);
                        using (var res = await _httpClient.SendAsync(req).ConfigureAwait(false))
                        {
                            if (!res.IsSuccessStatusCode)
                                break;

                            string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var doc = JObject.Parse(json);
                            var items = doc["items"] as JArray;
                            if (items == null || items.Count == 0)
                                break;

                            foreach (var item in items)
                            {
                                string id = item["id"]?.ToString();
                                string title = item["snippet"]?["title"]?.ToString() ?? "Untitled Playlist";
                                if (!string.IsNullOrEmpty(id))
                                {
                                    playlists.Add(new PlaylistConfig { Id = id, Title = title, Url = $"https://www.youtube.com/playlist?list={id}" });
                                }
                            }

                            pageToken = doc["nextPageToken"]?.ToString();
                            if (string.IsNullOrEmpty(pageToken))
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] GetUserPlaylistsAsync error: {ex}");
            }

            return playlists;
        }

        public static async Task<TrackInfo> GetVideoInfoAsync(string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
                return null;

            if (Config.TrackInfos.TryGetValue(videoId, out var cached) && !string.IsNullOrEmpty(cached.Title))
                return cached;

            try
            {
                string apiKey = Config.GetApiKey();
                string url = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,contentDetails&id={videoId}&key={apiKey}";

                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    await ApplyAuthHeadersAsync(req).ConfigureAwait(false);
                    using (var res = await _httpClient.SendAsync(req).ConfigureAwait(false))
                    {
                        if (!res.IsSuccessStatusCode)
                            return null;

                        string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var doc = JObject.Parse(json);
                        var items = doc["items"] as JArray;
                        if (items != null && items.Count > 0)
                        {
                            var item = items[0];
                            var snippet = item["snippet"];
                            var contentDetails = item["contentDetails"];

                            string title = snippet?["title"]?.ToString() ?? "Unknown";
                            string channel = snippet?["channelTitle"]?.ToString() ?? "";
                            string thumb = snippet?["thumbnails"]?["high"]?["url"]?.ToString() ??
                                           snippet?["thumbnails"]?["default"]?["url"]?.ToString() ?? "";
                            string durationStr = contentDetails?["duration"]?.ToString() ?? "";
                            double duration = Tools.ParseIsoDuration(durationStr);
                            string permalink = $"https://www.youtube.com/watch?v={videoId}";

                            var info = new TrackInfo(title, videoId, permalink, thumb, duration, channel);
                            Config.TrackInfos[videoId] = info;
                            Config.SaveCache();
                            return info;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] GetVideoInfoAsync error: {ex}");
            }

            return null;
        }

        public static async Task<List<TrackInfo>> GetBatchVideoDetailsAsync(List<string> videoIds)
        {
            var result = new List<TrackInfo>();
            if (videoIds == null || videoIds.Count == 0)
                return result;

            try
            {
                string apiKey = Config.GetApiKey();
                for (int i = 0; i < videoIds.Count; i += 50)
                {
                    var chunk = videoIds.Skip(i).Take(50).ToList();
                    string idsJoined = string.Join(",", chunk);
                    string url = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,contentDetails&id={idsJoined}&key={apiKey}";

                    using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        await ApplyAuthHeadersAsync(req).ConfigureAwait(false);
                        using (var res = await _httpClient.SendAsync(req).ConfigureAwait(false))
                        {
                            if (!res.IsSuccessStatusCode)
                                continue;

                            string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var doc = JObject.Parse(json);
                            var items = doc["items"] as JArray;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    string id = item["id"]?.ToString();
                                    if (string.IsNullOrEmpty(id)) continue;

                                    var snippet = item["snippet"];
                                    var contentDetails = item["contentDetails"];

                                    string title = snippet?["title"]?.ToString() ?? "Unknown";
                                    string channel = snippet?["channelTitle"]?.ToString() ?? "";
                                    string thumb = snippet?["thumbnails"]?["high"]?["url"]?.ToString() ??
                                                   snippet?["thumbnails"]?["default"]?["url"]?.ToString() ?? "";
                                    string durationStr = contentDetails?["duration"]?.ToString() ?? "";
                                    double duration = Tools.ParseIsoDuration(durationStr);
                                    string permalink = $"https://www.youtube.com/watch?v={id}";

                                    var info = new TrackInfo(title, id, permalink, thumb, duration, channel);
                                    Config.TrackInfos[id] = info;
                                    result.Add(info);
                                }
                            }
                        }
                    }
                }
                Config.SaveCache();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] GetBatchVideoDetailsAsync error: {ex}");
            }

            return result;
        }

        public static async Task<List<TrackInfo>> GetPlaylistTracksAsync(string playlistId, int maxTracks = 500)
        {
            var tracks = new List<TrackInfo>();
            if (string.IsNullOrWhiteSpace(playlistId))
                return tracks;

            try
            {
                string apiKey = Config.GetApiKey();
                string pageToken = "";

                while (tracks.Count < maxTracks)
                {
                    int toFetch = Math.Min(50, maxTracks - tracks.Count);
                    string url = $"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet,contentDetails&maxResults={toFetch}&playlistId={playlistId}&key={apiKey}";
                    if (!string.IsNullOrEmpty(pageToken))
                    {
                        url += $"&pageToken={pageToken}";
                    }

                    using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        await ApplyAuthHeadersAsync(req).ConfigureAwait(false);
                        using (var res = await _httpClient.SendAsync(req).ConfigureAwait(false))
                        {
                            if (!res.IsSuccessStatusCode)
                                break;

                            string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var doc = JObject.Parse(json);
                            var items = doc["items"] as JArray;
                            if (items == null || items.Count == 0)
                                break;

                            var batchIds = new List<string>();
                            foreach (var item in items)
                            {
                                string videoId = item["contentDetails"]?["videoId"]?.ToString() ??
                                                 item["snippet"]?["resourceId"]?["videoId"]?.ToString();

                                if (!string.IsNullOrEmpty(videoId) && !Config.TrackExclusions.Contains(videoId))
                                {
                                    batchIds.Add(videoId);
                                }
                            }

                            // Batch fetch full details including duration
                            var details = await GetBatchVideoDetailsAsync(batchIds).ConfigureAwait(false);
                            tracks.AddRange(details);

                            pageToken = doc["nextPageToken"]?.ToString();
                            if (string.IsNullOrEmpty(pageToken))
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] GetPlaylistTracksAsync error: {ex}");
            }

            return tracks;
        }

        public static async Task<string> GetChannelUploadsPlaylistIdAsync(string input)
        {
            try
            {
                string apiKey = Config.GetApiKey();
                string url = "";

                if (input.Contains("/channel/"))
                {
                    string id = Regex.Match(input, @"\/channel\/([a-zA-Z0-9_-]+)").Groups[1].Value;
                    url = $"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={id}&key={apiKey}";
                }
                else if (input.Contains("/@"))
                {
                    string handle = Regex.Match(input, @"\/@([a-zA-Z0-9_.-]+)").Groups[1].Value;
                    url = $"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&forHandle={handle}&key={apiKey}";
                }
                else if (input.Contains("/user/"))
                {
                    string user = Regex.Match(input, @"\/user\/([a-zA-Z0-9_.-]+)").Groups[1].Value;
                    url = $"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&forUsername={user}&key={apiKey}";
                }
                else if (input.Contains("/c/"))
                {
                    string custom = Regex.Match(input, @"\/c\/([a-zA-Z0-9_.-]+)").Groups[1].Value;
                    url = $"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&forUsername={custom}&key={apiKey}";
                }

                if (!string.IsNullOrEmpty(url))
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        await ApplyAuthHeadersAsync(req).ConfigureAwait(false);
                        using (var res = await _httpClient.SendAsync(req).ConfigureAwait(false))
                        {
                            if (res.IsSuccessStatusCode)
                            {
                                string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                                var doc = JObject.Parse(json);
                                var items = doc["items"] as JArray;
                                if (items != null && items.Count > 0)
                                {
                                    return items[0]?["contentDetails"]?["relatedPlaylists"]?["uploads"]?.ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] GetChannelUploadsPlaylistIdAsync error: {ex}");
            }

            return null;
        }

        public static void AddTracksToPlaylist(IAimpPlayer player, IAimpPlaylist playlist, List<TrackInfo> tracks, string albumOrGroup = "")
        {
            if (player == null || playlist == null || tracks == null || tracks.Count == 0)
                return;

            try
            {
                // Pre-cache all tracks in TrackInfos so the PlayerHook and FileInfo extensions
                // can resolve metadata when AIMP processes these URLs
                foreach (var track in tracks)
                {
                    if (!string.IsNullOrEmpty(track.Id))
                    {
                        Config.TrackInfos[track.Id] = track;
                    }
                }

                playlist.BeginUpdate();

                int addedCount = 0;
                int failedCount = 0;

                foreach (var track in tracks)
                {
                    if (Config.TrackExclusions.Contains(track.Id))
                        continue;

                    string customUrl = $"youtube://{track.Id}/{Tools.SanitizeFileName(track.Title)}.mp4";

                    // Use string URL overload — AIMP will process the URL through its extension pipeline,
                    // including our YouTubePlayerHook which resolves youtube:// to actual stream URLs
                    var addResult = playlist.Add(customUrl, PlaylistFlags.NoCheckFormat | PlaylistFlags.NoExpand | PlaylistFlags.NoAsync, PlaylistFilePosition.EndPosition);

                    if (addResult.ResultType == ActionResultType.OK)
                    {
                        addedCount++;
                    }
                    else
                    {
                        failedCount++;
                        System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Failed to add track '{track.Title}' (ID: {track.Id}): {addResult.ResultType}");
                    }
                }

                playlist.EndUpdate();

                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] AddTracksToPlaylist complete: {addedCount} added, {failedCount} failed out of {tracks.Count} total");

                // Save track cache to disk
                try { Config.SaveCache(); } catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] AddTracksToPlaylist error: {ex}");
                try { playlist.EndUpdate(); } catch { }
            }
        }
    }
}
