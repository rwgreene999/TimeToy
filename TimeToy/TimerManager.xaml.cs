using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static TimeToy.RunConfigManager;



namespace TimeToy
{

    

    /// <summary>
    /// Interaction logic for TimerManager.xaml
    /// </summary>
    /// 

    public partial class TimerManager : Window, INotifyPropertyChanged
    {
        private TimeSpan _originalSelectedNotification = TimeSpan.Zero;
        private TimeSpan _timeBacking = TimeSpan.Zero;
        private AudioNotificationManager _audioManager = new AudioNotificationManager();
        private OneShotInputWatcher _oneShotWatcher;
        private TimeSpan _time
        {
            get => _timeBacking;
            set
            {
                if (value == TimeSpan.Zero && _timeBacking != TimeSpan.Zero)
                {
                    SetOperationalState(OperationState.Zero);
                }
                else if (value != TimeSpan.Zero && _timeBacking == TimeSpan.Zero)
                {
                    SetOperationalState(OperationState.Ready);
                }
                _timeBacking = value;
                OnPropertyChanged(nameof(TimeString));
            }
        }

        Brush _originalTimeTextboxBackground = Brushes.White;
        private DateTime _expectedExpireTimeBacking = DateTime.MinValue;
        DateTime ExpectedExpireTime
        {
            get => _expectedExpireTimeBacking;
            set
            {
                _expectedExpireTimeBacking = value;
                if (_expectedExpireTimeBacking != null && _expectedExpireTimeBacking != DateTime.MinValue)
                {
                    TimeNotificationTextbox.Text = $"Notification time: {_expectedExpireTimeBacking.ToString("HH:mm:ss")}";
                }
                else
                {
                    TimeNotificationTextbox.Text = string.Empty;
                }
            }
        }



        private System.Windows.Threading.DispatcherTimer _dispatcherTimer;
        TimeSpan _snoozedTimeRemaining;


        private enum OperationState
        {
            Zero,
            Ready,
            Going,
            Paused,
            Ended
        }

        OperationState _currentOperationState = OperationState.Zero;

        RunConfigManager _configManager;
        public TimerManager(RunConfigManager config)
        {
            InitializeComponent();
            _time = TimeSpan.Zero;
            DataContext = this;
            _originalTimeTextboxBackground = TimeTextbox.Background;
            SetOperationalState(OperationState.Zero);
            _configManager = config;

            WindowSettingsManager.ApplyToWindow(this, _configManager.runConfig.TimerOptions.windowSettings);
            // Subscribe to move/size/state events
            LocationChanged += (s, e) => { CaptureWindowsLocation(); };
            SizeChanged += (s, e) => { CaptureWindowsLocation(); };
            StateChanged += (s, e) => { CaptureWindowsLocation(); };

            _oneShotWatcher = new OneShotInputWatcher(this, () =>
            {
                // <<User Code Goes Here>> - invoked once on next key or mouse input for this window
                try
                {
                    _audioManager.StopAll();
                    this.Topmost = false;
                    NativeMethods.UnforceForegroundWindow(this);
                }
                catch (Exception ex)
                {
                    try { ErrorLogging.Log(ex, "Error during UnforceForegroundWindow call."); } catch { }
                }
            });

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // allow base to run first so WPF can do its cleanup
            base.OnClosing(e);

            try { CaptureWindowsLocation(); } catch { }

            // stop and dispose audio resources created by this window
            try
            {
                _audioManager?.StopAll();
                _audioManager?.Dispose();
                _audioManager = null;
            }
            catch (Exception ex)
            {
                // log but do not prevent closing
                try { ErrorLogging.Log(ex, "Error cleaning up audio manager on closing."); } catch { }
            }

            _oneShotWatcher?.Dispose();
            _oneShotWatcher = null;

        }

        private void CaptureWindowsLocation()
        {
            WindowSettingsManager.CaptureFromWindow(this, _configManager.runConfig.TimerOptions.windowSettings);
            _configManager.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddTimeToTimer(TimeSpan timeToAdd)
        {
            _time = _time.Add(timeToAdd);
            OnPropertyChanged(nameof(TimeString));
            if (_dispatcherTimer?.IsEnabled == true)
            {
                ExpectedExpireTime = ExpectedExpireTime.Add(timeToAdd);
            }
        }


        private void Add10Minutes_Click(object sender, RoutedEventArgs e)
        {
            AddTimeToTimer(TimeSpan.FromMinutes(10));
        }

        private void Add1Minute_Click(object sender, RoutedEventArgs e)
        {
            AddTimeToTimer(TimeSpan.FromMinutes(1));
        }
        private void Add30Seconds_Click(object sender, RoutedEventArgs e)
        {
            AddTimeToTimer(TimeSpan.FromSeconds(30));
        }
        private void Add30Minutes_Click(object sender, RoutedEventArgs e)
        {
            AddTimeToTimer(TimeSpan.FromMinutes(30));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _dispatcherTimer?.Stop();
            this.Close();
        }


        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            _originalSelectedNotification = _time;
            if (_time.TotalSeconds <= 0)
                return;

            ExpectedExpireTime = DateTime.Now.Add(_time);
            if (_dispatcherTimer == null)
            {
                _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
                _dispatcherTimer.Tick += DispatcherTimer_Tick;
            }
            _dispatcherTimer.Start();
            SetOperationalState(OperationState.Going);

        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_currentOperationState != OperationState.Paused)
            {
                if (ExpectedExpireTime != DateTime.MinValue)
                {
                    var remaining = ExpectedExpireTime - DateTime.Now;
                    if (remaining.TotalSeconds > 0)
                    {
                        _time = remaining;
                        OnPropertyChanged(nameof(TimeString));
                    }
                    else
                    {
                        try
                        {
                            OnPropertyChanged(nameof(TimeString));
                            _dispatcherTimer.Stop();
                            ExpectedExpireTime = DateTime.MinValue;
                            NotifyUserTimeIsUp();
                            SetOperationalState(OperationState.Ended);
                            _time = TimeSpan.Zero;
                        }
                        catch (Exception ex)
                        {
                            ErrorLogging.Log(ex, "Error during timer tick expiration handling.");
                        }


                    }
                }
            }
        }

        private void NotifyUserTimeIsUp()
        {
            try
            {
                FullScreenNotification fs = new FullScreenNotification();
                fs.Show();
                if (_configManager.runConfig.TimerOptions.Notification == TimerNotificationOptions.Voice)
                {
                    _audioManager.PlayVoice(_configManager.runConfig.TimerOptions.Comment, 
                        _configManager.runConfig.TimerOptions.Voice, 100);
                }

                if (_configManager.runConfig.TimerOptions.Notification == TimerNotificationOptions.Sound)
                {
                    _audioManager.PlaySound(_configManager.runConfig.TimerOptions.Filename);
                }


                // Bring window to foreground and make it topmost
                this.Topmost = true;
                this.Activate();
                NativeMethods.ForceForegroundWindow(this);
                // wip consider this: 
                // Ensure window receives keyboard input
                //this.Focus();
                //Keyboard.Focus(this);
                _oneShotWatcher?.StartWatching();


            }
            catch (Exception ex)
            {
                ErrorLogging.Log(ex, "Error during notification playback.");
            }

        }

        
        //private KeyEventHandler _oneTimeKeyHandler;
        //// Add this field alongside the existing _oneTimeKeyHandler
        //private MouseButtonEventHandler _oneTimeMouseHandler;

        //private void WatchForAnyKey()
        //{
        //    // Remove any previous one-shot handlers (use RemoveHandler because we'll use AddHandler)
        //    if (_oneTimeMouseHandler != null)
        //    {
        //        try { RemoveHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler); } catch { }
        //        _oneTimeMouseHandler = null;
        //    }
        //    if (_oneTimeKeyHandler != null)
        //    {
        //        try { RemoveHandler(KeyDownEvent, _oneTimeKeyHandler); } catch { }
        //        _oneTimeKeyHandler = null;
        //    }

        //    // Create the one-shot mouse handler (client-area clicks, includes clicks on child controls)
        //    _oneTimeMouseHandler = (s, e) =>
        //    {
        //        try
        //        {
        //            _audioManager.StopAll();
        //            this.Topmost = false;
        //            NativeMethods.UnforceForegroundWindow(this);
        //        }
        //        catch (Exception ex)
        //        {
        //            try { ErrorLogging.Log(ex, "Error during UnforceForegroundWindow call."); } catch { }
        //        }
        //        finally
        //        {
        //            try { RemoveHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler); } catch { }
        //            try { if (_oneTimeKeyHandler != null) RemoveHandler(KeyDownEvent, _oneTimeKeyHandler); } catch { }
        //            _oneTimeMouseHandler = null;
        //            _oneTimeKeyHandler = null;
        //        }
        //        // do NOT set e.Handled = true so normal click processing continues
        //    };

        //    // Create the one-shot key handler (only triggers if the window has focus)
        //    _oneTimeKeyHandler = new KeyEventHandler((s, e) =>
        //    {
        //        try
        //        {
        //            _audioManager.StopAll();
        //            this.Topmost = false;
        //            NativeMethods.UnforceForegroundWindow(this);
        //        }
        //        catch (Exception ex)
        //        {
        //            try { ErrorLogging.Log(ex, "Error during UnforceForegroundWindow call."); } catch { }
        //        }
        //        finally
        //        {
        //            try { RemoveHandler(KeyDownEvent, _oneTimeKeyHandler); } catch { }
        //            try { if (_oneTimeMouseHandler != null) RemoveHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler); } catch { }
        //            _oneTimeKeyHandler = null;
        //            _oneTimeMouseHandler = null;
        //        }
        //        // do NOT set e.Handled = true so normal key processing continues
        //    });

        //    // Register handlers so they receive events even if child controls marked them handled
        //    AddHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler, handledEventsToo: true);
        //    AddHandler(KeyDownEvent, _oneTimeKeyHandler, handledEventsToo: true);

        //    // No forcing of focus; if the user focuses this window and then presses a key or clicks,
        //    // the handlers will fire and then remove themselves (one-shot).
        //}


        private void MediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            ErrorLogging.Log(e.ErrorException, "Media playback failed.");
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOperationState == OperationState.Paused)
            {
                ExpectedExpireTime = DateTime.Now.Add(_snoozedTimeRemaining);
                SetOperationalState(OperationState.Going);
                PauseButton.Content = "Pause";

            }
            else
            {
                SetOperationalState(OperationState.Paused);
                _snoozedTimeRemaining = ExpectedExpireTime - DateTime.Now;
                PauseButton.Content = "Resume";
                TimeNotificationTextbox.Text = String.Empty;
            }

        }

        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove topmost when user presses End
            this.Topmost = false;
            _dispatcherTimer?.Stop();
            _time = TimeSpan.Zero;
            OnPropertyChanged(nameof(TimeString));
            if (_currentOperationState == OperationState.Paused)
            {
                ExpectedExpireTime = DateTime.Now.Add(_snoozedTimeRemaining);
                SetOperationalState(OperationState.Ended);
                PauseButton.Content = "Pause";
            }
            SetOperationalState(OperationState.Ended);
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _time = _originalSelectedNotification;
            OnPropertyChanged(nameof(TimeString));
            SetOperationalState(OperationState.Ready);
            GoButton_Click(GoButton, new RoutedEventArgs());
        }


        private void TimeTextbox_Changed(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
                return;


            var parts = textBox.Text.Split(':');

            switch (parts.Length)
            {
                case 0:
                    // No input, do nothing or reset
                    textBox.Background = Brushes.LightPink;
                    break;
                case 1:
                    // Only seconds provided
                    if (int.TryParse(parts[0], out int seconds1))
                    {
                        _time = new TimeSpan(0, 0, seconds1);
                        OnPropertyChanged(nameof(TimeString));
                        textBox.Background = _originalTimeTextboxBackground;
                    }
                    else
                    {
                        textBox.Background = Brushes.LightPink;
                    }
                    break;
                case 2:
                    // Minutes and seconds provided
                    if (int.TryParse(parts[0], out int minutes2) && int.TryParse(parts[1], out int seconds2))
                    {
                        _time = new TimeSpan(0, minutes2, seconds2);
                        OnPropertyChanged(nameof(TimeString));
                        textBox.Background = _originalTimeTextboxBackground;
                    }
                    else
                    {
                        textBox.Background = Brushes.LightPink;
                    }
                    break;
                case 3:
                    // Hours, minutes, and seconds provided
                    if (int.TryParse(parts[0], out int hours3) && int.TryParse(parts[1], out int minutes3) && int.TryParse(parts[2], out int seconds3))
                    {
                        _time = new TimeSpan(hours3, minutes3, seconds3);
                        OnPropertyChanged(nameof(TimeString));
                        textBox.Background = _originalTimeTextboxBackground;
                    }
                    else
                    {
                        textBox.Background = Brushes.LightPink;
                    }
                    break;
                default:
                    // Invalid format
                    textBox.Background = Brushes.LightPink;
                    break;
            }
        }

        public string TimeString
        {
            get => _time.ToString(@"hh\:mm\:ss");
            set
            {
                if (TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, out var ts))
                {
                    _time = ts;
                }
                // Optionally, handle invalid input
            }
        }

        private void SetOperationalState(OperationState state)
        {
            _currentOperationState = state;
            switch (state)
            {
                case OperationState.Zero:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Ready:
                    GoButton.IsEnabled = true;
                    EndButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Going:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = true;
                    PauseButton.IsEnabled = true;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Paused:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = true;
                    PauseButton.IsEnabled = true;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Ended:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                    RepeatButton.IsEnabled = true;
                    break;
            }
        }

    }

}
