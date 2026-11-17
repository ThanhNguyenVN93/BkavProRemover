using System;
using System.Runtime.InteropServices;

namespace BkavRemover.Core.Native
{
    internal static class Kernel32
    {
        public const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);

        /// <summary>
        /// Schedules a locked file or empty directory for deletion on next boot,
        /// the same mechanism the original script used via PInvoke from PowerShell.
        /// </summary>
        public static bool ScheduleDeleteOnReboot(string path)
        {
            return MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT);
        }
    }
}
