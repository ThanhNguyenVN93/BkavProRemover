using System;
using System.Collections.Generic;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;

namespace BkavRemover.Core.Scanning
{
    /// <summary>
    /// Enumerates scheduled tasks via schtasks.exe (CSV output), the same tool
    /// the shell has always offered for this - no COM task-scheduler interop
    /// needed and no PowerShell dependency.
    /// </summary>
    public static class ScheduledTaskScanner
    {
        public static List<ScheduledTaskFinding> Scan()
        {
            List<ScheduledTaskFinding> found = new List<ScheduledTaskFinding>();
            ProcessRunner.Result result = ProcessRunner.Run(
                "schtasks.exe", new[] { "/query", "/fo", "CSV", "/nh" }, 20000);
            if (result.ExitCode != 0) return found;

            foreach (string rawLine in result.StdOut.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;
                string[] cols = SplitCsvLine(line);
                if (cols.Length == 0) continue;
                string taskPath = cols[0]; // e.g. \Bkav\SafeRun
                if (taskPath.IndexOf("Bkav", StringComparison.OrdinalIgnoreCase) < 0 &&
                    taskPath.IndexOf("BluPro", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                int lastSlash = taskPath.LastIndexOf('\\');
                string name = lastSlash >= 0 ? taskPath.Substring(lastSlash + 1) : taskPath;
                string folder = lastSlash > 0 ? taskPath.Substring(0, lastSlash + 1) : "\\";
                found.Add(new ScheduledTaskFinding { TaskName = name, TaskPath = folder });
            }
            return found;
        }

        private static string[] SplitCsvLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            System.Text.StringBuilder current = new System.Text.StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == ',' && !inQuotes) { fields.Add(current.ToString()); current.Length = 0; continue; }
                current.Append(c);
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}
