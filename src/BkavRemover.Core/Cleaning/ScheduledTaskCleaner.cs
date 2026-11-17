using System.Collections.Generic;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;

namespace BkavRemover.Core.Cleaning
{
    public class ScheduledTaskCleaner
    {
        private readonly Logger _logger;

        public ScheduledTaskCleaner(Logger logger)
        {
            _logger = logger;
        }

        public void Remove(IEnumerable<ScheduledTaskFinding> findings)
        {
            foreach (ScheduledTaskFinding finding in findings)
            {
                if (!finding.Selected) continue;
                string fullName = finding.TaskPath.TrimEnd('\\') + "\\" + finding.TaskName;
                ProcessRunner.Result result = ProcessRunner.Run(
                    "schtasks.exe", new[] { "/delete", "/tn", fullName, "/f" }, 15000);
                if (result.ExitCode == 0)
                    _logger.Info("Scheduled task removed: " + fullName);
                else
                    _logger.Warn("Could not remove scheduled task " + fullName + ": " + result.StdErr.Trim());
            }
        }
    }
}
