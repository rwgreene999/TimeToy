using System;
using System.Windows;
using TimeToy.Properties;

namespace TimeToy
{
    /// <summary>
    /// Helpers to capture, apply and persist WindowSettings using WPF APIs only.
    /// Does not depend on System.Windows.Forms/System.Drawing.
    /// </summary>
    public static class WindowSettingsManager
    {
        // Apply stored WindowSettings to an actual Window (safe: skips NaN values).
        // If saved position is off-screen (e.g. multi-monitor -> single monitor change),
        // this method recenters the window on the primary work area and clamps size.
        public static void ApplyToWindow(Window window, WindowSettings settings)
        {
            try
            {
                if (window == null) throw new ArgumentNullException(nameof(window));
                if (settings == null) return;

                // Apply size first so later checks use meaningful values
                if (!double.IsNaN(settings.Width) && settings.Width > 0) window.Width = settings.Width;
                if (!double.IsNaN(settings.Height) && settings.Height > 0) window.Height = settings.Height;

                bool hasPosition = !double.IsNaN(settings.Left) && !double.IsNaN(settings.Top);

                if (hasPosition)
                {
                    double useWidth = !double.IsNaN(settings.Width) && settings.Width > 0 ? settings.Width : window.Width;
                    double useHeight = !double.IsNaN(settings.Height) && settings.Height > 0 ? settings.Height : window.Height;

                    // Saved rect in WPF device-independent units (DIPs)
                    var savedRect = new Rect(settings.Left, settings.Top, useWidth, useHeight);

                    // Virtual screen bounds (DIPs) and primary work area (DIPs)
                    var virtualScreen = new Rect(
                        SystemParameters.VirtualScreenLeft,
                        SystemParameters.VirtualScreenTop,
                        SystemParameters.VirtualScreenWidth,
                        SystemParameters.VirtualScreenHeight);

                    var primaryWork = SystemParameters.WorkArea;

                    // If saved rect intersects the virtual screen, restore; otherwise fallback
                    if (savedRect.IntersectsWith(virtualScreen))
                    {
                        window.Left = settings.Left;
                        window.Top = settings.Top;
                    }
                    else
                    {
                        // Center on primary work area and clamp size
                        double desiredW = Math.Min(savedRect.Width, primaryWork.Width);
                        double desiredH = Math.Min(savedRect.Height, primaryWork.Height);

                        double left = primaryWork.Left + (primaryWork.Width - desiredW) / 2;
                        double top = primaryWork.Top + (primaryWork.Height - desiredH) / 2;

                        window.Left = left;
                        window.Top = top;

                        window.Width = Math.Min(window.Width, primaryWork.Width);
                        window.Height = Math.Min(window.Height, primaryWork.Height);
                    }
                }

                // Restore window state after position/size to avoid WPF quirks
                if (!string.IsNullOrWhiteSpace(settings.IsMaximized) &&
                    Enum.TryParse(settings.IsMaximized, out WindowState state))
                {
                    window.WindowState = state;
                }
                else
                {
                    window.WindowState = WindowState.Normal;
                }

            }
            catch (Exception ex)
            {
                ErrorLogging.Log(ex, "Error applying WindowSettings to Window.");
            }
        }

        // Capture current window position/size/state into WindowSettings.
        // Use RestoreBounds when window is maximized/minimized to get the user's restore location.
        public static void CaptureFromWindow(Window window, WindowSettings settings)
        {
            try
            {
                if (window.WindowState == WindowState.Maximized || window.WindowState == WindowState.Minimized)
                {
                    var r = window.RestoreBounds;
                    settings.Top = r.Top;
                    settings.Left = r.Left;
                    settings.Width = r.Width;
                    settings.Height = r.Height;
                }
                else
                {
                    settings.Top = window.Top;
                    settings.Left = window.Left;
                    settings.Width = window.Width;
                    settings.Height = window.Height;
                }

                settings.IsMaximized = window.WindowState.ToString();

            }
            catch (Exception ex)
            {

                ErrorLogging.Log(ex, "Error capturing WindowSettings from Window.");
            }
        }

    }
}