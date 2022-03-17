namespace SMSH.Data
{
    public class MediaStream
    {
        public string StreamId { get; set; }
        public string StreamType { get; set; }
        public string StreamURL { get; set; }
        public int Stop { get; set; }
        public long CreateDateTime { get; set; }
        public string FFmpegArg { get; set; }
        public string Title { get; set; }
        public int? ProcessId { get; set; }
    }
}