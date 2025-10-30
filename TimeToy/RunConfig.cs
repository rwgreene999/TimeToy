using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeToy
{
    // configuration for complete application
    public enum TimerNotificationOptions { None, Sound, Voice};
    public class TimerSettings
    {
        public TimerNotificationOptions Notification{ get; set; } = TimerNotificationOptions.Voice;
        public string Comment { get; set; } = "Timer Is Up";
        public string Voice { get; set; } = String.Empty;
        public string Filename { get; set; } = String.Empty;
    }
    public class StopWatchSettings
    {
        public string Voice { get; set; } = String.Empty;
        public double Volume { get; set; } = 100.0;
        
    }
    public class RunConfig
    {
        public string Theme { get; set; } = "Dark"; 
        public StopWatchSettings StopWatcherOptions { get; set; } = new StopWatchSettings();
        public TimerSettings TimerOptions { get; set; } = new TimerSettings();

    }
}
