using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using Microsoft.Win32;
using System.Media;
using System.Windows.Media;
using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Windows.Threading;

namespace TimeToy
{
    public partial class OptionsManager : Window
    {
        double _StopWatchVolume = 100.0;
        private RunConfigManager _originalConfigManager;
        private RunConfigManager _configManager = new RunConfigManager();
        private MediaPlayer mediaPlayer = new MediaPlayer();

        // Prevent re-entrancy when we programmatically Close() after confirmation
        private bool _closeConfirmed;

        public OptionsManager(RunConfigManager config)
        {
            InitializeComponent();
            _originalConfigManager = config;
            _originalConfigManager.SaveNow(); // ensure saved before editing
            _configManager.Load(); // load from file to edit

            // Populate installed voices
            using (var synth = new SpeechSynthesizer())
            {
                VoiceComboBox.ItemsSource = synth.GetInstalledVoices()
                    .Select(v => v.VoiceInfo.Name)
                    .ToList();
                StopWatchComboBox.ItemsSource = synth.GetInstalledVoices()
                    .Select(v => v.VoiceInfo.Name)
                    .ToList();
                if (VoiceComboBox.Items.Count > 0)
                    VoiceComboBox.SelectedIndex = 0;
            }

            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            WindowSettingsManager.ApplyToWindow(this, _originalConfigManager.runConfig.optionsSettings.windowSettings);

            // Subscribe to move/size/state events
            LocationChanged += (s, e) => { CaptureWindowsLocation(); };
            SizeChanged += (s, e) => { CaptureWindowsLocation(); };
            StateChanged += (s, e) => { CaptureWindowsLocation(); };
            Closing += (s, e) => { _configManager.SaveNow(); };

            // handle closing with timed prompt
            this.Closing += OptionsManager_Closing;

            LoadedUsersDataFromSettings();

            this.Closed += (s, e) =>
            {
                mediaPlayer.Close();
            };

            StopWatchVolumeSlider.ValueChanged += StopWatchVolumeSlider_ValueChanged;
        }

        private void CaptureWindowsLocation()
        {
            WindowSettingsManager.CaptureFromWindow(this, _originalConfigManager.runConfig.optionsSettings.windowSettings);
            _originalConfigManager.Save();
        }


        private void StopWatchVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _StopWatchVolume = e.NewValue;
        }

        private void LoadedUsersDataFromSettings()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // Get build time (last write time of the assembly)
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var buildTime = System.IO.File.GetLastWriteTime(assemblyPath);
            var buildTimeStr = buildTime.ToString("yyyy-MM-dd HH:mm:ss");
            RunVersion.Text = $"{version} (Built: {buildTimeStr})";


            if (_configManager.runConfig.TimerOptions.Notification == RunConfigManager.TimerNotificationOptions.Voice)
            {
                VoiceRadio.IsChecked = true;
            }
            else if (_configManager.runConfig.TimerOptions.Notification == RunConfigManager.TimerNotificationOptions.Sound)
            {
                MusicRadio.IsChecked = true;
            }
            VoiceTextBox.Text = _configManager.runConfig.TimerOptions.Comment;
            VoiceComboBox.SelectedItem = _configManager.runConfig.TimerOptions.Voice;
            MusicFileTextBox.Text = _configManager.runConfig.TimerOptions.Filename;

            StopWatchComboBox.SelectedItem = _configManager.runConfig.StopWatcherOptions.Voice;
            _StopWatchVolume = _configManager.runConfig.StopWatcherOptions.Volume;
            StopWatchVolumeSlider.Value = _StopWatchVolume;

            if (_configManager.runConfig.Theme == "Dark")
            {
                ThemeDark.IsChecked = true;
            }
            else
            {
                ThemeLight.IsChecked = true;
            }

        }

        private void MediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show($"media error:{e.ToString()} details:{e.ErrorException}");
            throw new NotImplementedException();
        }

        private void Radio_Checked2(object sender, RoutedEventArgs e)
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

        private void VoiceTest_Click(object sender, RoutedEventArgs e)
        {
            var text = VoiceTextBox.Text;
            var voice = VoiceComboBox.SelectedItem as string;
            Speaker(text, voice);
        }

        private void StopWatchTest_Click(object sender, RoutedEventArgs e)
        {
            string text = "5, 4, 3, 2, 1, Go...";
            string voice = StopWatchComboBox.SelectedItem as string;
            Speaker(text, voice, (int)StopWatchVolumeSlider.Value);
        }

        private void Speaker(string text, string voice, int volume = 100)
        {
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(voice))
            {
                using (var synth = new SpeechSynthesizer())
                {
                    synth.Volume = volume;
                    synth.SelectVoice(voice);
                    synth.Speak(text);
                }
            }
        }

        private void Keep()
        {
            if (VoiceRadio.IsChecked == true) { _configManager.runConfig.TimerOptions.Notification = RunConfigManager.TimerNotificationOptions.Voice; }
            else if (MusicRadio.IsChecked == true) { _configManager.runConfig.TimerOptions.Notification = RunConfigManager.TimerNotificationOptions.Sound; }
            else _configManager.runConfig.TimerOptions.Notification = RunConfigManager.TimerNotificationOptions.Voice;
            _configManager.runConfig.TimerOptions.Comment = VoiceTextBox.Text;
            _configManager.runConfig.TimerOptions.Voice = VoiceComboBox.SelectedItem as string;
            _configManager.runConfig.TimerOptions.Filename = MusicFileTextBox.Text;

            _configManager.runConfig.StopWatcherOptions.Voice = StopWatchComboBox.SelectedItem as string;
            _configManager.runConfig.StopWatcherOptions.Volume = _StopWatchVolume;
        }

        private void Save()
        {
            Keep();
            _configManager.Save();  // this updates the file 
            _originalConfigManager.Load(); // reload new changes for rest of system            
        }

        private void MusicBrowse_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Close();
            var dlg = new OpenFileDialog
            {
                Filter = "Audio Files|*.wav;*.mp3;*.wma;*.aac;*.m4a|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                MusicFileTextBox.Text = dlg.FileName;
            }
        }

        private void MusicTest_Click(object sender, RoutedEventArgs e)
        {

            var file = MusicFileTextBox.Text;
            mediaPlayer.Close();
            if (!string.IsNullOrWhiteSpace(file) && System.IO.File.Exists(file))
            {
                try
                {
                    if (System.IO.Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        var player = new SoundPlayer(file);
                        player.Play();
                    }
                    else
                    {

                        mediaPlayer.Open(new Uri(file));
                        mediaPlayer.Play();
                    }
                }
                catch
                {
                    MessageBox.Show("Unable to play the selected file.");
                }
            }
        }

        private void ThemeDark_Checked(object sender, RoutedEventArgs e)
        {
            _configManager.runConfig.Theme = "Dark";
            ((App)Application.Current).SetTheme("Dark");
        }

        private void ThemeLight_Checked(object sender, RoutedEventArgs e)
        {
            _configManager.runConfig.Theme = "Light";
            ((App)Application.Current).SetTheme("Light");
        }

        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            Save();
            _closeConfirmed = true; 
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _configManager.Load(); // reload previous settings_
            LoadedUsersDataFromSettings();
            // WIP decide if anything changed and prompt by _closeCOnfirmed = false 
            _closeConfirmed = true;
            Close();
        }

        private void Keep_Click(object sender, RoutedEventArgs e)
        {
            Keep();
            Close();
        }

        // Closing handler: prompt user to save; default to "cancel pathway" (discard) after timeout
        private void OptionsManager_Closing(object sender, CancelEventArgs e)
        {
            if (_closeConfirmed)
                return;

            // prevent immediate close while we ask
            e.Cancel = true;

            // show prompt with timeout (15s). Returns true when user chose Save, false otherwise.
            bool save = ShowSavePromptWithTimeout(TimeSpan.FromSeconds(15));

            if (save)
            {
                Save();
            }
            else
            {
                // cancel pathway: discard working edits and reload original manager
                _configManager.Load();
                LoadedUsersDataFromSettings();
            }

            // close for real now
            _closeConfirmed = true;
            
        }

        // Shows a modal dialog asking "Save changes?" with Save / Don't Save buttons.
        // If user doesn't answer within timeout, treat as "Don't Save".
        private bool ShowSavePromptWithTimeout(TimeSpan timeout)
        {
            var dialog = new Window
            {
                Title = "Save changes?",
                Width = 380,
                Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = this,
                Content = BuildPromptContent()
            };

            // Start a DispatcherTimer that will close the dialog after timeout
            var timer = new DispatcherTimer { Interval = timeout };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                // Close without setting DialogResult => treated as "Don't Save"
                try { dialog.Close(); } catch { }
            };
            timer.Start();

            // Show dialog modally; dialog.DialogResult will be true if Save button clicked
            bool? result = dialog.ShowDialog();

            timer.Stop();
            return result == true;
        }

        private UIElement BuildPromptContent()
        {
            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(12) };

            var text = new System.Windows.Controls.TextBlock
            {
                Text = "Save changes to options?",
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(text);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var saveBtn = new System.Windows.Controls.Button { Content = "Save", Width = 80, Margin = new Thickness(6, 0, 0, 0) };
            saveBtn.Click += (s, e) =>
            {
                // set dialog result true and close
                var wnd = Window.GetWindow((DependencyObject)s);
                if (wnd != null) wnd.DialogResult = true;
            };
            buttonPanel.Children.Add(saveBtn);

            var dontSaveBtn = new System.Windows.Controls.Button { Content = "Don't Save", Width = 100, Margin = new Thickness(6, 0, 0, 0) };
            dontSaveBtn.Click += (s, e) =>
            {
                var wnd = Window.GetWindow((DependencyObject)s);
                if (wnd != null) wnd.DialogResult = false;
            };
            buttonPanel.Children.Add(dontSaveBtn);

            panel.Children.Add(buttonPanel);
            return panel;
        }
    }
}