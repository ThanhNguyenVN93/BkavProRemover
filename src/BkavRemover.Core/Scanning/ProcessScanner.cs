using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BkavRemover.Core.Models;

namespace BkavRemover.Core.Scanning
{
    public static class ProcessScanner
    {
        private static readonly Regex NameRegex = new Regex(
            "Bkav|Blu[Pp]ro|^Bka$|Bmail", RegexOptions.IgnoreCase);

        public static List<ProcessFinding> Scan()
        {
            List<ProcessFinding> found = new List<ProcessFinding>();
            int selfId;
            using (Process self = Process.GetCurrentProcess()) selfId = self.Id;

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    // This tool's own exe name contains "Bkav"; never report (and later kill) ourselves.
                    if (p.Id != selfId && NameRegex.IsMatch(p.ProcessName))
                    {
                        string path = null;
                        try { path = p.MainModule.FileName; } catch { /* protected process: name match is enough */ }
                        found.Add(new ProcessFinding { Id = p.Id, Name = p.ProcessName, Path = path });
                    }
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }
            return found;
        }
    }
}
