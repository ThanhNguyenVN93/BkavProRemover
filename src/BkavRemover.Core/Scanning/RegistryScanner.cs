using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BkavRemover.Core.Models;
using BkavRemover.Core.Native;

namespace BkavRemover.Core.Scanning
{
    public class RegistryScanOptions
    {
        public bool DeepData;
        public string[] Patterns = { "Bkav", "BluPro" };
        public string[] Hives = { "HKLM", "HKCU", "HKU" };
    }

    public class RegistryScanResult
    {
        public List<RegistryKeyFinding> Keys = new List<RegistryKeyFinding>();
        public List<RegistryValueFinding> Values = new List<RegistryValueFinding>();
    }

    /// <summary>
    /// Ports the original script's Scan-Registry function: a fast key-name sweep
    /// of each hive, a name+data sweep of a short list of well-known autostart /
    /// uninstall / shell-extension locations, and a check of CLSID/AppID/TypeLib
    /// "GUID parent" keys whose child data references Bkav/BluPro. Shells out to
    /// reg.exe (via ProcessRunner, arguments passed as a vector) exactly as the
    /// script did, so behavior matches what was already verified by hand.
    /// </summary>
    public static class RegistryScanner
    {
        private static readonly string[] HotKeys =
        {
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
            @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules",
            @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlers",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved",
            @"HKLM\SOFTWARE\Microsoft\Security Center\Provider"
        };

        private static readonly string[] GuidParents =
        {
            @"HKLM\SOFTWARE\Classes\AppID",
            @"HKLM\SOFTWARE\Classes\CLSID",
            @"HKLM\SOFTWARE\Classes\TypeLib",
            @"HKLM\SOFTWARE\Classes\WOW6432Node\AppID",
            @"HKLM\SOFTWARE\Classes\WOW6432Node\TypeLib",
            @"HKLM\SOFTWARE\WOW6432Node\Classes\AppID",
            @"HKLM\SOFTWARE\WOW6432Node\Classes\TypeLib",
            @"HKLM\SOFTWARE\Microsoft\Security Center\Provider\Av",
            @"HKLM\SOFTWARE\Microsoft\Security Center\Provider\Fw"
        };

        private static readonly Regex KeyNameRegex = new Regex(@"(?i)(^|\\)([^\\]*(?:Bkav|BluPro)[^\\]*)$");
        private static readonly Regex ValueLineRegex = new Regex(@"^\s{2,}(.+?)\s+(REG_[A-Z_]+)\s*(.*)$");
        private static readonly Regex KeyLineRegex = new Regex(@"^HKEY_");

        public static RegistryScanResult Scan(RegistryScanOptions options)
        {
            RegistryScanResult result = new RegistryScanResult();
            HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> seenValueSigs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string hive in options.Hives)
            {
                foreach (string pattern in options.Patterns)
                {
                    foreach (string line in QueryLines(new[] { hive, "/f", pattern, "/s", "/k" }))
                    {
                        if (!KeyLineRegex.IsMatch(line)) continue;
                        string key = line.Trim();
                        if (KeyNameRegex.IsMatch(key)) AddKey(result, seenKeys, key);
                    }

                    if (options.DeepData)
                    {
                        ParseValueLines(QueryLines(new[] { hive, "/f", pattern, "/s", "/v" }), result, seenKeys, seenValueSigs);
                        ParseValueLines(QueryLines(new[] { hive, "/f", pattern, "/s", "/d" }), result, seenKeys, seenValueSigs);
                    }
                }
            }

            foreach (string hotKey in HotKeys)
            {
                foreach (string pattern in options.Patterns)
                {
                    ParseValueLines(QueryLines(new[] { hotKey, "/f", pattern, "/s", "/v" }), result, seenKeys, seenValueSigs);
                    ParseValueLines(QueryLines(new[] { hotKey, "/f", pattern, "/s", "/d" }), result, seenKeys, seenValueSigs);
                }
            }

            foreach (string guidParent in GuidParents)
            {
                string fullParent = ToFullHiveName(guidParent);
                foreach (string pattern in options.Patterns)
                {
                    foreach (string line in QueryLines(new[] { guidParent, "/f", pattern, "/s", "/d" }))
                    {
                        if (!KeyLineRegex.IsMatch(line)) continue;
                        string key = line.Trim();
                        if (key.Length <= fullParent.Length) continue;
                        string rel = key.Substring(fullParent.Length + 1);
                        int backslash = rel.IndexOf('\\');
                        if (backslash >= 0) rel = rel.Substring(0, backslash);
                        AddKey(result, seenKeys, fullParent + "\\" + rel);
                    }
                }
            }

            return result;
        }

        private static readonly Regex LegacyEnumRegex = new Regex(
            @"\\SYSTEM\\(ControlSet\d+|CurrentControlSet)\\Enum\\Root\\LEGACY_", RegexOptions.IgnoreCase);

        private static void AddKey(RegistryScanResult result, HashSet<string> seen, string fullKey)
        {
            if (LegacyEnumRegex.IsMatch(fullKey)) return;
            if (seen.Add(fullKey))
                result.Keys.Add(new RegistryKeyFinding { FullKeyPath = fullKey });
        }

        private static void ParseValueLines(IEnumerable<string> lines, RegistryScanResult result,
            HashSet<string> seenKeys, HashSet<string> seenValueSigs)
        {
            string currentKey = null;
            foreach (string line in lines)
            {
                if (KeyLineRegex.IsMatch(line)) { currentKey = line.Trim(); continue; }
                if (currentKey == null) continue;

                Match m = ValueLineRegex.Match(line);
                if (!m.Success) continue;

                if (IsUnderAnyFoundKey(currentKey, seenKeys)) continue;

                string valueName = m.Groups[1].Value.Trim();
                string valueType = m.Groups[2].Value;
                string data = m.Groups[3].Value.Trim();
                string sig = currentKey + "|" + valueName;
                if (seenValueSigs.Add(sig))
                {
                    result.Values.Add(new RegistryValueFinding
                    {
                        FullKeyPath = currentKey,
                        ValueName = valueName,
                        ValueType = valueType,
                        Data = data
                    });
                }
            }
        }

        private static bool IsUnderAnyFoundKey(string key, HashSet<string> foundKeys)
        {
            foreach (string k in foundKeys)
                if (string.Equals(key, k, StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith(k + "\\", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string ToFullHiveName(string shortPath)
        {
            if (shortPath.StartsWith(@"HKLM\")) return "HKEY_LOCAL_MACHINE" + shortPath.Substring(4);
            if (shortPath.StartsWith(@"HKCU\")) return "HKEY_CURRENT_USER" + shortPath.Substring(4);
            if (shortPath.StartsWith(@"HKU\")) return "HKEY_USERS" + shortPath.Substring(3);
            return shortPath;
        }

        private static IEnumerable<string> QueryLines(string[] args)
        {
            List<string> fullArgs = new List<string>(args.Length + 1);
            fullArgs.Add("query");
            fullArgs.AddRange(args);
            ProcessRunner.Result result = ProcessRunner.Run("reg.exe", fullArgs, 60000);
            return result.StdOut.Split('\n');
        }
    }
}
