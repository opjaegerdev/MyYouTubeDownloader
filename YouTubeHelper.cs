using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyYoutubeDownloader
{
    public static class YoutubeHelper
    {
        public static string ParseVideoId(string url)
        {
            var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?youtu(?:\.be\/|be\.com\/(?:watch\?(?:.*&)?v=|(?:embed|v|shorts)\/|.*[?&]v=))([^""&?\/\s]{11})", RegexOptions.IgnoreCase);
            var match = regex.Match(url);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
