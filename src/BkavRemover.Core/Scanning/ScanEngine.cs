using System.Collections.Generic;
using System.IO;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;

namespace BkavRemover.Core.Scanning
{
    public class ScanEngine
    {
        private readonly Logger _logger;

        public ScanEngine(Logger logger)
        {
            _logger = logger;
        }

        public ScanResult Scan(bool deepRegistry)
        {
            ScanResult result = new ScanResult();
            result.IsAdmin = ElevationHelper.IsAdministrator();
            result.IsSafeMode = SafeModeHelper.IsSafeMode();

            _logger.Info("Scanning services and drivers...");
            result.Services = ServiceScanner.Scan();

            _logger.Info("Scanning processes...");
            result.Processes = ProcessScanner.Scan();

            _logger.Info("Scanning files and folders...");
            result.Files = ScanFiles();

            _logger.Info("Scanning scheduled tasks...");
            result.ScheduledTasks = ScheduledTaskScanner.Scan();

            _logger.Info("Scanning registry (this can take a moment)...");
            RegistryScanOptions regOptions = new RegistryScanOptions { DeepData = deepRegistry };
            RegistryScanResult regResult = RegistryScanner.Scan(regOptions);
            result.RegistryKeys = regResult.Keys;
            result.RegistryValues = regResult.Values;

            _logger.Info(string.Format(
                "Scan complete: {0} services, {1} processes, {2} files/folders, {3} tasks, {4} registry keys, {5} registry values.",
                result.Services.Count, result.Processes.Count, result.Files.Count,
                result.ScheduledTasks.Count, result.RegistryKeys.Count, result.RegistryValues.Count));

            return result;
        }

        private static List<FileFinding> ScanFiles()
        {
            List<FileFinding> findings = new List<FileFinding>();
            List<string> dirs = new List<string>();
            dirs.AddRange(PathCatalog.GetFixedInstallDirs());
            dirs.AddRange(PathCatalog.GetExtraPaths());

            List<string> uniqueDirs = new List<string>();
            foreach (string d in dirs)
            {
                bool dup = false;
                foreach (string u in uniqueDirs)
                    if (string.Equals(u, d, System.StringComparison.OrdinalIgnoreCase)) { dup = true; break; }
                if (!dup && Directory.Exists(d))
                {
                    uniqueDirs.Add(d);
                    findings.Add(new FileFinding { Path = d, IsDirectory = true });
                }
            }

            foreach (string f in PathCatalog.GetScatterFiles())
                if (File.Exists(f))
                    findings.Add(new FileFinding { Path = f, IsDirectory = false });

            return findings;
        }
    }
}
