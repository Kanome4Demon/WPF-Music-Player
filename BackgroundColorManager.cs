using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFMusicPlayerDemo;

namespace YourNamespace.Services
{
    public class BackgroundColorManager
    {
        public event Action<Color> DominantColorChanged;

        public void UpdateImageSource(BitmapSource bitmap)
        {
            if (bitmap == null) return;
            var dominant = ColorHelper.GetDominantColor(bitmap);
            DominantColorChanged?.Invoke(dominant);
        }
    }
}
