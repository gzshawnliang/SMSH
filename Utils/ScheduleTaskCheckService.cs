using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SMSH.Utils
{
    //在 ASP.NET Core 中使用托管服务实现后台任务
    //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0&tabs=visual-studio
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

            new MediaStreamManager(_env).StartAll();
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
