using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BkavRemover.Core.Native
{
    /// <summary>
    /// Runs a fixed, allow-listed system executable with arguments passed as a
    /// vector (never a single concatenated command string), so no argument can
    /// break out into the shell. UseShellExecute is always false and no cmd.exe
    /// is ever involved.
    /// </summary>
    public static class ProcessRunner
    {
        public class Result
        {
            public int ExitCode;
            public string StdOut = string.Empty;
            public string StdErr = string.Empty;
        }

        public static Result Run(string exeName, IEnumerable<string> args, int timeoutMs)
        {
            string exePath = ResolveSystemExe(exeName);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exePath;
            psi.Arguments = BuildArgumentString(args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            Result result = new Result();
            using (Process p = new Process())
            {
                p.StartInfo = psi;
                StringBuilder outBuf = new StringBuilder();
                StringBuilder errBuf = new StringBuilder();
                p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) outBuf.AppendLine(e.Data); };
                p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) errBuf.AppendLine(e.Data); };
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                }
                // WaitForExit(int) returns before the async stdout/stderr readers hit EOF;
                // the parameterless overload waits for them. Disposing first crashes the reader thread.
                try { p.WaitForExit(); } catch { }
                result.ExitCode = SafeExitCode(p);
                result.StdOut = outBuf.ToString();
                result.StdErr = errBuf.ToString();
            }
            return result;
        }

        private static int SafeExitCode(Process p)
        {
            try { return p.ExitCode; } catch { return -1; }
        }

        /// <summary>
        /// Only ever resolves to a binary inside the Windows system directories -
        /// never a user-writable path - so a malicious file placed elsewhere
        /// (e.g. current directory) can never be launched in its place.
        /// </summary>
        private static string ResolveSystemExe(string exeName)
        {
            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string candidate = System.IO.Path.Combine(system32, exeName);
            if (System.IO.File.Exists(candidate)) return candidate;

            string windows = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            candidate = System.IO.Path.Combine(windows, exeName);
            if (System.IO.File.Exists(candidate)) return candidate;

            throw new InvalidOperationException("Required system tool not found: " + exeName);
        }

        private static string BuildArgumentString(IEnumerable<string> args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string a in args)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(QuoteArgument(a));
            }
            return sb.ToString();
        }

        // Windows CommandLineToArgvW-compatible quoting.
        private static string QuoteArgument(string arg)
        {
            if (arg == null) arg = string.Empty;
            if (arg.Length > 0 && arg.IndexOfAny(new char[] { ' ', '\t', '\n', '\v', '"' }) < 0)
                return arg;

            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            int backslashes = 0;
            foreach (char c in arg)
            {
                if (c == '\\')
                {
                    backslashes++;
                }
                else if (c == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                    backslashes = 0;
                    continue;
                }
                else
                {
                    sb.Append('\\', backslashes);
                    backslashes = 0;
                    sb.Append(c);
                }
            }
            sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }
    }
}
