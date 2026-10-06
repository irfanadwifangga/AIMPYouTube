using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIMP.SDK;
using AIMP.SDK.Actions.Objects;
using AIMP.SDK.MenuManager;
using AIMP.SDK.MenuManager.Objects;
using AIMP.SDK.MessageDispatcher;
using AIMP.SDK.Playlist.Objects;
using AIMPYouTube.Configuration;
using AIMPYouTube.Core;
using AIMPYouTube.Extensions;
using AIMPYouTube.Services;
using AIMPYouTube.UI;

namespace AIMPYouTube
{
    [AimpPlugin("AIMPYouTube", "Irfana D. F / Updated", "2.0.0", AimpPluginType = AimpPluginType.Addons, Description = "YouTube Support for AIMP (powered by .NET & yt-dlp)")]
    public class YouTubePlugin : AimpPlugin
    {
        private IAimpMenuItem _addUrlMenuItem;
        private IAimpMenuItem _openBrowserMenuItem;
        private IAimpMenuItem _excludeTrackMenuItem;

        public override void Initialize()
        {
            try
            {
                Localization.Initialize(Player);

                string profilePath = "";
                try
                {
                    profilePath = Player.Core.GetPath(AimpCorePathType.Profile);
                }
                catch { }

                Config.Initialize(profilePath);

                // Register Extensions
                Player.Core.RegisterExtension(new YouTubePlayerHook());
                Player.Core.RegisterExtension(new YouTubeFileInfoProvider(Player));
                Player.Core.RegisterExtension(new YouTubeAlbumArtProvider());
                Player.Core.RegisterExtension(new YouTubePlaylistListener());
                Player.Core.RegisterExtension(new YouTubeOptionsFrame());

                // Register Menu Items
                RegisterMenuItems();

                // Startup yt-dlp check in background
                Task.Run(() =>
                {
                    string ytDlp = YtDlpService.GetYtDlpPath();
                    if (!System.IO.File.Exists(ytDlp))
                    {
                        YtDlpService.DownloadYtDlp(ytDlp);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Initialization failed: {ex}");
            }
        }

        private void RegisterMenuItems()
        {
            try
            {
                string addUrlText = Localization.Get(@"YouTube.Menu\AddURL", "YouTube URL...");
                string openBrowserText = Localization.Get(@"YouTube.Menu\OpenInBrowser", "YouTube: Open in Browser");
                string excludeText = Localization.Get(@"YouTube.Menu\AddToExclusions", "YouTube: Add to Exclusions");

                // 1. "YouTube URL..." Action
                var addUrlActionRes = Player.Core.CreateObject(AimpObjectType.AimpAction);
                if (addUrlActionRes.ResultType == ActionResultType.OK && addUrlActionRes.Result is IAimpAction action)
                {
                    action.Id = "AIMPYouTube.Action.AddUrl";
                    action.Name = addUrlText;
                    action.OnExecute += (s, e) =>
                    {
                        using (var form = new AddUrlForm(Player))
                        {
                            form.ShowDialog();
                        }
                    };

                    // Add to Playlist Right-Click > "+ Add" submenu (PlayerPlaylistContextAdding = 30)
                    var ctxAddItemRes = Player.Core.CreateObject(AimpObjectType.AimpMenuItem);
                    if (ctxAddItemRes.ResultType == ActionResultType.OK && ctxAddItemRes.Result is IAimpMenuItem ctxAddItem)
                    {
                        ctxAddItem.Id = "AIMPYouTube.MenuItem.ContextAddUrl";
                        ctxAddItem.Name = addUrlText;
                        ctxAddItem.Action = action;
                        Player.ServiceMenuManager.Add(ParentMenuType.PlayerPlaylistContextAdding, ctxAddItem);
                    }

                    // Add to Playlist Bottom Bar "+" button (PlayerPlaylistAdding = 20)
                    var plAddItemRes = Player.Core.CreateObject(AimpObjectType.AimpMenuItem);
                    if (plAddItemRes.ResultType == ActionResultType.OK && plAddItemRes.Result is IAimpMenuItem plAddItem)
                    {
                        _addUrlMenuItem = plAddItem;
                        _addUrlMenuItem.Id = "AIMPYouTube.MenuItem.AddUrl";
                        _addUrlMenuItem.Name = addUrlText;
                        _addUrlMenuItem.Action = action;
                        Player.ServiceMenuManager.Add(ParentMenuType.PlayerPlaylistAdding, _addUrlMenuItem);
                    }

                    // Add directly into Playlist Right-Click Menu (PlayerPlaylistContextFunctions = 32)
                    var directCtxItemRes = Player.Core.CreateObject(AimpObjectType.AimpMenuItem);
                    if (directCtxItemRes.ResultType == ActionResultType.OK && directCtxItemRes.Result is IAimpMenuItem directCtxItem)
                    {
                        directCtxItem.Id = "AIMPYouTube.MenuItem.DirectAddUrl";
                        directCtxItem.Name = "YouTube: " + addUrlText;
                        directCtxItem.Action = action;
                        Player.ServiceMenuManager.Add(ParentMenuType.PlayerPlaylistContextFunctions, directCtxItem);
                    }

                    // Add to Main Menu > Open (PlayerMainOpen = 11)
                    var mainOpenItemRes = Player.Core.CreateObject(AimpObjectType.AimpMenuItem);
                    if (mainOpenItemRes.ResultType == ActionResultType.OK && mainOpenItemRes.Result is IAimpMenuItem mainOpenItem)
                    {
                        mainOpenItem.Id = "AIMPYouTube.MenuItem.MainOpenAddUrl";
                        mainOpenItem.Name = addUrlText;
                        mainOpenItem.Action = action;
                        Player.ServiceMenuManager.Add(ParentMenuType.PlayerMainOpen, mainOpenItem);
                    }
                }

                // 2. "Open in Browser" in Playlist Context menu
                var openBrowserActionRes = Player.Core.CreateObject(AimpObjectType.AimpAction);
                var openBrowserItemRes = Player.Core.CreateObject(AimpObjectType.AimpMenuItem);

                if (openBrowserActionRes.ResultType == ActionResultType.OK && openBrowserItemRes.ResultType == ActionResultType.OK)
                {
                    if (openBrowserActionRes.Result is IAimpAction obAction && openBrowserItemRes.Result is IAimpMenuItem menuItem)
                    {
                        obAction.Id = "AIMPYouTube.Action.OpenBrowser";
                        obAction.Name = openBrowserText;
                        obAction.OnExecute += (s, e) => OnOpenInBrowserClicked();

                        _openBrowserMenuItem = menuItem;
                        _openBrowserMenuItem.Id = "AIMPYouTube.MenuItem.OpenBrowser";
                        _openBrowserMenuItem.Name = openBrowserText;
                        _openBrowserMenuItem.Action = obAction;

                        Player.ServiceMenuManager.Add(ParentMenuType.PlayerPlaylistContextFunctions, _openBrowserMenuItem);
                    }
                }

                // 3. "Add to Exclusions" in Playlist Context menu
                var excludeActionRes = Player.Core.CreateObject(AimpObjectType.AimpAction);
                var excludeItemRes = Player.Core.CreateObject(AimpObjectType.AimpMenuItem);

                if (excludeActionRes.ResultType == ActionResultType.OK && excludeItemRes.ResultType == ActionResultType.OK)
                {
                    if (excludeActionRes.Result is IAimpAction exAction && excludeItemRes.Result is IAimpMenuItem menuItem)
                    {
                        exAction.Id = "AIMPYouTube.Action.Exclude";
                        exAction.Name = excludeText;
                        exAction.OnExecute += (s, e) => OnAddToExclusionsClicked();

                        _excludeTrackMenuItem = menuItem;
                        _excludeTrackMenuItem.Id = "AIMPYouTube.MenuItem.Exclude";
                        _excludeTrackMenuItem.Name = excludeText;
                        _excludeTrackMenuItem.Action = exAction;

                        Player.ServiceMenuManager.Add(ParentMenuType.PlayerPlaylistContextFunctions, _excludeTrackMenuItem);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Menu registration error: {ex}");
            }
        }

        private void OnOpenInBrowserClicked()
        {
            try
            {
                var activePlRes = Player.ServicePlaylistManager.GetActivePlaylist();
                if (activePlRes.ResultType == ActionResultType.OK && activePlRes.Result != null)
                {
                    var pl = activePlRes.Result;
                    var focused = pl.FocusedItem;
                    if (focused != null && !string.IsNullOrEmpty(focused.FileName))
                    {
                        string id = Tools.TrackIdFromUrl(focused.FileName);
                        if (!string.IsNullOrEmpty(id))
                        {
                            Process.Start($"https://www.youtube.com/watch?v={id}");
                            return;
                        }
                    }
                }
                MessageBox.Show("Menu ini khusus untuk trek lagu YouTube. Silakan klik pada trek YouTube di playlist.", "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Open in browser error: {ex}");
            }
        }

        private void OnAddToExclusionsClicked()
        {
            try
            {
                var activePlRes = Player.ServicePlaylistManager.GetActivePlaylist();
                if (activePlRes.ResultType == ActionResultType.OK && activePlRes.Result != null)
                {
                    var pl = activePlRes.Result;
                    var focused = pl.FocusedItem;
                    if (focused != null && !string.IsNullOrEmpty(focused.FileName))
                    {
                        string id = Tools.TrackIdFromUrl(focused.FileName);
                        if (!string.IsNullOrEmpty(id))
                        {
                            Config.TrackExclusions.Add(id);
                            Config.SaveExtendedConfig();
                            pl.Delete(focused);
                            return;
                        }
                    }
                }
                MessageBox.Show("Pilih trek lagu YouTube yang ingin dikecualikan dari playlist.", "AIMP YouTube", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Add to exclusions error: {ex}");
            }
        }

        public override void Dispose()
        {
            try
            {
                Config.SaveSettings();
                Config.SaveExtendedConfig();
                Config.SaveCache();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Dispose error: {ex}");
            }
        }
    }
}
