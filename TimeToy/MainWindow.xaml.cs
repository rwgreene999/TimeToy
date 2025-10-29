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
        public MainWindow()
        {
            InitializeComponent();
            _config = RunConfigManager.Load();
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
