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
                if (VoiceComboBox.Items.Count > 0)
                    VoiceComboBox.SelectedIndex = 0;
            }

            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            this.Closed += (s, e) => { mediaPlayer.Close(); };
            LoadedDataFromSettings();
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
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(voice))
            {
                using (var synth = new SpeechSynthesizer())
                {
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

    }
}