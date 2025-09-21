
using System.Threading.Tasks;

namespace WPFMusicPlayerDemo.Comon;

    public static class TimeSpanExtensions
    {
        public static string ToAutoString(this TimeSpan ts)
        {
            return ts.TotalHours >= 1
                ? ts.ToString(@"hh\:mm\:ss")
                : ts.ToString(@"mm\:ss");
        }
    }



