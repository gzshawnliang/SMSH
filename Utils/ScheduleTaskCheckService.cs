using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.Data.Sqlite;
using Dapper;
namespace SMSH.Utils
{
    public class ScheduleTaskCheckService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<ScheduleTaskCheckService> _logger;
        private Timer _timer = null!;
        private readonly IWebHostEnvironment _env;

        public ScheduleTaskCheckService(ILogger<ScheduleTaskCheckService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            StopAllVideoStream();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(CheckAndStartVideoStream, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        private void StopAllVideoStream()
        {
            Process[] ffmpegProcess = Process.GetProcessesByName("ffmpeg");
            foreach (Process process in ffmpegProcess)
                process?.Kill(true);
        }

        private void CheckAndStartVideoStream(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "CheckAndStartVideoStream. Count: {Count}", count);

            List<dynamic> videoStreamList;
            string sql = $"SELECT StreamId,Stop FROM MediaStream WHERE StreamURL IS NOT NULL AND FFmpegArg IS NOT NULL;";
            using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                videoStreamList = connection.Query<dynamic>(sql).ToList();

            foreach (var videoStream in videoStreamList)
            {
                MediaStreamManager mediaStreamManager = new MediaStreamManager(_env);
                if (videoStream.Stop==0 && !mediaStreamManager.IsRuning(videoStream.StreamId) )
                {
                    mediaStreamManager.Start(videoStream.StreamId);
                }
                else if (videoStream.Stop == 1)
                {
                    mediaStreamManager.Stop(videoStream.StreamId);
                }
            }

        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
