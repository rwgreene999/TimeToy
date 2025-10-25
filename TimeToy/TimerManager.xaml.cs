using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TimeToy
{
    /// <summary>
    /// Interaction logic for TimerManager.xaml
    /// </summary>
    /// 

    public partial class TimerManager : Window, INotifyPropertyChanged
    {
        private TimeSpan _originalSelectedTime = TimeSpan.Zero;
        private TimeSpan _timeBacking = TimeSpan.Zero;
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
        private DateTime? _targetExpireTime;
        private System.Windows.Threading.DispatcherTimer _dispatcherTimer;
        TimeSpan _snoozedTimeRemaining; 


        private enum OperationState
        {
            Zero,
            Ready,
            Going,
            Snoozed,
            Ended
        }

        OperationState _currentOperationState = OperationState.Zero;



        public TimerManager()
        {
            InitializeComponent();
            _time = TimeSpan.Zero;
            DataContext = this;
            _originalTimeTextboxBackground = TimeTextbox.Background;
            SetOperationalState(OperationState.Zero);
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
                _targetExpireTime = _targetExpireTime.Value.Add(timeToAdd);
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
            _originalSelectedTime = _time; 
            if (_time.TotalSeconds <= 0)
                return;

            _targetExpireTime = DateTime.Now.Add(_time);
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
            if (_currentOperationState != OperationState.Snoozed )
            {
                if (_targetExpireTime.HasValue)
                {
                    var remaining = _targetExpireTime.Value - DateTime.Now;
                    if (remaining.TotalSeconds > 0)
                    {
                        _time = remaining;
                        OnPropertyChanged(nameof(TimeString));
                    }
                    else
                    {
                        _time = TimeSpan.Zero;
                        OnPropertyChanged(nameof(TimeString));
                        _dispatcherTimer.Stop();
                        _targetExpireTime = null;
                        NotifyUserTimeIsUp();
                        SetOperationalState(OperationState.Ended);
                    }
                }
            }
        }

        private void NotifyUserTimeIsUp()
        {
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            synthesizer.SpeakAsync("Timer is up.");

            // Bring window to foreground and make it topmost
            this.Topmost = true;
            this.Activate();
        }

        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOperationState == OperationState.Snoozed)
            {
                _targetExpireTime = DateTime.Now.Add(_snoozedTimeRemaining);
                SetOperationalState(OperationState.Going);
                SnoozeButton.Content = "Snooze";
            }
            else
            {  
                SetOperationalState(OperationState.Snoozed);
                _snoozedTimeRemaining = _targetExpireTime.Value - DateTime.Now;
                SnoozeButton.Content = "Resume";
            }

        }

        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove topmost when user presses End
            this.Topmost = false;
            _dispatcherTimer?.Stop();
            _time = TimeSpan.Zero; 
            OnPropertyChanged(nameof(TimeString));
            if (_currentOperationState == OperationState.Snoozed)
            {
                _targetExpireTime = DateTime.Now.Add(_snoozedTimeRemaining);
                _currentOperationState = OperationState.Ended;
                SnoozeButton.Content = "Snooze";
            }
            SetOperationalState( OperationState.Ended);
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _time = _originalSelectedTime;
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
                    SnoozeButton.IsEnabled = false;
                    RepeatButton.IsEnabled = false; 
                    break;
                case OperationState.Ready:
                    GoButton.IsEnabled = true;
                    EndButton.IsEnabled = false;
                    SnoozeButton.IsEnabled = false;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Going:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = true;
                    SnoozeButton.IsEnabled = true;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Snoozed:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = true;
                    SnoozeButton.IsEnabled = true;
                    RepeatButton.IsEnabled = false;
                    break;
                case OperationState.Ended:
                    GoButton.IsEnabled = false;
                    EndButton.IsEnabled = false;
                    SnoozeButton.IsEnabled = false;
                    RepeatButton.IsEnabled = true;
                    break;
            }
        }

    }
}
