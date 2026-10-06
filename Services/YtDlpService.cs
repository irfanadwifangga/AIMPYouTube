using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AIMPYouTube.Configuration;

namespace AIMPYouTube.Services
{
    public static class YtDlpService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _resolvedYtDlpPath = string.Empty;

        public static string GetYtDlpPath()
        {
            if (!string.IsNullOrEmpty(_resolvedYtDlpPath) && File.Exists(_resolvedYtDlpPath))
                return _resolvedYtDlpPath;

            if (!string.IsNullOrWhiteSpace(Config.YtDlpPath) && File.Exists(Config.YtDlpPath))
            {
                _resolvedYtDlpPath = Config.YtDlpPath;
                return _resolvedYtDlpPath;
            }

            // Check next to this assembly
            string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string localYtDlp = Path.Combine(asmDir, "yt-dlp.exe");
            if (File.Exists(localYtDlp))
            {
                _resolvedYtDlpPath = localYtDlp;
                return _resolvedYtDlpPath;
            }

            // Check config folder
            if (!string.IsNullOrEmpty(Config.ConfigFolder))
            {
                string configYtDlp = Path.Combine(Config.ConfigFolder, "yt-dlp.exe");
                if (File.Exists(configYtDlp))
                {
                    _resolvedYtDlpPath = configYtDlp;
                    return _resolvedYtDlpPath;
                }
            }

            // Check PATH
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var p in pathEnv.Split(Path.PathSeparator))
            {
                try
                {
                    string candidate = Path.Combine(p.Trim(), "yt-dlp.exe");
                    if (File.Exists(candidate))
                    {
                        _resolvedYtDlpPath = candidate;
                        return _resolvedYtDlpPath;
                    }
                }
                catch { }
            }

            return localYtDlp; // default target
        }

        public static string GetStreamUrl(string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
                return string.Empty;

            // Check cache
            if (Config.TrackInfos.TryGetValue(videoId, out var cached) && !string.IsNullOrEmpty(cached.StreamUrl))
            {
                // Invalidate old webm format streams that AIMP cannot demux
                if (!cached.StreamUrl.Contains("audio%2Fwebm") && !cached.StreamUrl.Contains("audio/webm") && cached.StreamUrlExpires > DateTime.UtcNow)
                {
                    return cached.StreamUrl;
                }
            }

            string exePath = GetYtDlpPath();
            if (!File.Exists(exePath))
            {
                // Auto-download yt-dlp if not found
                bool downloaded = DownloadYtDlp(exePath);
                if (!downloaded || !File.Exists(exePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] yt-dlp.exe not found at {exePath}");
                    return string.Empty;
                }
            }

            string formatParam = !string.IsNullOrWhiteSpace(Config.YtDlpParams) ? Config.YtDlpParams : "-f ba[protocol^=m3u8]/234/233/140/139/ba[ext=m4a]/best[ext=mp4]/best";
            if (!formatParam.StartsWith("-f", StringComparison.OrdinalIgnoreCase))
            {
                formatParam = $"-f {formatParam}";
            }

            int timeoutSec = Config.YtDlpTimeout > 0 ? Config.YtDlpTimeout : 30;

            string cookiesParam = "";
            if (!string.IsNullOrWhiteSpace(Config.CookiesBrowser) && !Config.CookiesBrowser.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                cookiesParam = $"--cookies-from-browser {Config.CookiesBrowser.ToLower()} ";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"{cookiesParam}-g {formatParam} --no-warnings -- \"https://www.youtube.com/watch?v={videoId}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!process.WaitForExit(timeoutSec * 1000))
                    {
                        try { process.Kill(); } catch { }
                        return string.Empty;
                    }

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        string streamUrl = output.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        
                        // Cache for 4 hours
                        if (Config.TrackInfos.TryGetValue(videoId, out var info))
                        {
                            info.StreamUrl = streamUrl;
                            info.StreamUrlExpires = DateTime.UtcNow.AddHours(4);
                        }

                        return streamUrl;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] yt-dlp error (code {process.ExitCode}): {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] yt-dlp execution error: {ex}");
            }

            return string.Empty;
        }

        public static bool DownloadYtDlp(string targetPath)
        {
            try
            {
                string url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                byte[] data = _httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(targetPath, data);
                _resolvedYtDlpPath = targetPath;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Failed to download yt-dlp: {ex}");
                return false;
            }
        }

        public static async Task<System.Collections.Generic.List<TrackInfo>> GetPlaylistTracksWithYtDlpAsync(string playlistUrl)
        {
            var tracks = new System.Collections.Generic.List<TrackInfo>();
            if (string.IsNullOrWhiteSpace(playlistUrl))
                return tracks;

            string exePath = GetYtDlpPath();
            if (!File.Exists(exePath))
                return tracks;

            string cookiesParam = "";
            if (!string.IsNullOrWhiteSpace(Config.CookiesBrowser) && !Config.CookiesBrowser.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                cookiesParam = $"--cookies-from-browser {Config.CookiesBrowser.ToLower()} ";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"{cookiesParam}--flat-playlist -J --no-warnings -- \"{playlistUrl}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string json = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    await Task.Run(() => process.WaitForExit(30000)).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var doc = Newtonsoft.Json.Linq.JObject.Parse(json);
                        var entries = doc["entries"] as Newtonsoft.Json.Linq.JArray;
                        if (entries != null)
                        {
                            foreach (var item in entries)
                            {
                                string id = item["id"]?.ToString();
                                string title = item["title"]?.ToString() ?? "Untitled Track";
                                string uploader = item["uploader"]?.ToString() ?? item["channel"]?.ToString() ?? "";
                                double duration = item["duration"]?.ToObject<double>() ?? 0;
                                string thumb = item["thumbnails"]?.LastOrDefault()?["url"]?.ToString() ?? "";
                                string permalink = $"https://www.youtube.com/watch?v={id}";

                                if (!string.IsNullOrEmpty(id) && !Config.TrackExclusions.Contains(id))
                                {
                                    var info = new TrackInfo(title, id, permalink, thumb, duration, uploader);
                                    Config.TrackInfos[id] = info;
                                    tracks.Add(info);
                                }
                            }
                        }
                        else if (doc["id"] != null)
                        {
                            // Single track returned in json
                            string id = doc["id"]?.ToString();
                            string title = doc["title"]?.ToString() ?? "Untitled Track";
                            string uploader = doc["uploader"]?.ToString() ?? "";
                            double duration = doc["duration"]?.ToObject<double>() ?? 0;
                            string thumb = doc["thumbnail"]?.ToString() ?? "";
                            string permalink = $"https://www.youtube.com/watch?v={id}";

                            if (!string.IsNullOrEmpty(id))
                            {
                                var info = new TrackInfo(title, id, permalink, thumb, duration, uploader);
                                Config.TrackInfos[id] = info;
                                tracks.Add(info);
                            }
                        }

                        Config.SaveCache();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] GetPlaylistTracksWithYtDlp error: {ex}");
            }

            return tracks;
        }

        public static bool UpdateYtDlp(bool showWindow = true)
        {
            try
            {
                string exePath = GetYtDlpPath();
                if (!File.Exists(exePath))
                    return DownloadYtDlp(exePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-U",
                    UseShellExecute = showWindow,
                    CreateNoWindow = !showWindow
                };

                using (var proc = Process.Start(startInfo))
                {
                    proc?.WaitForExit(60000);
                    return proc?.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Update yt-dlp failed: {ex}");
                return false;
            }
        }
    }
}
