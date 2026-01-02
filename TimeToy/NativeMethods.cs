using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TimeToy
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // added constant
        internal const int SW_RESTORE = 9;
        internal const int SW_SHOW = 5;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_SHOWWINDOW = 0x0040;

        // Force the specified window handle to the foreground. Safe, static helper.
        public static void ForceForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return;

            try
            {
                var foreground = GetForegroundWindow();
                uint fgThread = GetWindowThreadProcessId(foreground, out _);
                uint currentThread = GetCurrentThreadId();

                bool attached = false;
                if (fgThread != currentThread)
                {
                    attached = AttachThreadInput(fgThread, currentThread, true);
                }

                ShowWindow(hWnd, SW_RESTORE);
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                SetForegroundWindow(hWnd);
                BringWindowToTop(hWnd);

                if (attached)
                {
                    AttachThreadInput(fgThread, currentThread, false);
                }
            }
            catch (Exception ex)
            {
                try { ErrorLogging.Log(ex, "ForceForegroundWindow failed"); } catch { /* swallow to avoid secondary failure */ }
            }
        }

        // Convenience overload that accepts a WPF Window.
        public static void ForceForegroundWindow(Window window)
        {
            if (window == null)
                return;
            var helper = new WindowInteropHelper(window);
            ForceForegroundWindow(helper.Handle);
        }

        // Remove topmost status for the specified window handle.
        public static void UnforceForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return;

            try
            {
                // Use HWND_NOTOPMOST to restore normal z-order. Keep position/size and ensure window is shown.
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            }
            catch (Exception ex)
            {
                try { ErrorLogging.Log(ex, "UnforceForegroundWindow failed"); } catch { }
            }
        }

        // Convenience overload that accepts a WPF Window.
        public static void UnforceForegroundWindow(Window window)
        {
            if (window == null)
                return;
            var helper = new WindowInteropHelper(window);
            UnforceForegroundWindow(helper.Handle);
        }


        public static void RestoreForegroundWindow(IntPtr targetHwnd)
        {
            if (targetHwnd == IntPtr.Zero)
                return;

            try
            {
                // Attach input between current thread and target's thread so SetForegroundWindow is allowed.
                uint targetThread = GetWindowThreadProcessId(targetHwnd, out _);
                uint currentThread = GetCurrentThreadId();

                bool attached = false;
                if (targetThread != currentThread)
                {
                    attached = AttachThreadInput(targetThread, currentThread, true);
                }

                // Best-effort restore focus
                SetForegroundWindow(targetHwnd);

                if (attached)
                {
                    AttachThreadInput(targetThread, currentThread, false);
                }
            }
            catch (Exception ex)
            {
                try { ErrorLogging.Log(ex, "RestoreForegroundWindow failed"); } catch { }
            }
        }

        public static void RestoreForegroundWindow(Window window)
        {
            if (window == null)
                return;
            var helper = new WindowInteropHelper(window);
            RestoreForegroundWindow(helper.Handle);
        }



    }
}

