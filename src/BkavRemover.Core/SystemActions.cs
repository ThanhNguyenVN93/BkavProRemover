using System;
using BkavRemover.Core.Native;

namespace BkavRemover.Core
{
    public static class SystemActions
    {
        /// <summary>
        /// Opens an https URL in the user's normal (non-elevated) browser by
        /// handing it to the already-running explorer.exe, instead of starting the
        /// browser as a child of this elevated process.
        /// </summary>
        public static bool OpenUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
                return false;
            ProcessRunner.Run("explorer.exe", new[] { uri.AbsoluteUri }, 10000);
            return true;
        }

        public static void Reboot(int delaySeconds, string reason)
        {
            ProcessRunner.Run("shutdown.exe",
                new[] { "/r", "/t", delaySeconds.ToString(), "/c", reason }, 10000);
        }
    }
}
