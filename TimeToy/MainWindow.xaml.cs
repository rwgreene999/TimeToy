using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using Microsoft.Win32;

namespace TimeToy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RunConfigManager _configManager; 

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _configManager = new RunConfigManager();
                _configManager.Load();
                ((App)Application.Current).SetTheme(_configManager.runConfig.Theme);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config, ex={ex.Message} Possible solution: delete json config file");
                ErrorLogging.Log(ex, "Error loading RunConfigManager in MainWindow constructor.");
                Application.Current.Shutdown(); // Exit the application on error
                return;
            }
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Get build time (last write time of the assembly)
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var buildTime = System.IO.File.GetLastWriteTime(assemblyPath);
            var buildTimeStr = buildTime.ToString("yyyy-MM-dd HH:mm:ss");

            RunVersion.Text = $"{version} (Built: {buildTimeStr})";

            // Apply saved window settings
            WindowSettingsManager.ApplyToWindow(this, _configManager.runConfig.windowSettings);
            CaptureWindowsLocation(); 


            // Subscribe to move/size/state events
            LocationChanged += (s, e) => { CaptureWindowsLocation();  };
            SizeChanged += (s, e) => { CaptureWindowsLocation(); };
            StateChanged += (s, e) => { CaptureWindowsLocation(); };
            Closing += (s, e) => { _configManager.SaveNow();  };
        }

        private void CaptureWindowsLocation()
        {
            WindowSettingsManager.CaptureFromWindow(this, _configManager.runConfig.windowSettings);
            _configManager.Save();
        }


        private void Timer_Click(object sender, RoutedEventArgs e)
        {
            var timerManager = new TimerManager(_configManager);
            timerManager.Owner = this;
            timerManager.Show();
        }

        private void StopWatch_Click(object sender, RoutedEventArgs e)
        {
            var stopWatcherManager = new StopWatcher(_configManager);
            stopWatcherManager.Owner = this;
            stopWatcherManager.Show();

        }

        private void Alarm_Click(object sender, RoutedEventArgs e)
        {
            var alarmClockManager = new AlarmClock(_configManager);
            alarmClockManager.Show();
        }


        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var OptionsManager = new OptionsManager(_configManager);
            OptionsManager.ShowDialog();

        }
    }
}
