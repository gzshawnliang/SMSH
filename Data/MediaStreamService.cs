using Microsoft.Data.Sqlite;
using Dapper;
using SMSH.Utils;

namespace SMSH.Data
{
    public class MediaStreamService
    {
        private readonly ILogger<MediaStreamService> _logger;
        private readonly IWebHostEnvironment _env;

        public MediaStreamService(ILogger<MediaStreamService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public Task<List<Tuple<string, string>>> GetAvailableAsync()
        {
            List<dynamic> videoStreamList;
            //string sql = $"SELECT StreamId,Title FROM MediaStream WHERE Stop=0 AND StreamURL IS NOT NULL AND FFmpegArg IS NOT NULL AND ProcessId > 0;";
            string sql = $"SELECT StreamId,Title FROM MediaStream WHERE Stop=0 AND StreamURL IS NOT NULL AND FFmpegArg IS NOT NULL;";
            using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                videoStreamList = connection.Query<dynamic>(sql).ToList();

            var VideoStreamList = new List<Tuple<string, string>>();
            foreach (var videoStream in videoStreamList)
            {
                MediaStreamManager mediaStreamManager = new MediaStreamManager(_env);
                if (mediaStreamManager.IsRuning(videoStream.StreamId) || mediaStreamManager.Start(videoStream.StreamId) == true)
                {
                    VideoStreamList.Add(new Tuple<string, string>(videoStream.StreamId, videoStream.Title));
                }
            }
            return Task.FromResult(VideoStreamList);
        }

        public Task<MediaStream> GetMediaStreamAsync(string streamId)
        {
            MediaStream currMediaStream;
            System.Diagnostics.Debug.WriteLine(streamId);
            if (!string.IsNullOrEmpty(streamId))
            {
                using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                    currMediaStream = connection.QueryFirst<MediaStream>($"SELECT * FROM MediaStream WHERE StreamId='{streamId}';");
            }
            else
            {
                currMediaStream = new MediaStream();
            }
            return Task.FromResult(currMediaStream);
        }

        public Task<ApiResult> DeleteAsync(MediaStream currMediaStream)
        {
            string currMediaStreamId = currMediaStream.StreamId;
            //stop before delete
            MediaStreamManager msm = new MediaStreamManager(_env);
            msm.Stop(currMediaStreamId);

            int result = 0;
            using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                result = connection.Execute($"DELETE FROM MediaStream WHERE StreamId = '{currMediaStreamId}';");

            if (result > 0)
            {
                return Task.FromResult(new ApiResult
                {
                    code = 0,
                    message = "删除成功！"
                });
            }
            else
            {
                return Task.FromResult(new ApiResult
                {
                    code = 100,
                    message = $"找不到视频流：{currMediaStreamId}"
                });
            }

        }

        public Task<ApiResult> SaveAsync(MediaStream currMediaStream)
        {
            System.Diagnostics.Debug.WriteLine(currMediaStream.StreamURL);

            string sql = string.Empty;
            string currStreamId = currMediaStream.StreamId;
            if (string.IsNullOrEmpty(currStreamId))
            {
                currStreamId = Guid.NewGuid().ToString().Split("-").First();
                sql += $"INSERT INTO    MediaStream \n";
                sql += $"               (\n";
                sql += $"               StreamId, \n";
                sql += $"               StreamType, \n";
                sql += $"               StreamURL, \n";
                sql += $"               Stop, \n";
                sql += $"               CreateDateTime, \n";
                sql += $"               FFmpegArg, \n";
                sql += $"               Title, \n";
                sql += $"               ProcessId \n";
                sql += $"               )\n";
                sql += $"VALUES         (\n";
                sql += $"               '{currStreamId}', \n";
                sql += $"               'RTPS', \n";
                sql += $"               '{currMediaStream.StreamURL}',\n";
                sql += $"               {currMediaStream.Stop},\n";
                sql += $"               {DateTime.Now.ToString("yyyyMMddHHmmss")},\n";
                sql += $"               '{currMediaStream.FFmpegArg}',\n";
                sql += $"               '{currMediaStream.Title}',\n";
                sql += $"               null\n";
                sql += $"               );";
            }
            else
            {
                sql += $"UPDATE MediaStream \n";
                sql += $"SET";
                sql += $"       StreamURL='{currMediaStream.StreamURL}',\n";
                sql += $"       Stop={currMediaStream.Stop},\n";
                sql += $"       FFmpegArg='{currMediaStream.FFmpegArg}',\n";
                sql += $"       Title='{currMediaStream.Title}',\n";
                sql += $"       CreateDateTime={DateTime.Now.ToString("yyyyMMddHHmmss")}\n";
                sql += $"WHERE  StreamId = '{currStreamId}';";
            }
            int result = 0;
            try
            {
                using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                    result = connection.Execute(sql);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResult
                {
                    code = 2000,
                    message = ex.ToString(),
                });
            }

            if (result > 0)
            {
                MediaStreamManager mediaStreamManager = new MediaStreamManager(_env);
                if (currMediaStream.Stop == 0)
                {
                    mediaStreamManager.Start(currStreamId);
                }
                else
                {
                    mediaStreamManager.Stop(currStreamId);
                }

                return Task.FromResult(new ApiResult
                {
                    code = 0,
                    message = "Ok"
                });
            }

            return Task.FromResult(new ApiResult
            {
                code = 1000,
                message = "error"
            });
        }

        public Task<List<MediaStream>> AllAsync()
        {
            List<MediaStream> VideoStreamList = new List<MediaStream>();
            using (var connection = new SqliteConnection($"Data Source={Global.DbFileName}"))
                VideoStreamList = connection.Query<MediaStream>($"SELECT * FROM MediaStream;").ToList();
            return Task.FromResult(VideoStreamList);
        }
    }
}