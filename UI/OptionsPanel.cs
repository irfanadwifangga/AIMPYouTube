using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIMPYouTube.Configuration;
using AIMPYouTube.Core;
using AIMPYouTube.Services;

namespace AIMPYouTube.UI
{
    public class OptionsPanel : UserControl
    {
        // Google Account / OAuth
        private Label _lblAccountStatus;
        private Button _btnConnectGoogle;
        private Button _btnDisconnectGoogle;
        private TextBox _txtClientId;
        private TextBox _txtClientSecret;
        private TextBox _txtApiKey;
        private Panel _pnlAdvancedApi;
        private LinkLabel _lnkToggleAdvanced;

        // yt-dlp & Cookies
        private ComboBox _cboBrowserCookies;
        private TextBox _txtYtDlpPath;
        private TextBox _txtYtDlpParams;
        private CheckBox _chkCheckStartup;
        private CheckBox _chkCreateNewPlaylist;
        private Label _lblYtDlpStatus;
        private Button _btnBrowse;
        private Button _btnUpdateYtDlp;
        private Button _btnDownloadYtDlp;

        public OptionsPanel()
        {
            InitializeComponent();
            OAuthService.AuthCompleted += OnAuthCompleted;
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(530, 620);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.Transparent;
            this.AutoScroll = true;

            int y = 10;

            // ================= 1. Google Account Section =================
            var grpAccount = new GroupBox
            {
                Text = Localization.Get(@"YouTube.Options\AccountTitle", "Akun Google / YouTube"),
                Location = new Point(10, y),
                Size = new Size(500, 160)
            };

            _lblAccountStatus = new Label
            {
                Text = Localization.Get(@"YouTube.Options\StatusNotConnected", "Status: Belum Terhubung"),
                Location = new Point(15, 25),
                Size = new Size(470, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.DimGray
            };

            _btnConnectGoogle = new Button
            {
                Text = Localization.Get(@"YouTube.Options\ConnectGoogle", "Hubungkan Akun Google..."),
                Location = new Point(15, 52),
                Size = new Size(185, 30),
                BackColor = Color.FromArgb(66, 133, 244),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnConnectGoogle.FlatAppearance.BorderSize = 0;
            _btnConnectGoogle.Click += async (s, e) =>
            {
                _btnConnectGoogle.Enabled = false;
                _lblAccountStatus.Text = Localization.Get(@"YouTube.Options\StatusWaitingAuth", "Menunggu otentikasi di browser...");
                _lblAccountStatus.ForeColor = Color.DarkSlateBlue;

                bool started = await OAuthService.StartAuthFlowAsync();
                if (!started)
                {
                    _lblAccountStatus.Text = Localization.Get(@"YouTube.Options\StatusAuthError", "Gagal memulai otentikasi browser.");
                    _lblAccountStatus.ForeColor = Color.Crimson;
                    _btnConnectGoogle.Enabled = true;
                }
            };

            _btnDisconnectGoogle = new Button
            {
                Text = Localization.Get(@"YouTube.Options\Disconnect", "Putuskan"),
                Location = new Point(208, 52),
                Size = new Size(95, 30),
                FlatStyle = FlatStyle.System,
                Visible = false
            };
            _btnDisconnectGoogle.Click += (s, e) =>
            {
                OAuthService.Disconnect();
                UpdateAccountUI();
            };

            _lnkToggleAdvanced = new LinkLabel
            {
                Text = Localization.Get(@"YouTube.Options\AdvancedApi", "Pengaturan API Lanjutan (Client ID / API Key)..."),
                Location = new Point(15, 92),
                AutoSize = true
            };

            _pnlAdvancedApi = new Panel
            {
                Location = new Point(15, 115),
                Size = new Size(470, 0),
                Visible = false
            };

            var lblApi = new Label { Text = Localization.Get(@"YouTube.Options\ApiKeyPrompt", "Custom API Key:"), Location = new Point(0, 0), AutoSize = true };
            _txtApiKey = new TextBox { Location = new Point(0, 20), Size = new Size(465, 23) };

            var lblCId = new Label { Text = "Custom Client ID:", Location = new Point(0, 48), AutoSize = true };
            _txtClientId = new TextBox { Location = new Point(0, 68), Size = new Size(465, 23) };

            var lblCSec = new Label { Text = "Custom Client Secret:", Location = new Point(0, 96), AutoSize = true };
            _txtClientSecret = new TextBox { Location = new Point(0, 116), Size = new Size(465, 23) };

            _pnlAdvancedApi.Controls.Add(lblApi);
            _pnlAdvancedApi.Controls.Add(_txtApiKey);
            _pnlAdvancedApi.Controls.Add(lblCId);
            _pnlAdvancedApi.Controls.Add(_txtClientId);
            _pnlAdvancedApi.Controls.Add(lblCSec);
            _pnlAdvancedApi.Controls.Add(_txtClientSecret);

            _lnkToggleAdvanced.LinkClicked += (s, e) =>
            {
                bool show = !_pnlAdvancedApi.Visible;
                _pnlAdvancedApi.Visible = show;
                if (show)
                {
                    _pnlAdvancedApi.Size = new Size(470, 145);
                    grpAccount.Size = new Size(500, 275);
                }
                else
                {
                    _pnlAdvancedApi.Size = new Size(470, 0);
                    grpAccount.Size = new Size(500, 125);
                }
                RepositionPanels();
            };

            grpAccount.Controls.Add(_lblAccountStatus);
            grpAccount.Controls.Add(_btnConnectGoogle);
            grpAccount.Controls.Add(_btnDisconnectGoogle);
            grpAccount.Controls.Add(_lnkToggleAdvanced);
            grpAccount.Controls.Add(_pnlAdvancedApi);

            // ================= 2. yt-dlp & Cookies Section =================
            var grpYtDlp = new GroupBox
            {
                Text = Localization.Get(@"YouTube.Options\YtDlpTitle", "Ekstraktor Stream yt-dlp & Cookies"),
                Location = new Point(10, 145),
                Size = new Size(500, 245)
            };

            var lblCookies = new Label
            {
                Text = Localization.Get(@"YouTube.Options\BrowserCookies", "Impor Cookies YouTube dari Browser:"),
                Location = new Point(15, 25),
                AutoSize = true
            };

            _cboBrowserCookies = new ComboBox
            {
                Location = new Point(15, 48),
                Size = new Size(470, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboBrowserCookies.Items.AddRange(new object[]
            {
                "none",
                "chrome",
                "brave",
                "edge",
                "firefox",
                "opera",
                "vivaldi",
                "chromium"
            });

            var lblPath = new Label
            {
                Text = Localization.Get(@"YouTube.Options\YtDlpPath", "Lokasi berkas yt-dlp.exe:"),
                Location = new Point(15, 80),
                AutoSize = true
            };

            _txtYtDlpPath = new TextBox
            {
                Location = new Point(15, 103),
                Size = new Size(375, 23)
            };

            _btnBrowse = new Button
            {
                Text = Localization.Get(@"YouTube.Options\Browse", "Telusuri..."),
                Location = new Point(398, 101),
                Size = new Size(88, 27)
            };
            _btnBrowse.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*", Title = "Pilih yt-dlp.exe" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        _txtYtDlpPath.Text = ofd.FileName;
                    }
                }
            };

            var lblParams = new Label
            {
                Text = Localization.Get(@"YouTube.Options\AudioParams", "Parameter Format Audio (-f):"),
                Location = new Point(15, 133),
                AutoSize = true
            };

            _txtYtDlpParams = new TextBox
            {
                Location = new Point(15, 155),
                Size = new Size(470, 23),
                Text = "-f bestaudio/best"
            };

            _lblYtDlpStatus = new Label
            {
                Text = Localization.Get(@"YouTube.Options\StatusReady", "yt-dlp terpasang dan siap digunakan."),
                Location = new Point(15, 195),
                Size = new Size(240, 25),
                ForeColor = Color.DarkSlateBlue
            };

            _btnDownloadYtDlp = new Button
            {
                Text = Localization.Get(@"YouTube.Options\DownloadYtDlp", "Unduh yt-dlp"),
                Location = new Point(260, 190),
                Size = new Size(110, 30)
            };
            _btnDownloadYtDlp.Click += async (s, e) =>
            {
                _btnDownloadYtDlp.Enabled = false;
                _lblYtDlpStatus.Text = Localization.Get(@"YouTube.Options\StatusDownloading", "Mengunduh yt-dlp.exe...");
                string target = Path.Combine(Config.ConfigFolder, "yt-dlp.exe");
                bool ok = await Task.Run(() => YtDlpService.DownloadYtDlp(target));
                _lblYtDlpStatus.Text = ok 
                    ? Localization.Get(@"YouTube.Options\StatusDownloadOk", "yt-dlp berhasil diunduh!") 
                    : Localization.Get(@"YouTube.Options\StatusDownloadFail", "Gagal mengunduh yt-dlp.");
                if (ok) _txtYtDlpPath.Text = target;
                _btnDownloadYtDlp.Enabled = true;
            };

            _btnUpdateYtDlp = new Button
            {
                Text = Localization.Get(@"YouTube.Options\UpdateYtDlp", "Perbarui (-U)"),
                Location = new Point(378, 190),
                Size = new Size(108, 30)
            };
            _btnUpdateYtDlp.Click += async (s, e) =>
            {
                _btnUpdateYtDlp.Enabled = false;
                _lblYtDlpStatus.Text = Localization.Get(@"YouTube.Options\StatusUpdating", "Memperbarui yt-dlp.exe...");
                bool ok = await Task.Run(() => YtDlpService.UpdateYtDlp(true));
                _lblYtDlpStatus.Text = ok 
                    ? Localization.Get(@"YouTube.Options\StatusUpdateDone", "Pemeriksaan pembaruan selesai.") 
                    : Localization.Get(@"YouTube.Options\StatusUpdateFail", "Pembaruan gagal.");
                _btnUpdateYtDlp.Enabled = true;
            };

            grpYtDlp.Controls.Add(lblCookies);
            grpYtDlp.Controls.Add(_cboBrowserCookies);
            grpYtDlp.Controls.Add(lblPath);
            grpYtDlp.Controls.Add(_txtYtDlpPath);
            grpYtDlp.Controls.Add(_btnBrowse);
            grpYtDlp.Controls.Add(lblParams);
            grpYtDlp.Controls.Add(_txtYtDlpParams);
            grpYtDlp.Controls.Add(_lblYtDlpStatus);
            grpYtDlp.Controls.Add(_btnDownloadYtDlp);
            grpYtDlp.Controls.Add(_btnUpdateYtDlp);

            // ================= 3. General Section =================
            var grpGeneral = new GroupBox
            {
                Text = Localization.Get(@"YouTube.Options\General", "Pengaturan Umum"),
                Location = new Point(10, 400),
                Size = new Size(500, 80)
            };

            _chkCheckStartup = new CheckBox
            {
                Text = Localization.Get(@"YouTube.Options\CheckAtStartup", "Periksa URL yang dipantau saat AIMP dijalankan"),
                Location = new Point(15, 22),
                AutoSize = true
            };

            _chkCreateNewPlaylist = new CheckBox
            {
                Text = Localization.Get(@"YouTube.Options\CreateNewByDefault", "Buat daftar putar baru secara default saat menambah URL"),
                Location = new Point(15, 48),
                AutoSize = true
            };

            grpGeneral.Controls.Add(_chkCheckStartup);
            grpGeneral.Controls.Add(_chkCreateNewPlaylist);

            this.Controls.Add(grpAccount);
            this.Controls.Add(grpYtDlp);
            this.Controls.Add(grpGeneral);
        }

        private void RepositionPanels()
        {
            if (this.Controls.Count >= 3)
            {
                var grpAccount = this.Controls[0];
                var grpYtDlp = this.Controls[1];
                var grpGeneral = this.Controls[2];

                grpYtDlp.Location = new Point(10, grpAccount.Bottom + 10);
                grpGeneral.Location = new Point(10, grpYtDlp.Bottom + 10);
            }
        }

        private void OnAuthCompleted(bool success, string user)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnAuthCompleted(success, user)));
                return;
            }

            UpdateAccountUI();
        }

        private void UpdateAccountUI()
        {
            if (Config.IsConnected)
            {
                string display = !string.IsNullOrEmpty(Config.UserName) ? Config.UserName : Config.UserYTName;
                _lblAccountStatus.Text = $"Status: Terhubung sebagai {display}";
                _lblAccountStatus.ForeColor = Color.ForestGreen;
                _btnConnectGoogle.Visible = false;
                _btnDisconnectGoogle.Visible = true;
            }
            else
            {
                _lblAccountStatus.Text = Localization.Get(@"YouTube.Options\StatusNotConnected", "Status: Belum Terhubung");
                _lblAccountStatus.ForeColor = Color.DimGray;
                _btnConnectGoogle.Visible = true;
                _btnConnectGoogle.Enabled = true;
                _btnDisconnectGoogle.Visible = false;
            }
        }

        public void LoadSettings()
        {
            _txtApiKey.Text = Config.YouTubeKey;
            _txtClientId.Text = Config.YouTubeClientId;
            _txtClientSecret.Text = Config.YouTubeClientSecret;
            _txtYtDlpPath.Text = YtDlpService.GetYtDlpPath();
            _txtYtDlpParams.Text = Config.YtDlpParams;
            _chkCheckStartup.Checked = Config.CheckOnStartup;
            _chkCreateNewPlaylist.Checked = Config.CreateNewPlaylist;

            int cookieIdx = _cboBrowserCookies.FindStringExact(Config.CookiesBrowser?.ToLower() ?? "none");
            _cboBrowserCookies.SelectedIndex = cookieIdx >= 0 ? cookieIdx : 0;

            string path = YtDlpService.GetYtDlpPath();
            if (File.Exists(path))
            {
                _lblYtDlpStatus.Text = Localization.Get(@"YouTube.Options\StatusReady", "yt-dlp terpasang dan siap digunakan.");
                _lblYtDlpStatus.ForeColor = Color.ForestGreen;
            }
            else
            {
                _lblYtDlpStatus.Text = Localization.Get(@"YouTube.Options\StatusMissing", "yt-dlp.exe belum ditemukan.");
                _lblYtDlpStatus.ForeColor = Color.Crimson;
            }

            UpdateAccountUI();
            RepositionPanels();
        }

        public void SaveSettings()
        {
            Config.YouTubeKey = _txtApiKey.Text.Trim();
            Config.YouTubeClientId = _txtClientId.Text.Trim();
            Config.YouTubeClientSecret = _txtClientSecret.Text.Trim();
            Config.CookiesBrowser = _cboBrowserCookies.SelectedItem?.ToString() ?? "none";
            Config.YtDlpPath = _txtYtDlpPath.Text.Trim();
            Config.YtDlpParams = string.IsNullOrWhiteSpace(_txtYtDlpParams.Text) ? "-f bestaudio/best" : _txtYtDlpParams.Text.Trim();
            Config.CheckOnStartup = _chkCheckStartup.Checked;
            Config.CreateNewPlaylist = _chkCreateNewPlaylist.Checked;
            Config.SaveSettings();
        }
    }
}
