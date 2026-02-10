using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Speech.Synthesis;

namespace TimeToy
{
    /// <summary>
    /// Interaction logic for StopWatcher.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for StopWatcher.xaml
    /// </summary>
    public partial class StopWatcher : Window
    {
        // Pseudocode plan:
        // 1. Create a loop that runs while a condition is true (e.g., a cancellation token or a flag).
        // 2. Inside the loop, get the current timer time (e.g., from a Stopwatch instance).
        // 3. Write the formatted time to OutputTextBox.
        // 4. Await Task.Delay(100) for 0.1 seconds.
        // 5. Provide a way to stop the loop (e.g., set the flag to false or cancel the token).

        // Example implementation (add fields and a method):

        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private bool _isTimerRunning;
        private bool _showTimer;
        private RunConfigManager _configManager;
        private AudioNotificationManager _audioManager = new AudioNotificationManager();
        private OneShotInputWatcher _oneShotWatcher;
        public StopWatcher(RunConfigManager config)
        {
            InitializeComponent();
            PrepareForStopwatchAction();
            _configManager = config;
            WindowSettingsManager.ApplyToWindow(this, _configManager.runConfig.StopWatcherOptions.windowSettings);

            // Subscribe to move/size/state events
            LocationChanged += (s, e) => { CaptureWindowsLocation(); };
            SizeChanged += (s, e) => { CaptureWindowsLocation(); };
            StateChanged += (s, e) => { CaptureWindowsLocation(); };
            Closing += (s, e) => { CaptureWindowsLocation(); };
            // create watcher that defers the "user code" action to this lambda
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
            WindowSettingsManager.CaptureFromWindow(this, _configManager.runConfig.StopWatcherOptions.windowSettings);
            _configManager.Save();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void PrepareForStopwatchAction()
        {
            _showTimer = true;
            if (CountdownTextBox == null)
                return;
            CountdownTextBox.Clear();
            CounterTextBox.Clear(); 
            _stopwatch.Reset();
            _showTimer = true;
            LapButton.Content = "Lap";

        }

        private void Speak(string text)
        {
            _audioManager.PlayVoice(text);
        }


        // The code you have is correct for disabling those buttons:
        private void DisableAllStartButtons()
        {
            StopWatcherNow.IsEnabled = false;
            StopWatcher5Seconds.IsEnabled = false;
            StopWatcher10Seconds.IsEnabled = false;
            StopWatcher5Sound.IsEnabled = false;
            ClearButton.IsEnabled = false;
        }
        private void EnableAllStartButtons()
        {
            StopWatcherNow.IsEnabled = true;
            StopWatcher5Seconds.IsEnabled = true;
            StopWatcher10Seconds.IsEnabled = true;
            StopWatcher5Sound.IsEnabled = true;
            ClearButton.IsEnabled = true;   
        }

        private async Task LaunchTheStopwatch(bool speak = false)
        {
            CountdownTextBox.Text = "go!";
            if ( speak)
            { Speak("Go!"); }
            var timerTask = StartTimerLoopAsync();
            await Task.Delay(2000);
            CountdownTextBox.Text = $"Started: {DateTime.Now.ToString("HH:mm:ss.ff")}";
            await timerTask;
        }




        private async void StopWatcher10Seconds_Click(object sender, RoutedEventArgs e)
        {

            DisableAllStartButtons();
            PrepareForStopwatchAction();
            

            for (int i = 10; i >= 1; i--)
            {
                CountdownTextBox.Text = i.ToString();
                await Task.Delay(1000);
            }

            await LaunchTheStopwatch();

            EnableAllStartButtons();
        }


        private async void StopWatcher5Seconds_Click(object sender, RoutedEventArgs e)
        {
            DisableAllStartButtons();
            PrepareForStopwatchAction();

            for (int i = 5; i >= 1; i--)
            {
                CountdownTextBox.Text = i.ToString();
                await Task.Delay(1000);
            }

            await LaunchTheStopwatch();

            EnableAllStartButtons();

        }

        private async void StopWatcher5Sound_Click(object sender, RoutedEventArgs e)
        {
            DisableAllStartButtons();
            PrepareForStopwatchAction();
            
            for (int i = 5; i >= 1; i--)
            {

                CountdownTextBox.Text = i.ToString();
                Speak(i.ToString() );
                await Task.Delay(1000);
            }

            await LaunchTheStopwatch(true);
            EnableAllStartButtons();

        }

        private async Task StartTimerLoopAsync()
        {
            if (CountdownTextBox == null)
                return;

            _isTimerRunning = true;
            _stopwatch.Start();

            while (_isTimerRunning)
            {
                if (_showTimer)
                {
                    CounterTextBox.Text = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.ff");
                }
                
                await Task.Delay(100);
            }

            _stopwatch.Stop();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isTimerRunning = false; 
        }

        private void LapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showTimer)
            {
                _showTimer = false;
                LapButton.Content = "Resume";
            } else
            {
                _showTimer = true;
                LapButton.Content = "Lap";
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {            
            PrepareForStopwatchAction(); 

        }

        private async void StopWatcherNow_Click(object sender, RoutedEventArgs e)
        {
            DisableAllStartButtons();
            PrepareForStopwatchAction();
            await LaunchTheStopwatch(false);
            EnableAllStartButtons();

        }
    } 
}
