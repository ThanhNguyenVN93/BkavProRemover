using System.Threading;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Scanning;

namespace BkavRemover.Core.Cleaning
{
    public class RemovalOrchestrator
    {
        private readonly Logger _logger;

        public RemovalOrchestrator(Logger logger)
        {
            _logger = logger;
        }

        public bool Prepare(ScanResult scan, out string error)
        {
            error = null;
            if (!scan.IsAdmin) { error = "Administrator privileges are required."; return false; }
            if (scan.IsSafeMode) { error = "Already in Safe Mode - run Clean instead."; return false; }

            _logger.Info("=== PHASE 1: preparing for Safe Mode ===");

            _logger.Info("Killing Bkav processes to remove self-protection...");
            ProcessCleaner pc = new ProcessCleaner(_logger);
            pc.Kill(scan.Processes);
            pc.KillByName();
            Thread.Sleep(1500);
            pc.KillByName();

            _logger.Info("Disabling Bkav services and drivers...");
            new ServiceCleaner(_logger).DisableStartup(scan.Services);

            string bcdError;
            if (SafeModeHelper.EnableSafeBootMinimal(out bcdError))
            {
                _logger.Info("Safe boot (minimal) enabled.");
            }
            else
            {
                error = "bcdedit failed: " + bcdError;
                _logger.Error(error);
                return false;
            }

            _logger.Info("PHASE 1 done -> reboot into Safe Mode, then run Clean.");
            return true;
        }

        public bool Clean(ScanResult scan, out string error)
        {
            error = null;
            if (!scan.IsAdmin) { error = "Administrator privileges are required."; return false; }

            _logger.Info("=== PHASE 2: cleaning Bkav/BluPro ===");
            if (!scan.IsSafeMode && scan.HasKernelDriver)
                _logger.Warn("Not running in Safe Mode - the kernel driver may block some removals.");

            new ProcessCleaner(_logger).Kill(scan.Processes);
            new ProcessCleaner(_logger).KillByName();
            new ServiceCleaner(_logger).Remove(scan.Services);
            new FileCleaner(_logger).Remove(scan.Files);
            new ScheduledTaskCleaner(_logger).Remove(scan.ScheduledTasks);
            new RegistryCleaner(_logger).Remove(scan.RegistryKeys, scan.RegistryValues);

            string bcdError;
            if (SafeModeHelper.DisableSafeBoot(out bcdError))
                _logger.Info("Safe boot disabled.");

            _logger.Info("PHASE 2 done -> reboot back to normal Windows, then re-scan to verify.");
            return true;
        }
    }
}
