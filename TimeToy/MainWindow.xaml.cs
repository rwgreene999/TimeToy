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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Timer_Click(object sender, RoutedEventArgs e)
        {
            var timerManager = new TimerManager();
            timerManager.ShowDialog();
        }

        private void StopWatch_Click(object sender, RoutedEventArgs e)
        {
            var stopWatcherManager = new StopWatcher();
            stopWatcherManager.ShowDialog();

        }

        private void Alarm_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Alarm button clicked.";
        }
    }
}
