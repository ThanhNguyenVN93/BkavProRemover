using System;
using System.Runtime.InteropServices;

namespace BkavRemover.Core.Native
{
    /// <summary>
    /// Deletes a Windows service via the native Service Control Manager API.
    /// .NET 3.5's ServiceController can stop a service but has no Delete method,
    /// so this fills that one gap without shelling out to sc.exe.
    /// </summary>
    internal static class ServiceDeleter
    {
        private const uint SC_MANAGER_CONNECT = 0x0001;
        private const uint SERVICE_DELETE = 0x10000;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint access);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenService(IntPtr scmHandle, string serviceName, uint access);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DeleteService(IntPtr serviceHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr handle);

        public static bool Delete(string serviceName, out string error)
        {
            error = null;
            IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero)
            {
                error = "OpenSCManager failed (" + Marshal.GetLastWin32Error() + ")";
                return false;
            }

            try
            {
                IntPtr svc = OpenService(scm, serviceName, SERVICE_DELETE);
                if (svc == IntPtr.Zero)
                {
                    error = "OpenService failed (" + Marshal.GetLastWin32Error() + ")";
                    return false;
                }

                try
                {
                    if (!DeleteService(svc))
                    {
                        error = "DeleteService failed (" + Marshal.GetLastWin32Error() + ")";
                        return false;
                    }
                    return true;
                }
                finally
                {
                    CloseServiceHandle(svc);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }
    }
}
