using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace TimeToy
{
    internal static class ErrorLogging
    {
        private static readonly object _sync = new object();
        private const long MaxLogFileBytes = 1 * 1024 * 1024; // 1 MB
        private const string LogFileName = "TimeToy.log";

        /// <summary>
        /// Log an exception with optional additional message.
        /// </summary>
        public static void Log(Exception ex, string additionalInfo = null)
        {
            if (ex == null)
            {
                Log(additionalInfo ?? "Null exception passed to Log(Exception, string)");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR");
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                sb.AppendLine($"Message: {additionalInfo}");
            }
            sb.AppendLine($"Exception: {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine("StackTrace:");
            sb.AppendLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                sb.AppendLine("InnerException:");
                sb.AppendLine(ex.InnerException.ToString());
            }
            sb.AppendLine(new string('-', 80));

            WriteLog(sb.ToString());
        }

        /// <summary>
        /// Log a plain text message.
        /// </summary>
        public static void Log(string message)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] INFO");
            sb.AppendLine(message ?? string.Empty);
            sb.AppendLine(new string('-', 80));

            WriteLog(sb.ToString());
        }

        private static void WriteLog(string text)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(text);


                var folder = AppDomain.CurrentDomain.BaseDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(folder))
                {
                    // Fallback to current directory
                    folder = Environment.CurrentDirectory;
                }

                var logPath = Path.Combine(folder, LogFileName);

                lock (_sync)
                {
                    try
                    {
                        if (File.Exists(logPath))
                        {
                            var fi = new FileInfo(logPath);
                            if (fi.Length > MaxLogFileBytes)
                            {
                                // Create a timestamped backup and start a new log file.
                                var backupName = Path.Combine(folder, $"TimeToy.log.bak");
                                try
                                {
                                    if (File.Exists(backupName))
                                    {
                                        File.Delete(backupName);
                                    }
                                }
                                catch
                                {
                                    // ignore - best effort to remove existing backup
                                }

                                try
                                {
                                    // Prefer move to preserve attributes; if that fails, copy and truncate original.
                                    File.Move(logPath, backupName);
                                }
                                catch
                                {
                                    try
                                    {
                                        File.Copy(logPath, backupName, true);
                                        File.WriteAllText(logPath, string.Empty, Encoding.UTF8);
                                    }
                                    catch
                                    {
                                        // If backup/truncate fails, fall through and attempt to append anyway.
                                    }
                                }
                            }
                        }
                        // Ensure directory exists (should be true for exe folder, but safe)
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        File.AppendAllText(logPath, text, Encoding.UTF8);
                    }
                    catch
                    {
                        // Swallow to avoid throwing from logger. Best effort only.
                        try
                        {
                            // As a last resort write to Debug output so it can be observed in development.
                            System.Diagnostics.Debug.WriteLine("Failed to write to log file.");
                            System.Diagnostics.Debug.WriteLine(text);
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                // Never let logging throw.
            }
        }
    }
}