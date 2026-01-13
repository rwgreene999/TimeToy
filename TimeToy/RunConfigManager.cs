using Newtonsoft.Json; // Install-Package Newtonsoft.Json
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TimeToy
{
    public class WindowSettings
    {
        public double Top { get; set; } = double.NaN;
        public double Left { get; set; } = double.NaN;
        public double Width { get; set; } = double.NaN;
        public double Height { get; set; } = double.NaN;
        public string IsMaximized { get; set; } = System.Windows.WindowState.Normal.ToString();
    }


    public enum AlarmRepeatMode { None, Daily, Weekdays, Weekly }
    public class AlarmClockEntry
    {
        public bool Active { get; set; } = false; 
        public string Title { get; set; } = String.Empty;
        public DateTime Alarm { get; set; } = DateTime.MinValue;
        public TimerNotificationOptions Notification { get; set; } = TimerNotificationOptions.Voice;
        public string Comment { get; set; } = String.Empty;
        public string Voice { get; set; } = String.Empty;
        public string Filename { get; set; } = String.Empty;
        public AlarmRepeatMode RepeatMode { get; set; } = AlarmRepeatMode.None;
        public WindowSettings windowSettings { get; set; } = new WindowSettings();

    }

    public enum TimerNotificationOptions { None, Sound, Voice };

    // configuration for complete application
    public class RunConfigManager
    {

        public class TimerSettings
        {
            public TimerNotificationOptions Notification { get; set; } = TimerNotificationOptions.Voice;
            public string Comment { get; set; } = "Timer Is Up";
            public string Voice { get; set; } = String.Empty;
            public string Filename { get; set; } = String.Empty;
            public WindowSettings windowSettings { get; set; } = new WindowSettings();
        }
        public class StopWatchSettings
        {
            public string Voice { get; set; } = String.Empty;
            public double Volume { get; set; } = 100.0;
            public WindowSettings windowSettings { get; set; } = new WindowSettings();

        }
        public class OptionsSettings
        {
            public WindowSettings windowSettings { get; set; } = new WindowSettings();

        }

        public AlarmClockEntry[] AlarmClocks { get; set; } = new AlarmClockEntry[]
        {
            new AlarmClockEntry(),
            new AlarmClockEntry(),
            new AlarmClockEntry()
        };

        public class RunConfig
        {
            public string Theme { get; set; } = "Dark";
            public StopWatchSettings StopWatcherOptions { get; set; } = new StopWatchSettings();
            public TimerSettings TimerOptions { get; set; } = new TimerSettings();
            public WindowSettings windowSettings { get; set; } = new WindowSettings();
            public OptionsSettings optionsSettings { get; set; } = new OptionsSettings();

        }


        public RunConfig runConfig { get; set; } = new RunConfig();


        // delay save to debounce multiple changes
        private readonly DispatcherTimer _saveDebounce;
        private static string ConfigFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TimeToy.json");
        // intra-process lock to prevent thread-level races
        private static readonly object s_saveLock = new object();

        public RunConfigManager()
        {
            _saveDebounce = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _saveDebounce.Tick += (s, e) =>
            {
                _saveDebounce.Stop();
                SaveNow();
            };
        }



        public void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    JsonConvert.PopulateObject(json, this);
                }
                else
                {
                    var config = new RunConfig();
                    SaveNow();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WIP Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveNow()
        {
            try
            {

                lock (s_saveLock)
                {
                    var json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);

                    // Write to a temp file next to the target and then replace atomically
                    var tempPath = ConfigFilePath + ".tmp";

                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath) ?? AppDomain.CurrentDomain.BaseDirectory);

                    File.WriteAllText(tempPath, json);

                    if (File.Exists(ConfigFilePath))
                    {
                        // Replace the existing file atomically. The third parameter (backup) is null.
                        File.Replace(tempPath, ConfigFilePath, null);
                    }
                    else
                    {
                        // Move temp into place
                        File.Move(tempPath, ConfigFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void Save()
        {
            _saveDebounce.Start();

        }
    }
}


