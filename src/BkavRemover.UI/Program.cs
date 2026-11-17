using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BkavRemover.UI
{
    internal static class Program
    {
        private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string name);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDefaultDllDirectories(uint flags);

        [STAThread]
        private static void Main(string[] args)
        {
            HardenDllSearchPath();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { ReportCrash(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { ReportCrash(e.ExceptionObject as Exception); };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!BkavRemover.Core.ElevationHelper.IsAdministrator())
            {
                MessageBox.Show(
                    "This tool needs to run as Administrator to detect and remove system services, " +
                    "drivers and registry entries.\n\nPlease right-click the program and choose " +
                    "\"Run as administrator\".",
                    "Administrator rights required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool autoContinue = false;
            foreach (string arg in args)
                if (string.Equals(arg, "--auto-continue", System.StringComparison.OrdinalIgnoreCase))
                    autoContinue = true;

            Application.Run(new MainForm(autoContinue));
        }

        private static void ReportCrash(Exception ex)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BkavRemover_crash.log");
            try
            {
                System.IO.File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    (ex == null ? "(unknown error)" : ex.ToString()) + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }
            try
            {
                MessageBox.Show("An unexpected error occurred:\n\n" + (ex == null ? "(unknown)" : ex.Message) +
                    "\n\nDetails were saved to:\n" + path, "Bkav Pro / BluPro Remover",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Stops later LoadLibrary-by-name calls from picking up a DLL planted next
        /// to the exe (e.g. in Downloads) or in the current directory.
        /// SetDefaultDllDirectories needs Win8+ or Win7 with KB2533623, so it is
        /// probed first; SetDllDirectory("") still removes the current directory.
        /// </summary>
        private static void HardenDllSearchPath()
        {
            try
            {
                IntPtr k32 = GetModuleHandle("kernel32.dll");
                if (k32 != IntPtr.Zero && GetProcAddress(k32, "SetDefaultDllDirectories") != IntPtr.Zero)
                    SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32);
                SetDllDirectory(string.Empty);
            }
            catch
            {
            }
        }
    }
}
