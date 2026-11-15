using System;

namespace LaptopInputControl.Models
{
    public class EventLogItem
    {
        public string TimeStamp { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string DeviceOrigin { get; set; }
        public bool IsBlocked { get; set; }

        public string ActionText
        {
            get { return IsBlocked ? "BLOCKED" : "ALLOWED"; }
        }

        public string ActionColor
        {
            get { return IsBlocked ? "#FF5555" : "#50FA7B"; }
        }

        public EventLogItem(string source, string target, bool isBlocked, string deviceOrigin = "Global")
        {
            TimeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Source = source;
            Target = target;
            IsBlocked = isBlocked;
            DeviceOrigin = deviceOrigin;
        }
    }
}
