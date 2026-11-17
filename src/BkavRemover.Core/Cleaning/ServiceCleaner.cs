using System;
using System.Collections.Generic;
using System.ServiceProcess;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;
using Microsoft.Win32;

namespace BkavRemover.Core.Cleaning
{
    public class ServiceCleaner
    {
        private readonly Logger _logger;

        public ServiceCleaner(Logger logger)
        {
            _logger = logger;
        }

        public void Remove(IEnumerable<ServiceFinding> findings)
        {
            foreach (ServiceFinding finding in findings)
            {
                if (!finding.Selected) continue;
                StopAndDelete(finding.Name);
            }
            RemoveOrphanServiceKeys();
        }

        /// <summary>Phase 1 (Prepare): disable start without touching anything else.</summary>
        public void DisableStartup(IEnumerable<ServiceFinding> findings)
        {
            foreach (ServiceFinding finding in findings)
            {
                try
                {
                    SetStartType(finding.Name, 4 /* SERVICE_DISABLED */);
                    _logger.Info("Disabled: " + finding.Name);
                }
                catch (Exception ex)
                {
                    _logger.Warn("Could not disable " + finding.Name + ": " + ex.Message);
                }
            }
        }

        private void StopAndDelete(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    string stateBefore = "Unknown";
                    try { stateBefore = sc.Status.ToString(); } catch { }

                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        try
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn("Could not stop " + serviceName + ": " + ex.Message);
                        }
                    }

                    string error;
                    if (ServiceDeleter.Delete(serviceName, out error))
                        _logger.Info("Service removed: " + serviceName + " [was " + stateBefore + "]");
                    else
                        _logger.Warn("Service delete failed for " + serviceName + ": " + error);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Service " + serviceName + " error: " + ex.Message);
            }
        }

        private static void SetStartType(string serviceName, int startType)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\" + serviceName, true))
            {
                if (key != null) key.SetValue("Start", startType, RegistryValueKind.DWord);
            }
        }

        private void RemoveOrphanServiceKeys()
        {
            foreach (string name in Scanning.ServiceScanner.ScanServiceKeys())
            {
                string subKey = @"SYSTEM\CurrentControlSet\Services\" + name;
                try
                {
                    using (RegistryKey check = Registry.LocalMachine.OpenSubKey(subKey))
                    {
                        if (check == null) continue; // Already deleted by SCM above.
                    }
                    Registry.LocalMachine.DeleteSubKeyTree(subKey);
                    _logger.Info("Service key removed: " + name);
                }
                catch (Exception ex)
                {
                    _logger.Warn("Could not remove service key " + name + ": " + ex.Message);
                }
            }
        }
    }
}
