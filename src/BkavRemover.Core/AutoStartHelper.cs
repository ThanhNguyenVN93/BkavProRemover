using Microsoft.Win32;

namespace BkavRemover.Core
{
    /// <summary>
    /// Registers (and unregisters) the tool in HKLM RunOnce so it starts
    /// automatically after the next reboot.  The "*" prefix on the value
    /// name tells Windows to process it even in Safe Mode.
    /// </summary>
    public static class AutoStartHelper
    {
        private const string RunOnceKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string ValueName = "*BkavRemover";

        public static void RegisterRunOnce(string exePath)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunOnceKey, true))
            {
                if (key != null)
                    key.SetValue(ValueName, "\"" + exePath + "\" --auto-continue");
            }
        }

        public static void UnregisterRunOnce()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunOnceKey, true))
                {
                    if (key != null) key.DeleteValue(ValueName, false);
                }
            }
            catch { }
        }
    }
}
