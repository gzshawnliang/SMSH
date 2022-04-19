using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSH
{
    public class Global
    {
        public const string DbFileName = "DataStorage/data.db";
        public const string M3u8FileDir = "MediaContent/HLS";
        public const int M3u8FileExpiredSeconds = 10;               //m3u8文件未过期秒数，超过表示FFmpeg没有在拉流
        public const string FFmpegExe = "FFmpeg/ffmpeg.exe";
    }
}
