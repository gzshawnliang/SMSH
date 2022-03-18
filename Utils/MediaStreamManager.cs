using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Dapper;
using SMSH.Data;

namespace SMSH.Utils
{
    public class MediaStreamManager
    {
        private readonly IWebHostEnvironment _env;
        public MediaStreamManager(IWebHostEnvironment env)
        {
            _env = env;
        }
        public string M3u8File(string streamId)
        {
            return $"{_env.ContentRootPath}{Global.M3u8FileDir}\\{streamId}\\index.m3u8";
        }

        public bool IsRuning(string streamId)
        {
            string m3u8File = M3u8File(streamId);
            if (File.Exists(m3u8File))
            {
                //进程id存在且m3u8文件未过期
                string processId =string.Empty;
                using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                {
                    processId = connection.QueryFirst<string>($"SELECT ProcessId FROM MediaStream WHERE StreamId='{streamId}';");
                }
                Process p = null;
                if (!string.IsNullOrEmpty(processId))
                {
                    try
                    {
                        p = Process.GetProcessById(int.Parse(processId));
                    }
                    catch
                    {
                        p = null;
                    }
                }
                
                if(p!=null)
                {
                    FileInfo fi = new FileInfo(m3u8File);
                    if ((DateTime.Now - fi.LastWriteTime).TotalSeconds < Global.M3u8FileExpiredSeconds)   //文件未过期 一直在拉流
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public MediaStream GetMediaStream(string streamId)
        {
            MediaStream mediaStream = null;
            using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
            {
                connection.Open();
                mediaStream = connection.QueryFirst<MediaStream>($"SELECT * FROM MediaStream WHERE StreamId='{streamId}';");
                System.Diagnostics.Debug.WriteLine($"{mediaStream.StreamId}");
            }
            return mediaStream;
        }

        public bool Stop(string streamId)
        {
            MediaStream mediaStream = GetMediaStream(streamId);
            if (mediaStream == null)
                return false;
            int processId = mediaStream.ProcessId ?? 0;
            string m3u8Dir = $"{_env.ContentRootPath}{Global.M3u8FileDir}\\{mediaStream.StreamId}";
            string m3u8File = M3u8File(mediaStream.StreamId);

            mediaStream.FFmpegArg = mediaStream.FFmpegArg.Replace("{input}", mediaStream.StreamURL);
            mediaStream.FFmpegArg = mediaStream.FFmpegArg.Replace("{output}", m3u8File);
            var ffmpeg = new FFmpeg();
            if (processId > 0)
            {
                ffmpeg.Stop(processId);
                if (Directory.Exists(m3u8Dir))
                {
                    Directory.Delete(m3u8Dir, true);
                }
                using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                {
                    connection.Execute($"UPDATE MediaStream SET ProcessId = NULL,Stop = 1 WHERE StreamId = '{mediaStream.StreamId}';");
                }
            }
            return true;
        }
        public bool Start(string streamId)
        {
            MediaStream mediaStream = GetMediaStream(streamId);
            if (mediaStream == null)
                return false;

            int processId = mediaStream.ProcessId ?? 0;

            string m3u8Dir = $"{_env.ContentRootPath}{Global.M3u8FileDir}\\{mediaStream.StreamId}";
            string m3u8File = M3u8File(mediaStream.StreamId);

            mediaStream.FFmpegArg = mediaStream.FFmpegArg.Replace("{input}", mediaStream.StreamURL);
            mediaStream.FFmpegArg = mediaStream.FFmpegArg.Replace("{output}", m3u8File);
            var ffmpeg = new FFmpeg();
            if (processId > 0)
            {
                ffmpeg.Stop(processId);
                if (Directory.Exists(m3u8Dir))
                {
                    Directory.Delete(m3u8Dir, true);
                }
            }
            if (!Directory.Exists(m3u8Dir))
            {
                Directory.CreateDirectory(m3u8Dir);
            }
            processId = ffmpeg.Start(mediaStream.FFmpegArg);

            if (processId > 0)
            {
                DateTime beginTime = DateTime.Now;
                while (!System.IO.File.Exists(m3u8File))
                {
                    Thread.Sleep(100);
                    if (DateTime.Now - beginTime > TimeSpan.FromSeconds(10))        //超时启动失败
                    {
                        using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                            connection.Execute($"UPDATE MediaStream SET ProcessId = NULL,Stop = 1 WHERE StreamId = '{mediaStream.StreamId}';");

                        //启动失败关闭进程
                        ffmpeg.Stop(processId);

                        return false;
                    }

                    continue;
                }
                using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                    connection.Execute($"UPDATE MediaStream SET ProcessId ={processId} WHERE StreamId = '{mediaStream.StreamId}';");

                return true;
            }
            return false;

        }
    }
}
