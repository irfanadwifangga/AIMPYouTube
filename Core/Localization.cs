using System;
using System.Collections.Generic;
using AIMP.SDK;

namespace AIMPYouTube.Core
{
    public static class Localization
    {
        private static IAimpPlayer _player;

        public static void Initialize(IAimpPlayer player)
        {
            _player = player;
        }

        public static string Get(string key, string fallback = "")
        {
            if (_player?.ServiceMui != null)
            {
                try
                {
                    string val = _player.ServiceMui.GetValue(key);
                    if (!string.IsNullOrEmpty(val))
                        return val;
                }
                catch { }
            }

            return !string.IsNullOrEmpty(fallback) ? fallback : key;
        }

        public static string GetPart(string key, int index, string fallback = "")
        {
            if (_player?.ServiceMui != null)
            {
                try
                {
                    string val = _player.ServiceMui.GetValuePart(key, index);
                    if (!string.IsNullOrEmpty(val))
                        return val;
                }
                catch { }
            }

            return fallback;
        }
    }
}
