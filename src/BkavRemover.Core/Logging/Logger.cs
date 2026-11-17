using System;
using System.IO;
using System.Text;

namespace BkavRemover.Core.Logging
{
    public enum LogLevel { Info, Warn, Error }

    public class LogEventArgs : EventArgs
    {
        public readonly LogLevel Level;
        public readonly string Message;
        public readonly DateTime Timestamp;

        public LogEventArgs(LogLevel level, string message)
        {
            Level = level;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Writes to a per-run log file in %TEMP% and raises an event so the UI
    /// can mirror each line, mirroring the original script's Log function.
    /// </summary>
    public class Logger
    {
        public readonly string LogFilePath;
        public event EventHandler<LogEventArgs> LineLogged;

        public Logger()
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            LogFilePath = Path.Combine(Path.GetTempPath(), "BkavRemoval_" + stamp + ".log");
        }

        public void Info(string message) { Write(LogLevel.Info, message); }
        public void Warn(string message) { Write(LogLevel.Warn, message); }
        public void Error(string message) { Write(LogLevel.Error, message); }

        private void Write(LogLevel level, string message)
        {
            string line = string.Format("[{0}] [{1}] {2}", DateTime.Now.ToString("HH:mm:ss"), level.ToString().ToUpperInvariant(), message);
            try
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Best effort only; never let logging failures abort a cleanup step.
            }

            EventHandler<LogEventArgs> handler = LineLogged;
            if (handler != null) handler(this, new LogEventArgs(level, message));
        }
    }
}
