using System.Management;

namespace BkavRemover.Core
{
    public static class SafeModeHelper
    {
        /// <summary>
        /// Mirrors the script's In-SafeMode check: Win32_ComputerSystem.BootupState
        /// reads "Normal boot", "Fail-safe boot" or "Fail-safe with network boot".
        /// </summary>
        public static bool IsSafeMode()
        {
            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT BootupState FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object val = mo["BootupState"];
                        string state = val == null ? null : val.ToString();
                        return state != null && state != "Normal boot";
                    }
                }
            }
            catch
            {
                // WMI unavailable: assume normal boot rather than block the user.
            }
            return false;
        }

        public static bool EnableSafeBootMinimal(out string error)
        {
            return RunBcdEdit(new[] { "/set", "{current}", "safeboot", "minimal" }, out error);
        }

        public static bool DisableSafeBoot(out string error)
        {
            return RunBcdEdit(new[] { "/deletevalue", "{current}", "safeboot" }, out error);
        }

        private static bool RunBcdEdit(string[] args, out string error)
        {
            Native.ProcessRunner.Result result = Native.ProcessRunner.Run("bcdedit.exe", args, 15000);
            error = result.ExitCode == 0 ? null : result.StdOut + result.StdErr;
            return result.ExitCode == 0;
        }
    }
}
