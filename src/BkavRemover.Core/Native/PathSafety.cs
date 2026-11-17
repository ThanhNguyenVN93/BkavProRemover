using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BkavRemover.Core.Native
{
    /// <summary>
    /// Guards every elevated file operation against junctions, symlinks and
    /// hardlinks that a non-admin user could plant in user-writable locations
    /// to redirect a delete or ACL change onto protected system files.
    /// </summary>
    public static class PathSafety
    {
        // A "name surrogate" reparse point (junction, mount point, symlink, ...) redirects
        // to another path. Non-surrogate tags (e.g. OneDrive cloud placeholders) do not.
        private const uint NameSurrogateBit = 0x20000000;
        private const int FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

        private const uint FILE_READ_ATTRIBUTES = 0x80;
        private const uint FILE_SHARE_ALL = 0x7;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public int dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint dwVolumeSerialNumber;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint nNumberOfLinks;
            public uint nFileIndexHigh;
            public uint nFileIndexLow;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFile(string fileName, out WIN32_FIND_DATA data);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint share,
            IntPtr security, uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out BY_HANDLE_FILE_INFORMATION info);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool RemoveDirectory(string path);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteFile(string path);

        /// <summary>True if this exact entry is a redirecting reparse point (junction/symlink).</summary>
        public static bool IsLink(string path)
        {
            WIN32_FIND_DATA data;
            IntPtr h = FindFirstFile(path.TrimEnd('\\'), out data);
            if (h == new IntPtr(-1)) return false;
            FindClose(h);
            if ((data.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) == 0) return false;
            return (data.dwReserved0 & NameSurrogateBit) != 0;
        }

        /// <summary>True if any parent directory of the path is a junction/symlink.</summary>
        public static bool AncestorIsLink(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full);
            DirectoryInfo parent = Directory.GetParent(full.TrimEnd('\\'));
            while (parent != null && !string.Equals(parent.FullName.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                if (IsLink(parent.FullName)) return true;
                parent = parent.Parent;
            }
            return false;
        }

        /// <summary>Number of hardlinks to a file; >1 means changing its ACL would also change another path's.</summary>
        public static uint HardLinkCount(string filePath)
        {
            using (SafeFileHandle h = CreateFile(filePath, FILE_READ_ATTRIBUTES, FILE_SHARE_ALL, IntPtr.Zero,
                OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero))
            {
                if (h.IsInvalid) return 0;
                BY_HANDLE_FILE_INFORMATION info;
                return GetFileInformationByHandle(h, out info) ? info.nNumberOfLinks : 0;
            }
        }

        /// <summary>Removes only the link entry itself; never touches what it points to.</summary>
        public static bool RemoveLinkOnly(string path, bool isDirectory)
        {
            return isDirectory ? RemoveDirectory(path) : DeleteFile(path);
        }

        /// <summary>
        /// Locations only administrators can write to, so nobody else can plant
        /// a link inside them. Ownership changes and reboot-time deletes are
        /// limited to these.
        /// </summary>
        public static bool IsUnderTrustedRoot(string path)
        {
            string full = Path.GetFullPath(path);
            string windir = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            // Not the whole Windows folder: parts of it (e.g. Windows\Temp) are user-writable.
            string[] roots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("ProgramW6432"),
                Path.Combine(windir, @"System32\drivers"),
                Path.Combine(windir, "ELAMBKUP")
            };
            string sysWow = Path.Combine(windir, "SysWOW64");
            if (string.Equals(Path.GetDirectoryName(full), sysWow, StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (string r in roots)
            {
                if (string.IsNullOrEmpty(r)) continue;
                string root = r.TrimEnd('\\') + "\\";
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
