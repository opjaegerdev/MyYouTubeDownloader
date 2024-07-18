using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyYoutubeDownloader
{
    public class AppSettings
    {
        public string defaultDownloadFolder { get; set; }
        public bool openDownloadFolderOnFinish { get; set; }
        public bool openDownloadFolderOnFinishAlways { get; set; }
    }
}
