using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using Microsoft.Win32;
using System.Media;
using System.Windows.Media;
using System;
using System.Runtime.CompilerServices;

namespace TimeToy
{
    public partial class OptionsManager : Window
    {
        double _StopWatchVolume = 100.0; 
        RunConfig _config; 
        private MediaPlayer mediaPlayer = new MediaPlayer();
        public OptionsManager(RunConfig config )
        {
            InitializeComponent();
            _config = config; 
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

            this.Closed += (s, e) => { mediaPlayer.Close(); };
            LoadedDataFromSettings();

            StopWatchVolumeSlider.ValueChanged += StopWatchVolumeSlider_ValueChanged;
        }

        private void StopWatchVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _StopWatchVolume = e.NewValue;
        }

        private void LoadedDataFromSettings()
        {
            if ( _config.TimerOptions.Notification == TimerNotificationOptions.Voice)
            {
                VoiceRadio.IsChecked = true;
            }
            else if (_config.TimerOptions.Notification == TimerNotificationOptions.Sound)
            {
                MusicRadio.IsChecked = true;
            }
            VoiceTextBox.Text = _config.TimerOptions.Comment;
            VoiceComboBox.SelectedItem = _config.TimerOptions.Voice;
            MusicFileTextBox.Text = _config.TimerOptions.Filename;

            StopWatchComboBox.SelectedItem = _config.StopWatcherOptions.Voice;
            _StopWatchVolume = _config.StopWatcherOptions.Volume;
            StopWatchVolumeSlider.Value = _StopWatchVolume;

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


        private void Speaker( string text, string voice, int volume = 100)
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

        private void VoiceSave_Click(object sender, RoutedEventArgs e)
        {
            Save(); 
        }


        private void Save()
        {
            if (VoiceRadio.IsChecked == true) { _config.TimerOptions.Notification = TimerNotificationOptions.Voice; }
            else if (MusicRadio.IsChecked == true) { _config.TimerOptions.Notification = TimerNotificationOptions.Sound; }
            else _config.TimerOptions.Notification = TimerNotificationOptions.Voice;
            _config.TimerOptions.Comment = VoiceTextBox.Text;
            _config.TimerOptions.Voice = VoiceComboBox.SelectedItem as string;
            _config.TimerOptions.Filename = MusicFileTextBox.Text;

            _config.StopWatcherOptions.Voice = StopWatchComboBox.SelectedItem as string;
            _config.StopWatcherOptions.Volume = _StopWatchVolume; 
            
            RunConfigManager.Save(_config);
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
        private void SaveMusic_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
        private void StopWatchSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void ThemeDark_Checked(object sender, RoutedEventArgs e)
        {
            _config.Theme = "Dark";
            ((App)Application.Current).SetTheme("Dark");
        }

        private void ThemeLight_Checked(object sender, RoutedEventArgs e)
        {
            _config.Theme = "Light"; 
            ((App)Application.Current).SetTheme("Light");
        }
    }
}