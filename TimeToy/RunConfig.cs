using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeToy
{
    // configuration for complete application
    public enum NotificationOptions { None, Sound, Voice};
    public class TimerSettings
    {
        private NotificationOptions NotificationOption { get; set; } = NotificationOptions.Voice;
        public string Comment { get; set; } = "Timer Is Up";
        public string Voice { get; set; } = String.Empty;
        public string Filename { get; set; } = String.Empty;
    }
    public class StopWatchSettings
    {
        public string Voice { get; set; } = String.Empty;
        // WIP there will be more when I start saving window locations 
    }
    public class RunConfig
    {
        public string Theme { get; set; } = "Light"; // WIP future use 
        public StopWatchSettings StopWatcherOptions { get; set; } = new StopWatchSettings();
        public TimerSettings TimerOptions { get; set; } = new TimerSettings();

    }
}
