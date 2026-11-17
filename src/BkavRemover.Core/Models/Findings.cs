using System.Collections.Generic;

namespace BkavRemover.Core.Models
{
    public class ServiceFinding
    {
        public string Name;
        public string DisplayName;
        public string State;
        public string PathName;
        public bool Selected = true;
    }

    public class ProcessFinding
    {
        public int Id;
        public string Name;
        public string Path;
        public bool Selected = true;
    }

    public class FileFinding
    {
        public string Path;
        public bool IsDirectory;
        public bool Selected = true;
    }

    public class RegistryKeyFinding
    {
        public string FullKeyPath; // e.g. HKEY_LOCAL_MACHINE\SOFTWARE\...
        public bool Selected = true;
    }

    public class RegistryValueFinding
    {
        public string FullKeyPath;
        public string ValueName;
        public string ValueType;
        public string Data;
        public bool Selected = true;
    }

    public class ScheduledTaskFinding
    {
        public string TaskName;
        public string TaskPath;
        public bool Selected = true;
    }

    public class ScanResult
    {
        public bool IsAdmin;
        public bool IsSafeMode;
        public List<ServiceFinding> Services = new List<ServiceFinding>();
        public List<ProcessFinding> Processes = new List<ProcessFinding>();
        public List<FileFinding> Files = new List<FileFinding>();
        public List<RegistryKeyFinding> RegistryKeys = new List<RegistryKeyFinding>();
        public List<RegistryValueFinding> RegistryValues = new List<RegistryValueFinding>();
        public List<ScheduledTaskFinding> ScheduledTasks = new List<ScheduledTaskFinding>();

        public bool HasKernelDriver
        {
            get
            {
                foreach (ServiceFinding s in Services)
                {
                    if (s.PathName != null && s.PathName.IndexOf(".sys", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            }
        }

        public int TotalCount
        {
            get
            {
                return Services.Count + Processes.Count + Files.Count +
                       RegistryKeys.Count + RegistryValues.Count + ScheduledTasks.Count;
            }
        }

        public bool IsClean
        {
            get { return TotalCount == 0; }
        }
    }
}
