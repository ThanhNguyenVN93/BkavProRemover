using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;
using Microsoft.Win32;

namespace BkavRemover.Core.Cleaning
{
    public class RegistryCleaner
    {
        private readonly Logger _logger;

        public RegistryCleaner(Logger logger)
        {
            _logger = logger;
        }

        public void Remove(IEnumerable<RegistryKeyFinding> keys, IEnumerable<RegistryValueFinding> values)
        {
            Native.PrivilegeHelper.EnableRegistryPrivileges();
            int keyOk = 0, keyStuck = 0, valueOk = 0;

            foreach (RegistryKeyFinding k in keys)
            {
                if (!k.Selected) continue;
                if (DeleteKey(k.FullKeyPath)) keyOk++;
                else keyStuck++;
            }

            foreach (RegistryValueFinding v in values)
            {
                if (!v.Selected) continue;
                if (DeleteValue(v.FullKeyPath, v.ValueName)) valueOk++;
            }

            _logger.Info(string.Format("Registry: {0} keys removed ({1} still locked), {2} values removed.",
                keyOk, keyStuck, valueOk));
        }

        private bool DeleteKey(string fullKeyPath)
        {
            ProcessRunner.Result result = ProcessRunner.Run("reg.exe", new[] { "delete", fullKeyPath, "/f" }, 15000);
            if (result.ExitCode == 0)
            {
                _logger.Info("Registry key removed: " + fullKeyPath);
                return true;
            }

            ProcessRunner.Result check = ProcessRunner.Run("reg.exe", new[] { "query", fullKeyPath }, 10000);
            if (check.ExitCode != 0)
                return true;

            if (TakeRegistryOwnership(fullKeyPath))
            {
                result = ProcessRunner.Run("reg.exe", new[] { "delete", fullKeyPath, "/f" }, 15000);
                if (result.ExitCode == 0)
                {
                    _logger.Info("Registry key removed (after taking ownership): " + fullKeyPath);
                    return true;
                }
            }

            if (DeleteViaSystemTask(fullKeyPath, null))
            {
                _logger.Info("Registry key removed (via SYSTEM task): " + fullKeyPath);
                return true;
            }

            _logger.Warn("Registry key still locked: " + fullKeyPath);
            return false;
        }

        private bool DeleteValue(string fullKeyPath, string valueName)
        {
            string[] args = string.Equals(valueName, "(Default)", StringComparison.OrdinalIgnoreCase)
                ? new[] { "delete", fullKeyPath, "/ve", "/f" }
                : new[] { "delete", fullKeyPath, "/v", valueName, "/f" };

            ProcessRunner.Result result = ProcessRunner.Run("reg.exe", args, 15000);
            if (result.ExitCode == 0)
            {
                _logger.Info("Registry value removed: " + fullKeyPath + " :: " + valueName);
                return true;
            }

            if (TakeRegistryOwnership(fullKeyPath))
            {
                result = ProcessRunner.Run("reg.exe", args, 15000);
                if (result.ExitCode == 0)
                {
                    _logger.Info("Registry value removed (after taking ownership): " + fullKeyPath + " :: " + valueName);
                    return true;
                }
            }

            if (DeleteViaSystemTask(fullKeyPath, valueName))
            {
                _logger.Info("Registry value removed (via SYSTEM task): " + fullKeyPath + " :: " + valueName);
                return true;
            }

            _logger.Warn("Registry value still locked: " + fullKeyPath + " :: " + valueName);
            return false;
        }

        private bool DeleteViaSystemTask(string fullKeyPath, string valueName)
        {
            string taskName = "BkavRemoverRegClean";
            string regArgs;
            if (valueName == null)
                regArgs = "delete \"" + fullKeyPath + "\" /f";
            else if (string.Equals(valueName, "(Default)", StringComparison.OrdinalIgnoreCase))
                regArgs = "delete \"" + fullKeyPath + "\" /ve /f";
            else
                regArgs = "delete \"" + fullKeyPath + "\" /v \"" + valueName + "\" /f";

            string sysRoot = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            string regExe = System.IO.Path.Combine(sysRoot, @"System32\reg.exe");
            string cmd = "\"" + regExe + "\" " + regArgs;

            ProcessRunner.Run("schtasks.exe", new[] {
                "/create", "/tn", taskName, "/tr", cmd,
                "/sc", "once", "/st", "00:00", "/ru", "SYSTEM", "/f", "/rl", "highest"
            }, 15000);

            ProcessRunner.Run("schtasks.exe", new[] { "/run", "/tn", taskName }, 15000);
            Thread.Sleep(2000);
            ProcessRunner.Run("schtasks.exe", new[] { "/delete", "/tn", taskName, "/f" }, 15000);

            ProcessRunner.Result check = ProcessRunner.Run("reg.exe", new[] { "query", fullKeyPath }, 10000);
            if (valueName == null)
                return check.ExitCode != 0;

            if (check.ExitCode != 0) return true;
            string[] queryArgs = string.Equals(valueName, "(Default)", StringComparison.OrdinalIgnoreCase)
                ? new[] { "query", fullKeyPath, "/ve" }
                : new[] { "query", fullKeyPath, "/v", valueName };
            ProcessRunner.Result valCheck = ProcessRunner.Run("reg.exe", queryArgs, 10000);
            return valCheck.ExitCode != 0;
        }

        private bool TakeRegistryOwnership(string fullKeyPath)
        {
            RegistryKey hive;
            string subKey;
            if (!TrySplit(fullKeyPath, out hive, out subKey)) return false;

            try
            {
                SecurityIdentifier admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                using (RegistryKey key = hive.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                    RegistryRights.TakeOwnership))
                {
                    if (key == null) return false;
                    RegistrySecurity security = key.GetAccessControl(AccessControlSections.Owner);
                    security.SetOwner(admins);
                    key.SetAccessControl(security);
                }

                using (RegistryKey key = hive.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                    RegistryRights.ChangePermissions))
                {
                    if (key == null) return false;
                    RegistrySecurity security = key.GetAccessControl();
                    security.AddAccessRule(new RegistryAccessRule(
                        admins, RegistryRights.FullControl,
                        InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                    key.SetAccessControl(security);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySplit(string fullKeyPath, out RegistryKey hive, out string subKey)
        {
            hive = null;
            subKey = null;
            string[] prefixes = { "HKEY_LOCAL_MACHINE\\", "HKEY_CURRENT_USER\\", "HKEY_USERS\\" };
            RegistryKey[] hives = { Registry.LocalMachine, Registry.CurrentUser, Registry.Users };

            for (int i = 0; i < prefixes.Length; i++)
            {
                if (fullKeyPath.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
                {
                    hive = hives[i];
                    subKey = fullKeyPath.Substring(prefixes[i].Length);
                    return true;
                }
            }
            return false;
        }
    }
}
