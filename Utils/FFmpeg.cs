using System.Diagnostics;

namespace SMSH.Utils
{
    public class FFmpeg
    {
        //private const string FFMPEG_EXE = "FFmpeg/ffmpeg.exe";

        private void Initialize()
        {
            if (!File.Exists(Global.FFmpegExe))
                throw new ApplicationException("Could not find a copy of ffmpeg.exe");
        }
        public void Stop(int processId)
        {
            Process p = null;
            if (processId != 0)
            {
                try
                {
                    p = Process.GetProcessById(processId);
                }
                catch
                {
                    p = null;
                }
            }
            p?.Kill(true);
        }
        public int Start(string argument)
        {
            //ffmpeg.exe -i rtsp://200.200.200.140/test1 -fflags flush_packets -max_delay 2 -hls_flags delete_segments -hls_time 2 -g 30 test-1.m3u8
            Process p = null;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Global.FFmpegExe,
                    Arguments = argument,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "RunAs"  //以管理员身份运行
                };

                p = Process.Start(startInfo);

                return p != null ? p.Id : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"restart conversion error,{ex.Message}");
                p?.Close();
                return 0;
            }
        }
    }
}
