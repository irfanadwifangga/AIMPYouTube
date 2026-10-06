using System;
using System.Runtime.InteropServices;
using AIMP.SDK;
using AIMP.SDK.Options;
using AIMPYouTube.UI;

namespace AIMPYouTube.Extensions
{
    public class YouTubeOptionsFrame : IAimpOptionsDialogFrame, IAimpExtension
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        private OptionsPanel _panel;

        public string GetName()
        {
            return "YouTube Support";
        }

        public IntPtr CreateFrame(IntPtr parentWindow)
        {
            if (_panel == null || _panel.IsDisposed)
            {
                _panel = new OptionsPanel();
            }

            SetParent(_panel.Handle, parentWindow);
            _panel.Visible = true;
            return _panel.Handle;
        }

        public void DestroyFrame()
        {
            if (_panel != null && !_panel.IsDisposed)
            {
                _panel.Dispose();
                _panel = null;
            }
        }

        public void Notification(OptionsDialogFrameNotificationType id)
        {
            if (_panel == null || _panel.IsDisposed)
                return;

            switch (id)
            {
                case OptionsDialogFrameNotificationType.Save:
                    _panel.SaveSettings();
                    break;
                case OptionsDialogFrameNotificationType.Load:
                    _panel.LoadSettings();
                    break;
            }
        }
    }
}
