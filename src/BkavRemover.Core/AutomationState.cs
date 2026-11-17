using System;
using System.IO;

namespace BkavRemover.Core
{
    /// <summary>
    /// Persists the current automation phase to disk so the tool can pick up
    /// where it left off after a reboot.  Uses ProgramData so the file is
    /// accessible regardless of which user logs in after the Safe-Mode reboot.
    /// </summary>
    public static class AutomationState
    {
        public const string PrepareDone = "prepare_done";
        public const string CleanDone = "clean_done";

        private static readonly string StateDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BkavRemover");

        private static readonly string StatePath = Path.Combine(StateDir, "autostate.txt");

        public static void Save(string phase)
        {
            if (!Directory.Exists(StateDir)) Directory.CreateDirectory(StateDir);
            File.WriteAllText(StatePath, phase + "|" + DateTime.UtcNow.ToString("o"));
        }

        public static string Load()
        {
            if (!File.Exists(StatePath)) return null;
            try
            {
                string content = File.ReadAllText(StatePath).Trim();
                string[] parts = content.Split('|');
                if (parts.Length >= 2)
                {
                    DateTime ts = DateTime.Parse(parts[1]).ToUniversalTime();
                    if ((DateTime.UtcNow - ts).TotalHours > 24) { Clear(); return null; }
                }
                return parts[0];
            }
            catch { Clear(); return null; }
        }

        public static void Clear()
        {
            try { if (File.Exists(StatePath)) File.Delete(StatePath); } catch { }
        }
    }
}
