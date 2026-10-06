using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AIMPYouTube.Configuration
{
    public static class Config
    {
        public const string DefaultAppKey = "AIzaSyCeEdBFVx7MrZmbEewZCQrCGyJf02UNY8c";
        public const string DefaultClientId = "6695011871-r9bnkpp9rkamnkrgim2iniqq38koff09.apps.googleusercontent.com";
        public const string DefaultClientSecret = "za2mleqMh2GJCC4ojs4WVYY_";

        public static string ConfigFolder { get; private set; } = string.Empty;

        // Settings
        public static string YouTubeKey { get; set; } = string.Empty;
        public static string YouTubeClientId { get; set; } = string.Empty;
        public static string YouTubeClientSecret { get; set; } = string.Empty;
        public static string AccessToken { get; set; } = string.Empty;
        public static string RefreshToken { get; set; } = string.Empty;
        public static long TokenExpires { get; set; }
        public static string UserYTName { get; set; } = string.Empty;
        public static string UserName { get; set; } = string.Empty;

        // yt-dlp Settings
        public static string YtDlpPath { get; set; } = string.Empty;
        public static string YtDlpParams { get; set; } = "-f ba[protocol^=m3u8]/234/233/140/139/ba[ext=m4a]/best[ext=mp4]/best";
        public static string CookiesBrowser { get; set; } = "none";
        public static int YtDlpTimeout { get; set; } = 30;
        public static bool ForceYtDlp { get; set; } = true;

        // Behavior Settings
        public static bool CheckOnStartup { get; set; } = true;
        public static bool MonitorUserPlaylists { get; set; } = true;
        public static bool CreateNewPlaylist { get; set; } = true;
        public static int MonitorIntervalMinutes { get; set; } = 15;
        public static int LimitUserStreamValue { get; set; } = 5000;
        public static bool LimitUserStream { get; set; } = false;

        // Collections
        public static HashSet<string> TrackExclusions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static List<MonitorUrl> MonitorUrls { get; } = new List<MonitorUrl>();
        public static List<PlaylistConfig> UserPlaylists { get; } = new List<PlaylistConfig>();
        public static ConcurrentDictionary<string, TrackInfo> TrackInfos { get; } = new ConcurrentDictionary<string, TrackInfo>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _configLock = new object();
        private static readonly object _cacheLock = new object();

        public static void Initialize(string profilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(profilePath))
                {
                    profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AIMP");
                }

                ConfigFolder = Path.Combine(profilePath, "AIMPYouTube");
                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }

                LoadSettings();
                LoadExtendedConfig();
                LoadCache();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Config init error: {ex}");
            }
        }

        public static string GetApiKey()
        {
            return !string.IsNullOrWhiteSpace(YouTubeKey) ? YouTubeKey : DefaultAppKey;
        }

        public static string GetClientId()
        {
            return !string.IsNullOrWhiteSpace(YouTubeClientId) ? YouTubeClientId : DefaultClientId;
        }

        public static string GetClientSecret()
        {
            return !string.IsNullOrWhiteSpace(YouTubeClientSecret) ? YouTubeClientSecret : DefaultClientSecret;
        }

        public static bool IsConnected => !string.IsNullOrEmpty(AccessToken);

        public static bool UseAccount => string.IsNullOrEmpty(YouTubeKey) || !string.IsNullOrEmpty(YouTubeClientId);

        public static void SaveSettings()
        {
            lock (_configLock)
            {
                try
                {
                    var settings = new SettingsDto
                    {
                        YouTubeKey = YouTubeKey,
                        YouTubeClientId = YouTubeClientId,
                        YouTubeClientSecret = YouTubeClientSecret,
                        AccessToken = AccessToken,
                        RefreshToken = RefreshToken,
                        TokenExpires = TokenExpires,
                        UserYTName = UserYTName,
                        UserName = UserName,
                        YtDlpPath = YtDlpPath,
                        YtDlpParams = YtDlpParams,
                        CookiesBrowser = CookiesBrowser,
                        YtDlpTimeout = YtDlpTimeout,
                        ForceYtDlp = ForceYtDlp,
                        CheckOnStartup = CheckOnStartup,
                        MonitorUserPlaylists = MonitorUserPlaylists,
                        CreateNewPlaylist = CreateNewPlaylist,
                        MonitorIntervalMinutes = MonitorIntervalMinutes,
                        LimitUserStream = LimitUserStream,
                        LimitUserStreamValue = LimitUserStreamValue
                    };

                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    File.WriteAllText(Path.Combine(ConfigFolder, "Settings.json"), json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] SaveSettings error: {ex}");
                }
            }
        }

        public static void LoadSettings()
        {
            lock (_configLock)
            {
                try
                {
                    string file = Path.Combine(ConfigFolder, "Settings.json");
                    if (File.Exists(file))
                    {
                        string json = File.ReadAllText(file);
                        var settings = JsonConvert.DeserializeObject<SettingsDto>(json);
                        if (settings != null)
                        {
                            YouTubeKey = settings.YouTubeKey ?? string.Empty;
                            YouTubeClientId = settings.YouTubeClientId ?? string.Empty;
                            YouTubeClientSecret = settings.YouTubeClientSecret ?? string.Empty;
                            AccessToken = settings.AccessToken ?? string.Empty;
                            RefreshToken = settings.RefreshToken ?? string.Empty;
                            TokenExpires = settings.TokenExpires;
                            UserYTName = settings.UserYTName ?? string.Empty;
                            UserName = settings.UserName ?? string.Empty;
                            YtDlpPath = settings.YtDlpPath ?? string.Empty;
                            string p = settings.YtDlpParams;
                            if (string.IsNullOrWhiteSpace(p) || p.Trim().Equals("-f bestaudio/best", StringComparison.OrdinalIgnoreCase) || p.Contains("140/bestaudio") || p.Trim().Equals("-f ba[protocol^=m3u8]/234/233/140/best", StringComparison.OrdinalIgnoreCase) || p.Trim().Equals("-f best[ext=mp4]/best", StringComparison.OrdinalIgnoreCase))
                            {
                                p = "-f ba[protocol^=m3u8]/234/233/140/139/ba[ext=m4a]/best[ext=mp4]/best";
                            }
                            YtDlpParams = p;
                            CookiesBrowser = string.IsNullOrWhiteSpace(settings.CookiesBrowser) ? "none" : settings.CookiesBrowser;
                            YtDlpTimeout = settings.YtDlpTimeout <= 0 ? 30 : settings.YtDlpTimeout;
                            ForceYtDlp = settings.ForceYtDlp;
                            CheckOnStartup = settings.CheckOnStartup;
                            MonitorUserPlaylists = settings.MonitorUserPlaylists;
                            CreateNewPlaylist = settings.CreateNewPlaylist;
                            MonitorIntervalMinutes = settings.MonitorIntervalMinutes <= 0 ? 15 : settings.MonitorIntervalMinutes;
                            LimitUserStream = settings.LimitUserStream;
                            LimitUserStreamValue = settings.LimitUserStreamValue <= 0 ? 5000 : settings.LimitUserStreamValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] LoadSettings error: {ex}");
                }
            }
        }

        public static void SaveExtendedConfig()
        {
            lock (_configLock)
            {
                try
                {
                    var data = new ExtendedConfigDto
                    {
                        Exclusions = new List<string>(TrackExclusions),
                        MonitorURLs = new List<MonitorUrl>(MonitorUrls),
                        UserPlaylists = new List<PlaylistConfig>(UserPlaylists)
                    };

                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(Path.Combine(ConfigFolder, "Config.json"), json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] SaveExtendedConfig error: {ex}");
                }
            }
            SaveCache();
        }

        public static void LoadExtendedConfig()
        {
            lock (_configLock)
            {
                try
                {
                    string file = Path.Combine(ConfigFolder, "Config.json");
                    if (File.Exists(file))
                    {
                        string json = File.ReadAllText(file);
                        var data = JsonConvert.DeserializeObject<ExtendedConfigDto>(json);
                        if (data != null)
                        {
                            TrackExclusions.Clear();
                            if (data.Exclusions != null)
                            {
                                foreach (var id in data.Exclusions)
                                    TrackExclusions.Add(id);
                            }

                            MonitorUrls.Clear();
                            if (data.MonitorURLs != null)
                            {
                                MonitorUrls.AddRange(data.MonitorURLs);
                            }

                            UserPlaylists.Clear();
                            if (data.UserPlaylists != null)
                            {
                                UserPlaylists.AddRange(data.UserPlaylists);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] LoadExtendedConfig error: {ex}");
                }
            }
        }

        public static void SaveCache()
        {
            lock (_cacheLock)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(TrackInfos, Formatting.Indented);
                    File.WriteAllText(Path.Combine(ConfigFolder, "Cache.json"), json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] SaveCache error: {ex}");
                }
            }
        }

        public static void LoadCache()
        {
            lock (_cacheLock)
            {
                try
                {
                    string file = Path.Combine(ConfigFolder, "Cache.json");
                    if (File.Exists(file))
                    {
                        string json = File.ReadAllText(file);
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, TrackInfo>>(json);
                        if (dict != null)
                        {
                            TrackInfos.Clear();
                            foreach (var kvp in dict)
                            {
                                kvp.Value.Id = kvp.Key;
                                TrackInfos[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] LoadCache error: {ex}");
                }
            }
        }

        private class SettingsDto
        {
            public string YouTubeKey { get; set; }
            public string YouTubeClientId { get; set; }
            public string YouTubeClientSecret { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public long TokenExpires { get; set; }
            public string UserYTName { get; set; }
            public string UserName { get; set; }
            public string YtDlpPath { get; set; }
            public string YtDlpParams { get; set; }
            public string CookiesBrowser { get; set; }
            public int YtDlpTimeout { get; set; }
            public bool ForceYtDlp { get; set; }
            public bool CheckOnStartup { get; set; }
            public bool MonitorUserPlaylists { get; set; }
            public bool CreateNewPlaylist { get; set; }
            public int MonitorIntervalMinutes { get; set; }
            public bool LimitUserStream { get; set; }
            public int LimitUserStreamValue { get; set; }
        }

        private class ExtendedConfigDto
        {
            public List<string> Exclusions { get; set; }
            public List<MonitorUrl> MonitorURLs { get; set; }
            public List<PlaylistConfig> UserPlaylists { get; set; }
        }
    }
}
