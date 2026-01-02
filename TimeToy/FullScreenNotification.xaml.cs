using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Forms;

namespace TimeToy
{
    public partial class FullScreenNotification : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly Stopwatch _sw = new Stopwatch();

        public FullScreenNotification()
        {
            InitializeComponent(); // REQUIRED

            MakeClickThrough();
            CoverAllScreens();

            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += TimerTick;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _sw.Start();
            _timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            double t = _sw.Elapsed.TotalSeconds;

            if (t >= 5)
            {
                _timer.Stop();
                Close();
                return;
            }

            double hue = (t / 5.0) * 360.0;
            BorderElement.BorderBrush = new SolidColorBrush(ColorFromHSV(hue, 1.0, 1.0));
        }

        private void CoverAllScreens()
        {
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            foreach (var screen in Screen.AllScreens)
            {
                left = Math.Min(left, screen.Bounds.Left);
                top = Math.Min(top, screen.Bounds.Top);
                right = Math.Max(right, screen.Bounds.Right);
                bottom = Math.Max(bottom, screen.Bounds.Bottom);
            }

            Left = left;
            Top = top;
            Width = right - left;
            Height = bottom - top;
        }

        private void MakeClickThrough()
        {
            Loaded += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                    exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
            };
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = (int)(hue / 60) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            byte v = (byte)value;
            byte p = (byte)(value * (1 - saturation));
            byte q = (byte)(value * (1 - f * saturation));
            byte t = (byte)(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return Color.FromRgb(v, t, p);
                case 1: return Color.FromRgb(q, v, p);
                case 2: return Color.FromRgb(p, v, t);
                case 3: return Color.FromRgb(p, q, v);
                case 4: return Color.FromRgb(t, p, v);
                default: return Color.FromRgb(v, p, q);
            }
        }
    }

}
