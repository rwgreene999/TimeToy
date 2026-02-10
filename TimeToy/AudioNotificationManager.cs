using System;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;
using static System.Net.Mime.MediaTypeNames;


namespace TimeToy
{
    /// rwg Note: AI generated code on request for a centralized audio manager 
    /// to handle both speech and sound playback, with proper resource management 
    /// and error handling. This class is designed to be reusable across multiple 
    /// windows in the application, allowing for consistent audio notifications 
    /// while ensuring that resources are cleaned up appropriately when no longer needed.
    /// 
    /// Here  I learned about "private Dispatcher UIDispatcher => Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;"
    /// to handle sceranios where WPF is not running, like in a console app or during unit tests
    /// If I ever write a console WPF app to play music I wouodl need this!  (this would not make sense) 
    /// 



    /// <summary>
    /// Centralized audio notification manager for speech and sound playback.
    /// Reusable from multiple windows; exposes explicit stop/cleanup methods.
    /// </summary>
    public class AudioNotificationManager : IDisposable
    {
        private readonly object _sync = new object();

        private SpeechSynthesizer _synth;
        private SoundPlayer _soundPlayer;
        private MediaPlayer _mediaPlayer;
        private EventHandler<ExceptionEventArgs> _mediaFailedHandler;

        private Dispatcher UIDispatcher => WpfApplication.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public AudioNotificationManager()
        {
            // nothing heavy here; resources are created on demand
        }

        // Play speech asynchronously. Reuses a single SpeechSynthesizer instance.
        public void PlayVoice(string text, string voiceName = null, int volume = 100, int? speakRate = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            lock (_sync)
            {
                EnsureSynth();
                try
                {
                    if (!string.IsNullOrWhiteSpace(voiceName))
                    {
                        try { _synth.SelectVoice(voiceName); } catch { /* voice may not exist — ignore */ }
                    }
                    _synth.Volume = volume; 
                    if ( speakRate.HasValue)
                    {
                        _synth.Rate = speakRate.Value; 
                    }
                    try { _synth.SpeakAsyncCancelAll(); } catch { }
                    _synth.SpeakAsync(text);
                }
                catch (Exception ex)
                {
                    try { ErrorLogging.Log(ex, "PlayVoice error."); } catch { }
                }
            }
        }

        // Play a sound file. Uses SoundPlayer for WAV, MediaPlayer otherwise.
        public void PlaySound(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (!File.Exists(filePath))
            {   try { ErrorLogging.Log($"Sound file not found: {filePath}"); } catch { }
                PlayVoice("Ended, requested sound file not found");
                return; 
            }

            var ext = Path.GetExtension(filePath) ?? string.Empty;
            if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                lock (_sync)
                {
                    CleanupSoundPlayer();
                    try
                    {
                        _soundPlayer = new SoundPlayer(filePath);
                        _soundPlayer.Play(); // non-blocking
                    }
                    catch (Exception ex)
                    {
                        try { ErrorLogging.Log(ex, "SoundPlayer play error."); } catch { }
                    }
                }
            }
            else
            {
                // MediaPlayer must be used/owned on the UI dispatcher
                UIDispatcher.Invoke(() =>
                {
                    lock (_sync)
                    {
                        CleanupMediaPlayer();
                        try
                        {
                            _mediaPlayer = new MediaPlayer();
                            _mediaFailedHandler = (s, e) =>
                            {
                                try { ErrorLogging.Log(e.ErrorException, "MediaPlayer playback failed."); } catch { }
                            };
                            _mediaPlayer.MediaFailed += (s, e) => _mediaFailedHandler?.Invoke(s, e);
                            _mediaPlayer.Open(new Uri(filePath));
                            _media_player_PlaySafe();
                        }
                        catch (Exception ex)
                        {
                            try { ErrorLogging.Log(ex, "MediaPlayer play error."); } catch { }
                        }
                    }
                });
            }
        }

        // Stop specifically the active MediaPlayer (if any).
        public void StopMediaPlayer()
        {
            UIDispatcher.Invoke(() =>
            {
                lock (_sync)
                {
                    if (_mediaPlayer != null)
                    {
                        try { _mediaPlayer.Stop(); } catch { }
                        try { _mediaPlayer.Close(); } catch { }
                        try
                        {
                            if (_mediaFailedHandler != null) _mediaPlayer.MediaFailed -= (s, e) => _mediaFailedHandler?.Invoke(s, e);
                        }
                        catch { }
                        _mediaPlayer = null;
                        _mediaFailedHandler = null;
                    }
                }
            });
        }

        // Stop all audio/speech and free resources.
        public void StopAll()
        {
            lock (_sync)
            {
                try { _synth?.SpeakAsyncCancelAll(); } catch { }
                try { CleanupSoundPlayer(); } catch { }
                try { StopMediaPlayer(); } catch { }
            }
        }

        // Clean up managed resources.
        public void Dispose()
        {
            StopAll();
            lock (_sync)
            {
                try { _synth?.Dispose(); } catch { }
                _synth = null;
            }
            GC.SuppressFinalize(this);
        }

        #region Helpers

        private void EnsureSynth()
        {
            if (_synth != null) return;
            _synth = new SpeechSynthesizer();
            _synth.SpeakCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    try { ErrorLogging.Log(e.Error, "Speech synthesis error."); } catch { }
                }
            };
        }

        private void CleanupSoundPlayer()
        {
            if (_soundPlayer == null) return;
            try { _soundPlayer.Stop(); } catch { }
            try { _soundPlayer.Dispose(); } catch { }
            _soundPlayer = null;
        }

        private void CleanupMediaPlayer()
        {
            if (_mediaPlayer == null) return;
            try { _mediaPlayer.Stop(); } catch { }
            try { _mediaPlayer.Close(); } catch { }
            try
            {
                if (_mediaFailedHandler != null)
                    _mediaPlayer.MediaFailed -= (s, e) => _mediaFailedHandler?.Invoke(s, e);
            }
            catch { }
            _mediaPlayer = null;
            _mediaFailedHandler = null;
        }

        // Helper to start playback with basic error handling on UI thread
        private void _media_player_PlaySafe()
        {
            try
            {
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                try { ErrorLogging.Log(ex, "MediaPlayer Play() error."); } catch { }
            }
        }

        #endregion
    }
}