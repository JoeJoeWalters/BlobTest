using System;

namespace Common
{
    public class Log
    {
        public string Account { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public object Request { get; set; }
        public object Response { get; set; }
    }
}
