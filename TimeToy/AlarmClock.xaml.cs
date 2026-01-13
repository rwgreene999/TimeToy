using System;
using System.Dynamic;
using System.Linq;
using System.Media;
using System.Runtime.ExceptionServices;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TimeToy
{
    public partial class AlarmClock : Window
    {
        private readonly DispatcherTimer _checkTimer;
        private DateTime _nextAlarm;
        private bool _isRunning;
        private AlarmRepeatMode _repeatMode = AlarmRepeatMode.None;
        private DayOfWeek _weeklyDay = DayOfWeek.Monday;
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private SpeechSynthesizer _synth;
        private double _normalWidth, _normalHeight;
        private ResizeMode _normalResizeMode;
        RunConfigManager _configManager; 

        

        private int _alarmSelected = 0; // right not just using 1 alarm WIP find and pick alarm 



        public AlarmClock(RunConfigManager config)
        {
            InitializeComponent();
            _configManager = config;

            // Populate time combos
            for (int h = 1; h <= 12; h++) HourCombo.Items.Add(h.ToString("D2"));
            for (int m = 0; m < 60; m++) MinuteCombo.Items.Add(m.ToString("D2"));
            AmPmCombo.Items.Add("AM");
            AmPmCombo.Items.Add("PM");
            HourCombo.SelectedIndex = 7; // 08
            MinuteCombo.SelectedIndex = 0;
            AmPmCombo.SelectedIndex = 0;

            // Weekly day
            foreach (var d in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>())
                WeeklyDayCombo.Items.Add(d.ToString());
            WeeklyDayCombo.SelectedIndex = 0;

            WindowSettingsManager.ApplyToWindow(this, _configManager.runConfig.TimerOptions.windowSettings);
            // Subscribe to move/size/state events
            LocationChanged += (s, e) => { CaptureWindowsLocation(); };
            SizeChanged += (s, e) => { CaptureWindowsLocation(); };
            StateChanged += (s, e) => { CaptureWindowsLocation(); };
            Closing += (s, e) => { CaptureWindowsLocation(); };

            LoadWindowFromConfig(_alarmSelected, _configManager);
            _mediaPlayer.MediaFailed += (s, e) =>
            {
                MessageBox.Show($"Media error: {e.ErrorException?.Message}");
            };


            // store normal size for expand after trigger
            Loaded += (s, e) =>
            {
                LoadWindowFromConfig(_alarmSelected, _configManager);
            };
        }

        private void CaptureWindowsLocation()
        {
            WindowSettingsManager.CaptureFromWindow(this, _configManager.runConfig.TimerOptions.windowSettings);
            _configManager.Save();
        }


        void LoadWindowFromConfig(int idx, RunConfigManager config)
        {

            if (config.AlarmClocks[idx].Title == String.Empty )
            {
                SetInitialConfiguration(idx, config);
            }

            TitleTextBox.Text = config.AlarmClocks[idx].Title; 

            // Voices WIP this will go away 
            using (var s = new SpeechSynthesizer())
            {
                var voices = s.GetInstalledVoices().Select(v => v.VoiceInfo.Name).ToList();
                if (voices.Any())
                {
                    VoiceCombo.ItemsSource = voices;
                    VoiceCombo.SelectedValue = config.AlarmClocks[idx].Voice;
                }
            }

            // DatePicker default
            DatePicker.SelectedDate = DateTime.Now.Date;
            HourCombo.Text = config.AlarmClocks[idx].Alarm.ToString("hh");
            MinuteCombo.Text = config.AlarmClocks[idx].Alarm.ToString("mm");
            AmPmCombo.Text = config.AlarmClocks[idx].Alarm.ToString("tt");


            //// Timer ticks each second to evaluate alarm
            //_checkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            //_checkTimer.Tick += CheckTimer_Tick;

            // Repeat options
            var entry = config.AlarmClocks[idx];
            switch (entry.RepeatMode)
            {
                case AlarmRepeatMode.Daily:
                    RepeatDaily.IsChecked = true;
                    _repeatMode = AlarmRepeatMode.Daily;
                    break;
                case AlarmRepeatMode.Weekdays:
                    RepeatWeekdays.IsChecked = true;
                    _repeatMode = AlarmRepeatMode.Weekdays;
                    break;
                case AlarmRepeatMode.Weekly:
                    RepeatWeekly.IsChecked = true;
                    _repeatMode = AlarmRepeatMode.Weekly;
                    // If the saved Alarm has a sensible DayOfWeek, select it in the combo
                    if (entry.Alarm != DateTime.MinValue)
                    {
                        var dayName = entry.Alarm.DayOfWeek.ToString();
                        if (WeeklyDayCombo.Items.Contains(dayName))
                        {
                            WeeklyDayCombo.SelectedItem = dayName;
                            Enum.TryParse(dayName, out _weeklyDay);
                        }
                    }
                    break;
                default:
                    RepeatNone.IsChecked = true;
                    _repeatMode = AlarmRepeatMode.None;
                    break;
            }


            switch (entry.Notification)
            {
                case TimerNotificationOptions.Voice:
                    VoiceRadio.IsChecked = true;
                    MusicPanel.Visibility = Visibility.Collapsed;
                    VoicePanel.Visibility = Visibility.Visible;
                    break;
                case TimerNotificationOptions.Sound:
                    MusicRadio.IsChecked = true;
                    VoicePanel.Visibility = Visibility.Collapsed;
                    MusicPanel.Visibility = Visibility.Visible;
                    break;
                default:
                    VoiceRadio.IsChecked = true;
                    break; 
            }
            VoiceTextBox.Text = "Alarm Active";            
        }
        void SetInitialConfiguration(int idx, RunConfigManager config)
        {
            config.AlarmClocks[idx].Title = "Alarm " + (idx + 1).ToString();
            config.AlarmClocks[idx].Alarm = DateTime.Now.AddMinutes(-1);
            config.AlarmClocks[idx].Notification = TimerNotificationOptions.Voice;
            config.AlarmClocks[idx].Comment = "Alarm Time Is Up";
            using (var s = new SpeechSynthesizer())
            {
                var voices = s.GetInstalledVoices().Select(v => v.VoiceInfo.Name).ToList();
                config.AlarmClocks[idx].Voice = voices.FirstOrDefault() ?? String.Empty;
            }
            config.AlarmClocks[idx].Filename = String.Empty;
        }


        private void RepeatOption_Checked(object sender, RoutedEventArgs e)
        {
            if (RepeatDaily.IsChecked == true) _repeatMode = AlarmRepeatMode.Daily;
            else if (RepeatWeekdays.IsChecked == true) _repeatMode = AlarmRepeatMode.Weekdays;
            else if (RepeatWeekly.IsChecked == true)
            {
                _repeatMode = AlarmRepeatMode.Weekly;
                if (WeeklyDayCombo.SelectedItem != null)
                    Enum.TryParse(WeeklyDayCombo.SelectedItem.ToString(), out _weeklyDay);
            }
            else _repeatMode = AlarmRepeatMode.None;
        }

        private void NotifOption_Checked(object sender, RoutedEventArgs e)
        {
            if (VoiceRadio.IsChecked == true)
            {
                VoicePanel.Visibility = Visibility.Visible;
                MusicPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                VoicePanel.Visibility = Visibility.Collapsed;
                MusicPanel.Visibility = Visibility.Visible;
            }
        }

        private void VoiceTestButton_Click(object sender, RoutedEventArgs e)
        {
            var text = VoiceTextBox.Text;
            var voice = VoiceCombo.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(voice))
            {
                using (var s = new SpeechSynthesizer())
                {
                    s.SelectVoice(voice);
                    s.SpeakAsync(text);
                }
            }
        }

        private void MusicBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio Files|*.wav;*.mp3;*.wma;*.aac;*.m4a|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                MusicFileTextBox.Text = dlg.FileName;
            }
        }

        private void MusicTestButton_Click(object sender, RoutedEventArgs e)
        {
            var file = MusicFileTextBox.Text;
            _mediaPlayer.Close();
            if (!string.IsNullOrWhiteSpace(file) && System.IO.File.Exists(file))
            {
                try
                {
                    if (System.IO.Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        var sp = new SoundPlayer(file);
                        sp.Play();
                    }
                    else
                    {
                        _mediaPlayer.Open(new Uri(file));
                        _mediaPlayer.Play();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing file: {ex.Message}");
                }
            }
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            // compute selected time
            if (!int.TryParse(HourCombo.SelectedItem as string, out int hour12)) return;
            if (!int.TryParse(MinuteCombo.SelectedItem as string, out int minute)) return;
            bool isPm = (AmPmCombo.SelectedItem as string) == "PM";
            int hour24 = hour12 % 12 + (isPm ? 12 : 0);

            DateTime baseDate = DatePicker.SelectedDate ?? DateTime.Now.Date;
            DateTime candidate = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hour24, minute, 0);

            // When repeating, compute next occurrence according to repeat mode
            _nextAlarm = ComputeNextOccurrence(candidate, _repeatMode);

            if (_nextAlarm <= DateTime.Now)
            {
                // fallback: push one day if in the past for non repeating
                _nextAlarm = _nextAlarm.AddDays(1);
            }

            // Set display
            CompactTitleText.Text = string.IsNullOrWhiteSpace(TitleTextBox.Text) ? "(alarm)" : TitleTextBox.Text.Trim();
            CompactTimeText.Text = _nextAlarm.ToString("f");

            // Switch to compact view
            EnterCompactMode();

            // Start timer
            _isRunning = true;
            _checkTimer.Start();
            StopButton.IsEnabled = true;
            GoButton.IsEnabled = false;
        }

        private DateTime ComputeNextOccurrence(DateTime candidate, AlarmRepeatMode mode)
        {
            DateTime now = DateTime.Now;
            switch (mode)
            {
                case AlarmRepeatMode.None:
                    // If candidate in past, keep candidate but will be pushed in GoButton
                    return candidate;
                case AlarmRepeatMode.Daily:
                    var daily = new DateTime(now.Year, now.Month, now.Day, candidate.Hour, candidate.Minute, 0);
                    if (daily <= now) daily = daily.AddDays(1);
                    return daily;
                case AlarmRepeatMode.Weekdays:
                    // find next Mon-Fri
                    var day = now;
                    DateTime next = new DateTime(day.Year, day.Month, day.Day, candidate.Hour, candidate.Minute, 0);
                    int attempts = 0;
                    while (attempts < 7)
                    {
                        if (next > now && next.DayOfWeek != DayOfWeek.Saturday && next.DayOfWeek != DayOfWeek.Sunday)
                            return next;
                        next = next.AddDays(1);
                        attempts++;
                    }
                    return next;
                case AlarmRepeatMode.Weekly:
                    // target weekly day selected
                    DayOfWeek target = _weeklyDay;
                    // if WeeklyDayCombo has selection, update
                    if (WeeklyDayCombo.SelectedItem != null) Enum.TryParse(WeeklyDayCombo.SelectedItem.ToString(), out target);
                    var start = new DateTime(now.Year, now.Month, now.Day, candidate.Hour, candidate.Minute, 0);
                    int daysToAdd = ((int)target - (int)start.DayOfWeek + 7) % 7;
                    if (daysToAdd == 0 && start <= now) daysToAdd = 7;
                    return start.AddDays(daysToAdd);
                default:
                    return candidate;
            }
        }

        private void EnterCompactMode()
        {
            // save normal size if not already saved
            _normalWidth = Width;
            _normalHeight = Height;
            _normalResizeMode = ResizeMode;

            FullPanel.Visibility = Visibility.Collapsed;
            CompactPanel.Visibility = Visibility.Visible;
            // small size
            Width = 360;
            Height = 140;
            ResizeMode = ResizeMode.NoResize;
        }

        private void ExitCompactMode()
        {
            CompactPanel.Visibility = Visibility.Collapsed;
            FullPanel.Visibility = Visibility.Visible;
            Width = _normalWidth;
            Height = _normalHeight;
            ResizeMode = _normalResizeMode;
        }

        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            if (!_isRunning) return;
            if (DateTime.Now >= _nextAlarm)
            {
                // Trigger alarm
                _checkTimer.Stop();
                ActivateAlarm();
            }
            else
            {
                // update compact time text to keep user informed
                CompactTimeText.Text = _nextAlarm.ToString("f");
            }
        }

        private void ActivateAlarm()
        {
            // Expand window
            ExitCompactMode();

            // Play notification
            if (MusicRadio.IsChecked == true)
            {
                var file = MusicFileTextBox.Text;
                if (!string.IsNullOrWhiteSpace(file) && System.IO.File.Exists(file))
                {
                    try
                    {
                        if (System.IO.Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                        {
                            var sp = new SoundPlayer(file);
                            sp.Play();
                        }
                        else
                        {
                            _mediaPlayer.Open(new Uri(file));
                            _mediaPlayer.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error playing music: {ex.Message}");
                    }
                }
            }
            else
            {
                var msg = VoiceTextBox.Text;
                var voice = VoiceCombo.SelectedItem as string;
                try
                {
                    _synth?.Dispose();
                    _synth = new SpeechSynthesizer();
                    if (!string.IsNullOrWhiteSpace(voice)) _synth.SelectVoice(voice);
                    if (!string.IsNullOrWhiteSpace(msg))
                        _synth.SpeakAsync(msg);
                    else
                        _synth.SpeakAsync("Alarm");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Voice synth error: {ex.Message}");
                }
            }

            // If repeating, compute next occurrence and schedule re-collapse
            if (_repeatMode != AlarmRepeatMode.None)
            {
                _nextAlarm = ComputeNextOccurrence(_nextAlarm, _repeatMode);
                // If next alarm still in the past (shouldn't normally happen), advance one unit
                if (_nextAlarm <= DateTime.Now)
                {
                    switch (_repeatMode)
                    {
                        case AlarmRepeatMode.Daily: _nextAlarm = _nextAlarm.AddDays(1); break;
                        case AlarmRepeatMode.Weekdays:
                            do { _nextAlarm = _nextAlarm.AddDays(1); } while (_nextAlarm.DayOfWeek == DayOfWeek.Saturday || _nextAlarm.DayOfWeek == DayOfWeek.Sunday);
                            break;
                        case AlarmRepeatMode.Weekly: _nextAlarm = _nextAlarm.AddDays(7); break;
                    }
                }

                // After a short delay, hide again and restart timer
                var rebundleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                rebundleTimer.Tick += (s, e) =>
                {
                    rebundleTimer.Stop();
                    EnterCompactMode();
                    _checkTimer.Start();
                };
                rebundleTimer.Start();
            }
            else
            {
                // Non repeating: stop running, allow user to start again
                _isRunning = false;
                StopButton.IsEnabled = false;
                GoButton.IsEnabled = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // cancel any pending notifications and timers
            _checkTimer.Stop();
            _isRunning = false;
            StopButton.IsEnabled = false;
            GoButton.IsEnabled = true;

            try
            {
                _mediaPlayer.Stop();
            }
            catch { }
            try
            {
                _synth?.SpeakAsyncCancelAll();
            }
            catch { }

            // restore full UI so user can edit
            ExitCompactMode();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // cleanup
            _checkTimer.Stop();
            try { _mediaPlayer.Close(); } catch { }
            try { _synth?.Dispose(); } catch { }
            Close();
        }
    }
}