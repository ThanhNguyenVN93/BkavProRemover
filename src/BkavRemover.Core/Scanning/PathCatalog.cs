using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BkavRemover.Core.Scanning
{
    /// <summary>
    /// Builds the same set of candidate install/leftover paths the original
    /// script enumerated: fixed Program Files / ProgramData / AppData locations,
    /// Start Menu shortcuts, temp folders, and a scan across every user profile.
    /// </summary>
    public static class PathCatalog
    {
        private static readonly string[] NamePatterns = { "Bkav", "BluPro" };

        public static List<string> GetFixedInstallDirs()
        {
            List<string> dirs = new List<string>();
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (string.IsNullOrEmpty(pf86)) pf86 = pf; // 32-bit OS: no separate x86 folder.
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            AddIfSet(dirs, pf, "BkavPro");
            AddIfSet(dirs, pf86, "BkavPro");
            AddIfSet(dirs, pf, "BluPro");
            AddIfSet(dirs, pf86, "BluPro");
            AddIfSet(dirs, pf, "Bkav Corporation");
            AddIfSet(dirs, pf86, "Bkav Corporation");
            AddIfSet(dirs, programData, "Bkav");
            AddIfSet(dirs, programData, "BkavPro");
            AddIfSet(dirs, programData, "BluPro");
            AddIfSet(dirs, localAppData, "BluPro");
            AddIfSet(dirs, localAppData, "Bkav");
            AddIfSet(dirs, appData, "Bkav");
            AddIfSet(dirs, appData, "BluPro");

            string smProgramData = Path.Combine(programData, @"Microsoft\Windows\Start Menu\Programs");
            string smAppData = Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs");
            AddIfSet(dirs, smProgramData, "Bkav Pro 2026 Internet Security");
            AddIfSet(dirs, smProgramData, "Bkav Pro Internet Security");
            AddIfSet(dirs, smAppData, "Bkav Pro 2026 Internet Security");
            AddIfSet(dirs, smAppData, "Bkav Pro Internet Security");

            return dirs;
        }

        public static List<string> GetExtraPaths()
        {
            List<string> extra = new List<string>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string temp = Path.GetTempPath().TrimEnd('\\');

            AddMatchingDirs(extra, localAppData, "Bkav*");
            AddMatchingDirs(extra, temp, "Bkav*");

            string tempParent = Directory.GetParent(temp) == null ? null : Directory.GetParent(temp).FullName;
            if (tempParent != null)
                AddMatchingDirs(extra, Path.Combine(tempParent, "Temp"), "Bkav*");

            foreach (string desk in DesktopFolders())
                AddShortcuts(extra, desk);

            string smRoot = CommonStartMenuRoot();
            AddMatchingDirs(extra, smRoot, "*Bkav*");
            AddMatchingDirs(extra, smRoot, "*BluPro*");
            string smUser = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            AddMatchingDirs(extra, smUser, "*Bkav*");
            AddMatchingDirs(extra, smUser, "*BluPro*");

            foreach (string profileDir in EnumerateUserProfiles())
            {
                AddIfExists(extra, Path.Combine(profileDir, @"AppData\Roaming\Bkav"));
                AddIfExists(extra, Path.Combine(profileDir, @"AppData\Roaming\BluPro"));
                AddIfExists(extra, Path.Combine(profileDir, @"AppData\Local\Bkav"));
                AddIfExists(extra, Path.Combine(profileDir, @"AppData\Local\BluPro"));
                AddMatchingDirs(extra, Path.Combine(profileDir, @"AppData\Local\Temp"), "Bkav*");
                AddShortcuts(extra, Path.Combine(profileDir, "Desktop"));
            }

            List<string> unique = new List<string>();
            foreach (string p in extra)
                if (!unique.Contains(p, StringComparer.OrdinalIgnoreCase))
                    unique.Add(p);
            return unique;
        }

        public static List<string> GetScatterFiles()
        {
            List<string> files = new List<string>();
            string sysDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
            string sysRoot = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            string temp = Path.GetTempPath();

            AddMatchingFiles(files, sysDrive + "\\", "bkav*");
            AddMatchingFiles(files, Path.Combine(sysRoot, @"System32\drivers"), "Bkav*");
            AddMatchingFiles(files, Path.Combine(sysRoot, "SysWOW64"), "Bkav*");
            AddMatchingFiles(files, Path.Combine(sysRoot, "ELAMBKUP"), "Bkav*");

            AddMatchingFiles(files, temp, "BkavWhatsNew*");
            AddMatchingFiles(files, temp, "BkavSafeMode_*");
            AddMatchingFiles(files, temp, "BkavUninstall_*");

            string smRoot = CommonStartMenuRoot();
            if (Directory.Exists(smRoot)) AddMatchingFiles(files, smRoot, "*Bkav*");

            string quickLaunch = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            AddMatchingFiles(files, quickLaunch, "*Bkav*");

            foreach (string profileDir in EnumerateUserProfiles())
            {
                string uql = Path.Combine(profileDir, @"AppData\Roaming\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                AddMatchingFiles(files, uql, "*Bkav*");
                string uTemp = Path.Combine(profileDir, @"AppData\Local\Temp");
                AddMatchingFiles(files, uTemp, "BkavWhatsNew*");
                AddMatchingFiles(files, uTemp, "BkavSafeMode_*");
                AddMatchingFiles(files, uTemp, "BkavUninstall_*");
                string cacheDir = Path.Combine(profileDir, @"AppData\Local\Packages\Microsoft.Windows.Search_cw5n1h2txyewy\LocalState\AppIconCache\100");
                AddMatchingFiles(files, cacheDir, "*Bkav*");
            }

            List<string> unique = new List<string>();
            foreach (string p in files)
                if (!unique.Contains(p, StringComparer.OrdinalIgnoreCase))
                    unique.Add(p);
            return unique;
        }

        private static IEnumerable<string> DesktopFolders()
        {
            string publicProfile = Environment.GetEnvironmentVariable("Public");
            if (!string.IsNullOrEmpty(publicProfile))
                yield return Path.Combine(publicProfile, "Desktop");
            yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        private static string CommonStartMenuRoot()
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, @"Microsoft\Windows\Start Menu");
        }

        private static IEnumerable<string> EnumerateUserProfiles()
        {
            // "C:" + "Users" via Path.Combine gives the drive-relative "C:Users"; root it explicitly.
            string usersRoot = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\') + @"\Users";
            if (!Directory.Exists(usersRoot)) yield break;

            string[] excluded = { "Public", "Default", "Default User", "All Users" };
            foreach (string dir in Directory.GetDirectories(usersRoot))
            {
                string name = Path.GetFileName(dir);
                bool skip = false;
                foreach (string ex in excluded)
                    if (string.Equals(ex, name, StringComparison.OrdinalIgnoreCase)) { skip = true; break; }
                if (!skip) yield return dir;
            }
        }

        private static void AddIfSet(List<string> list, string baseDir, string child)
        {
            if (string.IsNullOrEmpty(baseDir)) return;
            AddIfExists(list, Path.Combine(baseDir, child));
        }

        private static void AddIfExists(List<string> list, string path)
        {
            if (Directory.Exists(path)) list.Add(path);
        }

        private static void AddMatchingDirs(List<string> list, string baseDir, string pattern)
        {
            if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) return;
            try
            {
                foreach (string d in Directory.GetDirectories(baseDir, pattern))
                    list.Add(d);
            }
            catch { /* access denied / transient IO: skip, never abort the scan */ }
        }

        private static void AddMatchingFiles(List<string> list, string baseDir, string pattern)
        {
            if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) return;
            try
            {
                foreach (string f in Directory.GetFiles(baseDir, pattern))
                    list.Add(f);
            }
            catch { }
        }

        private static readonly string[] ShortcutExtensions = { ".lnk", ".url", ".exe" };

        private static void AddShortcuts(List<string> list, string desktopDir)
        {
            if (string.IsNullOrEmpty(desktopDir) || !Directory.Exists(desktopDir)) return;
            try
            {
                foreach (string f in Directory.GetFiles(desktopDir, "*Bkav*"))
                    if (HasShortcutExtension(f)) list.Add(f);
                foreach (string f in Directory.GetFiles(desktopDir, "*My Bkav*"))
                    if (HasShortcutExtension(f)) list.Add(f);
            }
            catch { }
        }

        private static bool HasShortcutExtension(string path)
        {
            string ext = Path.GetExtension(path);
            foreach (string e in ShortcutExtensions)
                if (string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
