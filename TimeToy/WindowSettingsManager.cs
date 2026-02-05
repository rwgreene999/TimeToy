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
                if (window == null && settings == null )
                {
                    ErrorLogging.Log($"Request to set window {window?.Title ?? "null Window" }, settings { (settings is null ? "ok," : "settings null" ) }. "); 
                }
                    
                if (SettingsWouldBeOnScreen( settings) ) 
                {
                    window.Left = settings.Left;
                    window.Top = settings.Top;
                }
                else
                {
                }

                // NOTE: restoring state led to weird results, like opening a timer and it opens minimized 
                //// Restore window state after position/size to avoid WPF quirks
                //if (!string.IsNullOrWhiteSpace(settings.IsMaximized) &&
                //    Enum.TryParse(settings.IsMaximized, out WindowState state))
                //{
                //    window.WindowState = state;
                //}
                //else
                //{
                //    window.WindowState = WindowState.Normal;
                //}
                window.WindowState = WindowState.Normal;

            }
            catch (Exception ex)
            {
                ErrorLogging.Log(ex, $"Error applying WindowSettings to Window {window.Title}.");
            }
        }

        public static bool SettingsWouldBeOnScreen(WindowSettings settings, double minVisiblePixels = 16)
        {
            if (settings == null) return false;

            // Need a position to test
            if (double.IsNaN(settings.Left) || double.IsNaN(settings.Top))
                return false;

            // If size is unset, use a sensible default so we can test visibility
            double useWidth = !double.IsNaN(settings.Width) && settings.Width > 0 ? settings.Width : 100.0;
            double useHeight = !double.IsNaN(settings.Height) && settings.Height > 0 ? settings.Height : 100.0;

            var savedRect = new Rect(settings.Left, settings.Top, useWidth, useHeight);
            var virtualScreen = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);

            if (!savedRect.IntersectsWith(virtualScreen))
                return false;

            // Compute overlap area and require at least minVisiblePixels to consider "on screen"
            double overlapW = Math.Min(savedRect.Right, virtualScreen.Right) - Math.Max(savedRect.Left, virtualScreen.Left);
            double overlapH = Math.Min(savedRect.Bottom, virtualScreen.Bottom) - Math.Max(savedRect.Top, virtualScreen.Top);

            if (overlapW <= 0 || overlapH <= 0) return false;

            return (overlapW * overlapH) >= minVisiblePixels;
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