using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIMPYouTube.Configuration;
using Newtonsoft.Json.Linq;

namespace AIMPYouTube.Services
{
    public static class OAuthService
    {
        public const int Port = 35910;
        public const string RedirectUri = "http://localhost:35910/";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static HttpListener _listener;
        private static CancellationTokenSource _cts;

        public static bool IsListening => _listener != null && _listener.IsListening;

        public static event Action<bool, string> AuthCompleted;

        public static async Task<bool> StartAuthFlowAsync()
        {
            try
            {
                CancelAuthFlow();

                _cts = new CancellationTokenSource();
                _listener = new HttpListener();
                _listener.Prefixes.Add(RedirectUri);
                _listener.Start();

                string clientId = Config.GetClientId();
                string scopes = Uri.EscapeDataString("https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email");
                string authUrl = $"https://accounts.google.com/o/oauth2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&scope={scopes}&access_type=offline&prompt=consent";

                // Open browser
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

                // Listen for callback in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var context = await _listener.GetContextAsync().ConfigureAwait(false);
                        var request = context.Request;
                        var response = context.Response;

                        string code = request.QueryString["code"];
                        string error = request.QueryString["error"];

                        string htmlResponse;
                        if (!string.IsNullOrEmpty(code))
                        {
                            bool success = await ExchangeCodeForTokenAsync(code).ConfigureAwait(false);
                            if (success)
                            {
                                await FetchUserInfoAsync().ConfigureAwait(false);
                                htmlResponse = @"<!DOCTYPE html><html><head><meta charset='utf-8'><title>AIMP YouTube Login</title>
<style>body{font-family:'Segoe UI',sans-serif;background:#181818;color:#eee;text-align:center;padding:50px 20px;}h1{color:#4CAF50;}p{color:#aaa;font-size:16px;}div{background:#222;max-width:460px;margin:0 auto;padding:30px;border-radius:12px;box-shadow:0 8px 24px rgba(0,0,0,0.5);}</style>
</head><body><div><h1>&#10004; Login Berhasil!</h1><p>Akun YouTube kamu sudah terhubung ke AIMP.<br>Kamu dapat menutup tab ini dan kembali ke AIMP.</p></div></body></html>";
                                AuthCompleted?.Invoke(true, Config.UserName);
                            }
                            else
                            {
                                htmlResponse = @"<!DOCTYPE html><html><head><meta charset='utf-8'><title>AIMP YouTube Login</title>
<style>body{font-family:'Segoe UI',sans-serif;background:#181818;color:#eee;text-align:center;padding:50px 20px;}h1{color:#f44336;}p{color:#aaa;font-size:16px;}div{background:#222;max-width:460px;margin:0 auto;padding:30px;border-radius:12px;box-shadow:0 8px 24px rgba(0,0,0,0.5);}</style>
</head><body><div><h1>&#10008; Gagal Menukar Token</h1><p>Terjadi kesalahan saat memproses otentikasi Google. Silakan coba kembali.</p></div></body></html>";
                                AuthCompleted?.Invoke(false, "Gagal menukar token otentikasi.");
                            }
                        }
                        else
                        {
                            htmlResponse = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>AIMP YouTube Login</title>
<style>body{{font-family:'Segoe UI',sans-serif;background:#181818;color:#eee;text-align:center;padding:50px 20px;}}h1{{color:#f44336;}}p{{color:#aaa;font-size:16px;}}div{{background:#222;max-width:460px;margin:0 auto;padding:30px;border-radius:12px;box-shadow:0 8px 24px rgba(0,0,0,0.5);}}</style>
</head><body><div><h1>&#10008; Otorisasi Dibatalkan</h1><p>{error ?? "Akses ditolak oleh pengguna."}</p></div></body></html>";
                            AuthCompleted?.Invoke(false, error ?? "Akses ditolak");
                        }

                        byte[] buffer = Encoding.UTF8.GetBytes(htmlResponse);
                        response.ContentType = "text/html; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        response.OutputStream.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] OAuth listener error: {ex}");
                    }
                    finally
                    {
                        CancelAuthFlow();
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] StartAuthFlowAsync error: {ex}");
                CancelAuthFlow();
                return false;
            }
        }

        public static void CancelAuthFlow()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (_listener != null)
                {
                    if (_listener.IsListening)
                        _listener.Stop();
                    _listener.Close();
                    _listener = null;
                }
            }
            catch { }
        }

        private static async Task<bool> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                string clientId = Config.GetClientId();
                string clientSecret = Config.GetClientSecret();

                var postData = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("code", code),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_secret", clientSecret),
                    new System.Collections.Generic.KeyValuePair<string, string>("redirect_uri", RedirectUri),
                    new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "authorization_code")
                });

                using (var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", postData).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        return false;

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var doc = JObject.Parse(json);

                    string accessToken = doc["access_token"]?.ToString();
                    string refreshToken = doc["refresh_token"]?.ToString();
                    long expiresIn = doc["expires_in"]?.ToObject<long>() ?? 3600;

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        Config.AccessToken = accessToken;
                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            Config.RefreshToken = refreshToken;
                        }
                        Config.TokenExpires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn;
                        Config.SaveSettings();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] ExchangeCodeForToken error: {ex}");
            }

            return false;
        }

        public static async Task<bool> RefreshTokenIfNeededAsync()
        {
            if (string.IsNullOrEmpty(Config.RefreshToken))
                return false;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < Config.TokenExpires - 300) // 5 minutes buffer
                return true;

            try
            {
                string clientId = Config.GetClientId();
                string clientSecret = Config.GetClientSecret();

                var postData = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("refresh_token", Config.RefreshToken),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_secret", clientSecret),
                    new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "refresh_token")
                });

                using (var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", postData).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        return false;

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var doc = JObject.Parse(json);

                    string accessToken = doc["access_token"]?.ToString();
                    long expiresIn = doc["expires_in"]?.ToObject<long>() ?? 3600;

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        Config.AccessToken = accessToken;
                        Config.TokenExpires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn;
                        Config.SaveSettings();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] RefreshTokenIfNeeded error: {ex}");
            }

            return false;
        }

        public static async Task FetchUserInfoAsync()
        {
            if (string.IsNullOrEmpty(Config.AccessToken))
                return;

            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo"))
                {
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.AccessToken);
                    using (var res = await _httpClient.SendAsync(req).ConfigureAwait(false))
                    {
                        if (res.IsSuccessStatusCode)
                        {
                            string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var doc = JObject.Parse(json);

                            string name = doc["name"]?.ToString() ?? "";
                            string email = doc["email"]?.ToString() ?? "";

                            Config.UserName = !string.IsNullOrEmpty(email) ? $"{name} ({email})" : name;
                            Config.UserYTName = name;
                            Config.SaveSettings();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] FetchUserInfo error: {ex}");
            }
        }

        public static void Disconnect()
        {
            Config.AccessToken = string.Empty;
            Config.RefreshToken = string.Empty;
            Config.TokenExpires = 0;
            Config.UserName = string.Empty;
            Config.UserYTName = string.Empty;
            Config.SaveSettings();
            AuthCompleted?.Invoke(false, "Disconnected");
        }
    }
}
