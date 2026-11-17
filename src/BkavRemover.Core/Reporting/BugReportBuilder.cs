using System;
using System.IO;
using System.Text;

namespace BkavRemover.Core.Reporting
{
    public static class BugReportBuilder
    {
        public static string ReportsFolder
        {
            get
            {
                return Path.Combine(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BkavRemover"),
                    "Reports");
            }
        }

        public static int PendingCount
        {
            get
            {
                string dir = ReportsFolder;
                if (!Directory.Exists(dir)) return 0;
                return Directory.GetDirectories(dir).Length;
            }
        }

        public static string BuildBundle(string subject, string description, string logFilePath, bool includeLog)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string reportDir = Path.Combine(ReportsFolder, "Report_" + stamp);
            Directory.CreateDirectory(reportDir);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Subject: " + (string.IsNullOrEmpty(subject) ? "(no subject)" : subject));
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("OS: " + Environment.OSVersion.VersionString);
            sb.AppendLine("64-bit OS: " + Is64BitOperatingSystem());
            sb.AppendLine("App bitness: " + (IntPtr.Size == 8 ? "x64" : "x86"));
            sb.AppendLine("Machine: " + Environment.MachineName);
            sb.AppendLine("CLR: " + Environment.Version);
            sb.AppendLine();
            sb.AppendLine("Description:");
            sb.AppendLine(description);

            File.WriteAllText(Path.Combine(reportDir, "report.txt"), sb.ToString(), Encoding.UTF8);

            if (includeLog && logFilePath != null && File.Exists(logFilePath))
                File.Copy(logFilePath, Path.Combine(reportDir, Path.GetFileName(logFilePath)), true);

            return reportDir;
        }

        private static bool Is64BitOperatingSystem()
        {
            if (IntPtr.Size == 8) return true;
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
        }
    }
}
