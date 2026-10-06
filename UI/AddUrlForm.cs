using System;
using System.Drawing;
using System.Windows.Forms;
using AIMP.SDK;
using AIMP.SDK.Playlist.Objects;
using AIMPYouTube.Configuration;
using AIMPYouTube.Core;
using AIMPYouTube.Services;

namespace AIMPYouTube.UI
{
    public class AddUrlForm : Form
    {
        private readonly IAimpPlayer _player;
        private TextBox _txtUrl;
        private TextBox _txtPlaylistTitle;
        private CheckBox _chkCreateNewPlaylist;
        private Button _btnAdd;
        private Button _btnCancel;
        private Label _lblStatus;

        public AddUrlForm(IAimpPlayer player)
        {
            _player = player;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = Localization.Get(@"YouTube.AddURL\Title", "Tambah URL YouTube");
            this.Size = new Size(520, 260);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 245, 245);

            var lblUrl = new Label
            {
                Text = Localization.Get(@"YouTube.AddURL\URL", "URL YouTube (Video, Playlist, Channel, atau @handle):"),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            int urlBoxWidth = Config.IsConnected ? 340 : 465;

            _txtUrl = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(urlBoxWidth, 25)
            };
            _txtUrl.TextChanged += (s, e) => UpdatePlaylistTitleSuggestion();

            if (Config.IsConnected)
            {
                var btnMyPlaylists = new Button
                {
                    Text = Localization.Get(@"YouTube.AddURL\MyPlaylists", "Playlist Saya..."),
                    Location = new Point(368, 44),
                    Size = new Size(117, 27),
                    FlatStyle = FlatStyle.System
                };

                btnMyPlaylists.Click += async (s, e) =>
                {
                    btnMyPlaylists.Enabled = false;
                    _lblStatus.Text = Localization.Get(@"YouTube.AddURL\StatusFetchingMyPlaylists", "Memuat playlist akun kamu...");
                    var playlists = await YouTubeService.GetUserPlaylistsAsync();
                    _lblStatus.Text = "";
                    btnMyPlaylists.Enabled = true;

                    if (playlists != null && playlists.Count > 0)
                    {
                        var menu = new ContextMenuStrip();
                        foreach (var pl in playlists)
                        {
                            var item = menu.Items.Add(pl.Title);
                            item.Click += (ms, me) =>
                            {
                                _txtUrl.Text = pl.Url;
                                _txtPlaylistTitle.Text = pl.Title;
                            };
                        }
                        menu.Show(btnMyPlaylists, new Point(0, btnMyPlaylists.Height));
                    }
                    else
                    {
                        MessageBox.Show(this, Localization.Get(@"YouTube.Messages\NoUserPlaylists", "Tidak ada playlist ditemukan pada akun ini."), "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                this.Controls.Add(btnMyPlaylists);
            }

            _chkCreateNewPlaylist = new CheckBox
            {
                Text = Localization.Get(@"YouTube.AddURL\CreateNew", "Buat daftar putar baru untuk item ini"),
                Location = new Point(20, 80),
                AutoSize = true,
                Checked = Config.CreateNewPlaylist
            };
            _chkCreateNewPlaylist.CheckedChanged += (s, e) =>
            {
                _txtPlaylistTitle.Enabled = _chkCreateNewPlaylist.Checked;
                Config.CreateNewPlaylist = _chkCreateNewPlaylist.Checked;
            };

            var lblPlTitle = new Label
            {
                Text = Localization.Get(@"YouTube.AddURL\PlaylistName", "Nama Daftar Putar (Opsional):"),
                Location = new Point(20, 110),
                AutoSize = true
            };

            _txtPlaylistTitle = new TextBox
            {
                Location = new Point(20, 135),
                Size = new Size(465, 25),
                Enabled = _chkCreateNewPlaylist.Checked,
                Text = "YouTube"
            };

            _lblStatus = new Label
            {
                Text = "",
                Location = new Point(20, 175),
                Size = new Size(280, 30),
                ForeColor = Color.DarkSlateBlue
            };

            _btnAdd = new Button
            {
                Text = Localization.Get(@"YouTube.AddURL\OK", "Tambah ke AIMP"),
                Location = new Point(305, 175),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAdd.FlatAppearance.BorderSize = 0;
            _btnAdd.Click += async (s, e) => await OnAddClickedAsync();

            _btnCancel = new Button
            {
                Text = Localization.Get(@"YouTube.AddURL\Cancel", "Batal"),
                Location = new Point(415, 175),
                Size = new Size(70, 32),
                FlatStyle = FlatStyle.System
            };
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(lblUrl);
            this.Controls.Add(_txtUrl);
            this.Controls.Add(_chkCreateNewPlaylist);
            this.Controls.Add(lblPlTitle);
            this.Controls.Add(_txtPlaylistTitle);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_btnAdd);
            this.Controls.Add(_btnCancel);
            this.AcceptButton = _btnAdd;
            this.CancelButton = _btnCancel;
        }

        private void UpdatePlaylistTitleSuggestion()
        {
            string url = _txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;

            string videoId = Tools.TrackIdFromUrl(url);
            string playlistId = Tools.PlaylistIdFromUrl(url);

            if (!string.IsNullOrEmpty(playlistId))
            {
                _txtPlaylistTitle.Text = "YouTube Playlist";
            }
            else if (url.Contains("/@") || url.Contains("/channel/") || url.Contains("/user/"))
            {
                _txtPlaylistTitle.Text = "YouTube Channel";
            }
            else if (!string.IsNullOrEmpty(videoId))
            {
                _txtPlaylistTitle.Text = "YouTube";
            }
        }

        private async System.Threading.Tasks.Task OnAddClickedAsync()
        {
            string url = _txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                string msg = Localization.Get(@"YouTube.Messages\EmptyUrl", "Silakan masukkan URL YouTube yang valid.");
                MessageBox.Show(this, msg, "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnAdd.Enabled = false;
            _lblStatus.Text = Localization.Get(@"YouTube.AddURL\StatusResolving", "Memproses metadata YouTube...");

            try
            {
                IAimpPlaylist targetPlaylist = null;
                if (_chkCreateNewPlaylist.Checked)
                {
                    string plName = string.IsNullOrWhiteSpace(_txtPlaylistTitle.Text) ? "YouTube" : _txtPlaylistTitle.Text.Trim();
                    var createResult = _player.ServicePlaylistManager.CreatePlaylist(plName, true);
                    if (createResult.ResultType == ActionResultType.OK)
                    {
                        targetPlaylist = createResult.Result;
                    }
                }
                else
                {
                    var activeResult = _player.ServicePlaylistManager.GetActivePlaylist();
                    if (activeResult.ResultType == ActionResultType.OK)
                    {
                        targetPlaylist = activeResult.Result;
                    }
                    else
                    {
                        var createResult = _player.ServicePlaylistManager.CreatePlaylist("YouTube", true);
                        targetPlaylist = createResult.Result;
                    }
                }

                if (targetPlaylist == null)
                {
                    string noPlMsg = Localization.Get(@"YouTube.Messages\NoPlaylist", "Gagal mengakses atau membuat daftar putar AIMP.");
                    MessageBox.Show(this, noPlMsg, "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _btnAdd.Enabled = true;
                    return;
                }

                string playlistId = Tools.PlaylistIdFromUrl(url);
                string videoId = Tools.TrackIdFromUrl(url);
                System.Collections.Generic.List<TrackInfo> tracks = null;

                if (!string.IsNullOrEmpty(playlistId))
                {
                    _lblStatus.Text = Localization.Get(@"YouTube.AddURL\StatusFetchingPlaylist", "Mengambil daftar trek playlist...");
                    tracks = await YouTubeService.GetPlaylistTracksAsync(playlistId).ConfigureAwait(true);

                    // Fallback to yt-dlp if API returned empty (e.g. Mix playlist or private)
                    if (tracks == null || tracks.Count == 0)
                    {
                        _lblStatus.Text = "Memproses playlist via yt-dlp...";
                        tracks = await YtDlpService.GetPlaylistTracksWithYtDlpAsync(url).ConfigureAwait(true);
                    }
                }
                else if (url.Contains("/@") || url.Contains("/channel/") || url.Contains("/user/") || url.Contains("/c/"))
                {
                    _lblStatus.Text = Localization.Get(@"YouTube.AddURL\StatusFetchingChannel", "Mengambil daftar video channel...");
                    string uploadsPlId = await YouTubeService.GetChannelUploadsPlaylistIdAsync(url).ConfigureAwait(true);
                    if (!string.IsNullOrEmpty(uploadsPlId))
                    {
                        tracks = await YouTubeService.GetPlaylistTracksAsync(uploadsPlId).ConfigureAwait(true);
                    }

                    if (tracks == null || tracks.Count == 0)
                    {
                        _lblStatus.Text = "Memproses channel via yt-dlp...";
                        tracks = await YtDlpService.GetPlaylistTracksWithYtDlpAsync(url).ConfigureAwait(true);
                    }
                }
                else if (!string.IsNullOrEmpty(videoId))
                {
                    _lblStatus.Text = Localization.Get(@"YouTube.AddURL\StatusFetchingVideo", "Mengambil detail video...");
                    var track = await YouTubeService.GetVideoInfoAsync(videoId).ConfigureAwait(true);
                    if (track != null)
                    {
                        tracks = new System.Collections.Generic.List<TrackInfo> { track };
                    }
                    else
                    {
                        // Fallback: yt-dlp single track info
                        tracks = await YtDlpService.GetPlaylistTracksWithYtDlpAsync(url).ConfigureAwait(true);
                        if (tracks == null || tracks.Count == 0)
                        {
                            var fallback = new TrackInfo($"YouTube Track ({videoId})", videoId, $"https://www.youtube.com/watch?v={videoId}", "", 0);
                            tracks = new System.Collections.Generic.List<TrackInfo> { fallback };
                        }
                    }
                }
                else
                {
                    // Generic URL fallback (e.g. music.youtube.com or direct link)
                    _lblStatus.Text = "Memproses URL via yt-dlp...";
                    tracks = await YtDlpService.GetPlaylistTracksWithYtDlpAsync(url).ConfigureAwait(true);
                }

                if (tracks != null && tracks.Count > 0)
                {
                    YouTubeService.AddTracksToPlaylist(_player, targetPlaylist, tracks);
                    try
                    {
                        _player.ServicePlaylistManager.SetActivePlaylist(targetPlaylist);
                    }
                    catch { }

                    _lblStatus.Text = string.Format(Localization.Get(@"YouTube.AddURL\StatusSuccess", "Berhasil menambahkan {0} trek!"), tracks.Count);
                    await System.Threading.Tasks.Task.Delay(600);
                    this.Close();
                    return;
                }

                string cantResMsg = Localization.Get(@"YouTube.Messages\CantResolve", "Tidak dapat memproses URL YouTube ini.");
                MessageBox.Show(this, cantResMsg, "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error: {ex.Message}", "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnAdd.Enabled = true;
                _lblStatus.Text = "";
            }
        }
    }
}
