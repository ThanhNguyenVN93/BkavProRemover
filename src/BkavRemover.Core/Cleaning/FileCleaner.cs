using System;
using System.Collections.Generic;
using System.IO;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;

namespace BkavRemover.Core.Cleaning
{
    /// <summary>
    /// Removes directories and scatter files with a link-safe walk: junctions and
    /// symlinks are removed as links and never followed, hardlinked files never
    /// get their ACL changed, and ownership changes (takeown/icacls) and
    /// reboot-time deletes are only used inside admin-only locations, where no
    /// standard user could have planted a redirect.
    /// </summary>
    public class FileCleaner
    {
        private readonly Logger _logger;
        private readonly List<string> _pendingReboot = new List<string>();

        public FileCleaner(Logger logger)
        {
            _logger = logger;
        }

        public void Remove(IEnumerable<FileFinding> findings)
        {
            foreach (FileFinding finding in findings)
            {
                if (!finding.Selected) continue;
                if (string.Equals(finding.Path, _logger.LogFilePath, StringComparison.OrdinalIgnoreCase)) continue;
                RemoveTopLevel(finding.Path, finding.IsDirectory);
            }

            foreach (string p in _pendingReboot)
            {
                Kernel32.ScheduleDeleteOnReboot(p);
                _logger.Warn("Locked, scheduled for deletion on reboot: " + p);
            }
        }

        private void RemoveTopLevel(string path, bool isDirectory)
        {
            if (!Directory.Exists(path) && !File.Exists(path)) return;

            if (PathSafety.AncestorIsLink(path))
            {
                _logger.Warn("Skipped (a parent folder is a junction/symlink): " + path);
                return;
            }

            if (PathSafety.IsLink(path))
            {
                if (PathSafety.RemoveLinkOnly(path, isDirectory))
                    _logger.Info("Removed link (target untouched): " + path);
                else
                    _logger.Warn("Could not remove link: " + path);
                return;
            }

            bool trusted = PathSafety.IsUnderTrustedRoot(path);
            bool ok = isDirectory ? DeleteTree(path, trusted) : DeleteFileSafe(path, trusted);

            if (ok) _logger.Info((isDirectory ? "Removed folder: " : "Removed file: ") + path);
            else if (!trusted) _logger.Warn("Could not fully remove (left in place): " + path);
        }

        /// <summary>Post-order walk that removes links as links and never descends into them.</summary>
        private bool DeleteTree(string dir, bool trusted)
        {
            bool allOk = true;
            FileSystemInfo[] entries;
            try
            {
                entries = new DirectoryInfo(dir).GetFileSystemInfos();
            }
            catch (UnauthorizedAccessException)
            {
                if (!trusted) return false;
                TakeOwnershipSingle(dir);
                try { entries = new DirectoryInfo(dir).GetFileSystemInfos(); }
                catch { return QueueReboot(dir, trusted); }
            }
            catch
            {
                return false;
            }

            foreach (FileSystemInfo entry in entries)
            {
                bool isDir = (entry.Attributes & FileAttributes.Directory) != 0;
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0 && PathSafety.IsLink(entry.FullName))
                {
                    if (!PathSafety.RemoveLinkOnly(entry.FullName, isDir)) allOk = false;
                    continue;
                }

                if (isDir) { if (!DeleteTree(entry.FullName, trusted)) allOk = false; }
                else if (!DeleteFileSafe(entry.FullName, trusted)) allOk = false;
            }

            try
            {
                Directory.Delete(dir, false);
                return allOk;
            }
            catch
            {
                if (trusted)
                {
                    TakeOwnershipSingle(dir);
                    try { Directory.Delete(dir, false); return allOk; } catch { }
                }
                QueueReboot(dir, trusted);
                return false;
            }
        }

        private bool DeleteFileSafe(string file, bool trusted)
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                return true;
            }
            catch
            {
            }

            // A hardlink shares its security descriptor with another path; never retarget its ACL.
            if (trusted && PathSafety.HardLinkCount(file) <= 1)
            {
                TakeOwnershipSingle(file);
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    return true;
                }
                catch
                {
                }
            }

            QueueReboot(file, trusted);
            return false;
        }

        // Reboot-time deletes run by path; only safe where no one else can swap a link in before reboot.
        private bool QueueReboot(string path, bool trusted)
        {
            if (trusted) _pendingReboot.Add(path);
            return false;
        }

        private static void TakeOwnershipSingle(string path)
        {
            if (PathSafety.IsLink(path)) return;
            ProcessRunner.Run("takeown.exe", new[] { "/f", path, "/a" }, 15000);
            ProcessRunner.Run("icacls.exe", new[] { path, "/grant", "*S-1-5-32-544:F", "/L", "/q" }, 15000);
        }
    }
}
