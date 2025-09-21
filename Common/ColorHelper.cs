using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WinColor = System.Windows.Media.Color;
using WColors = System.Windows.Media.Colors;
using CT = ColorThief.ImageSharp.ColorThief;

namespace WPFMusicPlayerDemo
{
    public static class ColorHelper
    {
        public static WinColor GetDominantColor(BitmapSource bitmap)
        {
            if (bitmap == null) return WColors.Gray; // 注意这里使用 WPF Colors.Gray

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                using (var image = Image.Load<Rgba32>(ms))
                {
                    var colorThief = new CT();
                    var dominant = colorThief.GetColor(image);
                    return WinColor.FromRgb(dominant.Color.R, dominant.Color.G, dominant.Color.B);
                }
            }
        }
    }
}
