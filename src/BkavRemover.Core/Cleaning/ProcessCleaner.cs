using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;

namespace BkavRemover.Core.Cleaning
{
    public class ProcessCleaner
    {
        private readonly Logger _logger;

        public ProcessCleaner(Logger logger)
        {
            _logger = logger;
        }

        private static readonly Regex BkavPattern = new Regex("Bka|BluPro", RegexOptions.IgnoreCase);

        public void KillByName()
        {
            int myPid = Process.GetCurrentProcess().Id;
            List<string> stubborn = new List<string>();

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.Id == myPid) continue;
                    if (!BkavPattern.IsMatch(p.ProcessName)) continue;

                    string name = p.ProcessName;
                    int pid = p.Id;
                    try
                    {
                        p.Kill();
                        _logger.Info(string.Format("Killed: {0} (PID {1})", name, pid));
                    }
                    catch (Exception)
                    {
                        if (!stubborn.Contains(name))
                            stubborn.Add(name);
                    }
                }
                catch (Exception) { }
                finally { p.Dispose(); }
            }

            foreach (string name in stubborn)
                ForceKill(name);

            if (stubborn.Count > 0)
            {
                Thread.Sleep(1000);
                ForceKillViaSystem();
                Thread.Sleep(500);
                RetryKillAll(myPid);
            }
        }

        private void ForceKill(string processName)
        {
            ProcessRunner.Result r = ProcessRunner.Run("taskkill.exe",
                new[] { "/f", "/t", "/im", processName + ".exe" }, 15000);
            if (r.ExitCode == 0)
                _logger.Info("Force-killed (taskkill /f /t): " + processName);
            else
                _logger.Warn("taskkill failed for " + processName + ": " + r.StdOut.Trim());
        }

        private void ForceKillViaSystem()
        {
            string taskName = "BkavRemoverKill";
            string sysRoot = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            string taskkillExe = System.IO.Path.Combine(sysRoot, @"System32\taskkill.exe");

            string[] targets = { "Bka.exe", "BkavPro.exe", "BkavService.exe",
                "BkavSystemServer.exe", "BkavSystemService.exe", "BkavSystemService64.exe",
                "BluProService.exe", "BkavUtil.exe" };

            foreach (string target in targets)
            {
                string cmd = "\"" + taskkillExe + "\" /f /t /im " + target;
                ProcessRunner.Run("schtasks.exe", new[] {
                    "/create", "/tn", taskName, "/tr", cmd,
                    "/sc", "once", "/st", "00:00", "/ru", "SYSTEM", "/f", "/rl", "highest"
                }, 10000);
                ProcessRunner.Result r = ProcessRunner.Run("schtasks.exe",
                    new[] { "/run", "/tn", taskName }, 10000);
                if (r.ExitCode == 0)
                    _logger.Info("SYSTEM kill scheduled: " + target);
            }

            Thread.Sleep(2000);
            ProcessRunner.Run("schtasks.exe", new[] { "/delete", "/tn", taskName, "/f" }, 10000);
        }

        private void RetryKillAll(int myPid)
        {
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.Id == myPid) continue;
                    if (!BkavPattern.IsMatch(p.ProcessName)) continue;

                    string name = p.ProcessName;
                    int pid = p.Id;
                    try { p.Kill(); } catch (Exception) { }

                    ProcessRunner.Run("taskkill.exe",
                        new[] { "/f", "/pid", pid.ToString() }, 10000);
                    _logger.Warn(string.Format("Still alive after all attempts: {0} (PID {1})", name, pid));
                }
                catch (Exception) { }
                finally { p.Dispose(); }
            }
        }

        public void Kill(IEnumerable<ProcessFinding> findings)
        {
            foreach (ProcessFinding finding in findings)
            {
                if (!finding.Selected) continue;
                try
                {
                    using (Process p = Process.GetProcessById(finding.Id))
                    {
                        if (string.Equals(p.ProcessName, finding.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill();
                            _logger.Info(string.Format("Killed: {0} (PID {1})", finding.Name, finding.Id));
                        }
                    }
                }
                catch (ArgumentException)
                {
                }
                catch (Exception ex)
                {
                    _logger.Warn(string.Format("Could not kill {0} (PID {1}): {2}", finding.Name, finding.Id, ex.Message));
                    ForceKill(finding.Name);
                }
            }
        }
    }
}
