using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using AIMP.SDK;
using AIMP.SDK.AlbumArt;
using AIMP.SDK.AlbumArt.Extensions;
using AIMP.SDK.FileManager.Objects;
using AIMPYouTube.Core;

namespace AIMPYouTube.Extensions
{
    public class YouTubeAlbumArtProvider : IAimpExtensionAlbumArtProvider2, IAimpExtension
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public AimpActionResult<Bitmap> Get(IAimpFileInfo fileInfo, IAimpAlbumArtSearchOptions options)
        {
            if (fileInfo == null)
                return new AimpActionResult<Bitmap>(ActionResultType.Fail);

            try
            {
                string fileName = fileInfo.FileName;
                var trackInfo = Tools.GetTrackInfoFromFileName(fileName);
                if (trackInfo != null && !string.IsNullOrEmpty(trackInfo.Artwork))
                {
                    byte[] data = _httpClient.GetByteArrayAsync(trackInfo.Artwork).GetAwaiter().GetResult();
                    if (data != null && data.Length > 0)
                    {
                        using (var ms = new MemoryStream(data))
                        {
                            var bmp = new Bitmap(ms);
                            return new AimpActionResult<Bitmap>(ActionResultType.OK, bmp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIMPYouTube] Album art download error: {ex}");
            }

            return new AimpActionResult<Bitmap>(ActionResultType.Fail);
        }
    }
}
