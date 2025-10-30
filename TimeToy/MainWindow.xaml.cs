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

namespace TimeToy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RunConfig _config; 
      // Pseudocode:
        // 1. Get the path to the current assembly.
        // 2. Use System.IO.File.GetLastWriteTime to get the last write time (as a proxy for build time).
        // 3. Format the build time as a string.
        // 4. Append or display the build time with the version in RunVersion.Text.

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _config = RunConfigManager.Load();
                ((App)Application.Current).SetTheme(_config.Theme);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config, ex={ex.Message} Possible solution: delete json config file");
                Application.Current.Shutdown(); // Exit the application on error
                return;
            }
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Get build time (last write time of the assembly)
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var buildTime = System.IO.File.GetLastWriteTime(assemblyPath);
            var buildTimeStr = buildTime.ToString("yyyy-MM-dd HH:mm:ss");

            RunVersion.Text = $"{version} (Built: {buildTimeStr})";
        }

        private void Timer_Click(object sender, RoutedEventArgs e)
        {
            var timerManager = new TimerManager(_config);
            timerManager.Show();
        }

        private void StopWatch_Click(object sender, RoutedEventArgs e)
        {
            var stopWatcherManager = new StopWatcher(_config);
            stopWatcherManager.Show();

        }

        private void Alarm_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Alarm button clicked.";
            this.Title = "Move ByMe";
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
            var OptionsManager = new OptionsManager(_config);
            OptionsManager.ShowDialog();

        }
    }
}
