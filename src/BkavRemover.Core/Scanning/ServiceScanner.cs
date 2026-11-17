using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using BkavRemover.Core.Models;

namespace BkavRemover.Core.Scanning
{
    public static class ServiceScanner
    {
        private static readonly Regex NameRegex = new Regex("Bkav|Blu[Pp]ro", RegexOptions.IgnoreCase);

        public static List<ServiceFinding> Scan()
        {
            List<ServiceFinding> found = new List<ServiceFinding>();
            Dictionary<string, string> imagePaths = ReadServiceImagePaths();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // GetServices() returns Win32 services; GetDevices() returns kernel/file-system drivers.
            // Bkav installs both, and HasKernelDriver needs the .sys entries to trigger Safe Mode.
            ServiceController[][] lists = { ServiceController.GetServices(), ServiceController.GetDevices() };
            foreach (ServiceController[] list in lists)
            {
                foreach (ServiceController sc in list)
                {
                    try
                    {
                        if (seen.Contains(sc.ServiceName)) continue;

                        string path;
                        imagePaths.TryGetValue(sc.ServiceName, out path);
                        bool nameMatches = NameRegex.IsMatch(sc.ServiceName);
                        bool pathMatches = path != null && (path.IndexOf("BkavPro", StringComparison.OrdinalIgnoreCase) >= 0
                            || path.IndexOf("BluPro", StringComparison.OrdinalIgnoreCase) >= 0
                            || path.IndexOf("Bkav Corporation", StringComparison.OrdinalIgnoreCase) >= 0);

                        if (nameMatches || pathMatches)
                        {
                            seen.Add(sc.ServiceName);
                            found.Add(new ServiceFinding
                            {
                                Name = sc.ServiceName,
                                DisplayName = sc.DisplayName,
                                State = sc.Status.ToString(),
                                PathName = path
                            });
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        sc.Dispose();
                    }
                }
            }
            return found;
        }

        private static Dictionary<string, string> ReadServiceImagePaths()
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (RegistryKey servicesRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
            {
                if (servicesRoot == null) return map;
                foreach (string name in servicesRoot.GetSubKeyNames())
                {
                    using (RegistryKey key = servicesRoot.OpenSubKey(name))
                    {
                        if (key == null) continue;
                        object imagePath = key.GetValue("ImagePath");
                        if (imagePath != null) map[name] = imagePath.ToString();
                    }
                }
            }
            return map;
        }

        /// <summary>Service registry subkeys whose name itself matches Bkav/BluPro.</summary>
        public static List<string> ScanServiceKeys()
        {
            List<string> keys = new List<string>();
            using (RegistryKey servicesRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
            {
                if (servicesRoot == null) return keys;
                foreach (string name in servicesRoot.GetSubKeyNames())
                    if (NameRegex.IsMatch(name)) keys.Add(name);
            }
            return keys;
        }
    }
}
