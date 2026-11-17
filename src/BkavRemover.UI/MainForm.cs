using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using BkavRemover.Core;
using BkavRemover.Core.Cleaning;
using BkavRemover.Core.Logging;
using BkavRemover.Core.Models;
using BkavRemover.Core.Scanning;
using BkavRemover.UI.Views;

namespace BkavRemover.UI
{
    internal class MainForm : Form
    {
        private readonly Logger _logger = new Logger();
        private readonly bool _autoContinue;
        private ScanResult _lastScan;

        private Panel _content;
        private readonly DashboardView _dashboard = new DashboardView();
        private readonly CleaningView _cleaning = new CleaningView();
        private readonly ReportIssueView _report = new ReportIssueView();
        private readonly DonateView _donate = new DonateView();
        private readonly AboutView _about = new AboutView();

        private Button _navDashboard, _navReport, _navDonate, _navAbout, _navLang;

        public MainForm(bool autoContinue)
        {
            _autoContinue = autoContinue;

            Text = "Bkav Pro / BluPro Remover";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            MinimumSize = new Size(900, 600);
            Size = new Size(980, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Background;
            Font = UiTheme.FontRegular;

            BuildLayout();
            WireEvents();

            ShowView(_dashboard);
            Shown += delegate { RunScan(); };
        }

        private void BuildLayout()
        {
            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 120, BackColor = UiTheme.Sidebar };

            Label logo = new Label
            {
                Text = "B",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UiTheme.Accent,
                Size = new Size(48, 48),
                Location = new Point(36, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label appLabel = new Label
            {
                Text = "Bkav Remover",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = ColorTranslator.FromHtml("#6B7A96"),
                Size = new Size(120, 18),
                Location = new Point(0, 72),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _navDashboard = MakeNavButton(Strings.Get("nav.scan"), 108);
            _navReport = MakeNavButton(Strings.Get("nav.report"), 156);
            _navDonate = MakeNavButton(Strings.Get("nav.donate"), 204);
            _navAbout = MakeNavButton(Strings.Get("nav.about"), 252);

            _navLang = new Button
            {
                Text = Strings.Get("nav.lang"),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#26324A"),
                ForeColor = ColorTranslator.FromHtml("#A0B0C8"),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Size = new Size(50, 28),
                Location = new Point(35, 310),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _navLang.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#3A4A66");
            _navLang.FlatAppearance.BorderSize = 1;
            _navLang.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#344060");

            sidebar.Controls.Add(logo);
            sidebar.Controls.Add(appLabel);
            sidebar.Controls.Add(_navDashboard);
            sidebar.Controls.Add(_navReport);
            sidebar.Controls.Add(_navDonate);
            sidebar.Controls.Add(_navAbout);
            sidebar.Controls.Add(_navLang);

            _content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };

            Controls.Add(_content);
            Controls.Add(sidebar);
        }

        private Button MakeNavButton(string text, int top)
        {
            Button b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Sidebar,
                ForeColor = ColorTranslator.FromHtml("#8692AC"),
                Font = new Font("Segoe UI", 8.5f),
                Size = new Size(106, 40),
                Location = new Point(7, top),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#26324A");
            return b;
        }

        private void WireEvents()
        {
            _navDashboard.Click += delegate { ShowView(_dashboard); SetActiveNav(_navDashboard); };
            _navReport.Click += delegate { ShowView(_report); SetActiveNav(_navReport); };
            _navDonate.Click += delegate { ShowView(_donate); SetActiveNav(_navDonate); };
            _navAbout.Click += delegate { ShowView(_about); SetActiveNav(_navAbout); };
            SetActiveNav(_navDashboard);

            _navLang.Click += delegate
            {
                Strings.Current = Strings.Current == Strings.Lang.En ? Strings.Lang.Vi : Strings.Lang.En;
            };
            Strings.LanguageChanged += delegate { ApplyLanguage(); };

            _dashboard.ScanRequested += delegate { RunScan(); };
            _dashboard.CleanRequested += Dashboard_CleanRequested;
            _cleaning.FinishRequested += Cleaning_FinishRequested;
            _report.SetLogFileProvider(delegate { return _logger.LogFilePath; });

            _logger.LineLogged += Logger_LineLogged;
        }

        private void ApplyLanguage()
        {
            _navDashboard.Text = Strings.Get("nav.scan");
            _navReport.Text = Strings.Get("nav.report");
            _navDonate.Text = Strings.Get("nav.donate");
            _navAbout.Text = Strings.Get("nav.about");
            _navLang.Text = Strings.Get("nav.lang");

            _dashboard.ApplyLanguage();
            _cleaning.ApplyLanguage();
            _report.ApplyLanguage();
            _donate.ApplyLanguage();
            _about.ApplyLanguage();
        }

        private void SetActiveNav(Button active)
        {
            Button[] all = { _navDashboard, _navReport, _navDonate, _navAbout };
            foreach (Button b in all)
            {
                bool isActive = b == active;
                b.BackColor = isActive ? UiTheme.Accent : UiTheme.Sidebar;
                b.ForeColor = isActive ? Color.White : ColorTranslator.FromHtml("#8692AC");
            }
        }

        private void SetNavEnabled(bool enabled)
        {
            _navDashboard.Enabled = enabled;
            _navReport.Enabled = enabled;
            _navDonate.Enabled = enabled;
            _navAbout.Enabled = enabled;
        }

        private void ShowView(Control view)
        {
            _content.Controls.Clear();
            _content.Controls.Add(view);
        }

        private void Logger_LineLogged(object sender, LogEventArgs e)
        {
            Color color;
            switch (e.Level)
            {
                case LogLevel.Warn: color = ColorTranslator.FromHtml("#F0C56B"); break;
                case LogLevel.Error: color = ColorTranslator.FromHtml("#F08B82"); break;
                default: color = ColorTranslator.FromHtml("#9FE6C8"); break;
            }
            string line = string.Format("[{0:HH:mm:ss}] [{1}] {2}", e.Timestamp, e.Level.ToString().ToUpperInvariant(), e.Message);
            if (!IsHandleCreated || IsDisposed) return;
            if (InvokeRequired)
                BeginInvoke(new Action<string, Color>(_cleaning.AppendLog), line, color);
            else
                _cleaning.AppendLog(line, color);
        }

        // ---------------- Scan ----------------

        private void RunScan()
        {
            SetNavEnabled(false);
            _dashboard.SetBusy(true);

            Stopwatch sw = Stopwatch.StartNew();
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs e)
            {
                e.Result = new ScanEngine(_logger).Scan(false);
            };
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
            {
                sw.Stop();
                if (e.Error != null)
                {
                    _logger.Error("Scan failed: " + e.Error.Message);
                    SetNavEnabled(true);
                    _dashboard.SetBusy(false);
                    if (!_autoContinue)
                        MessageBox.Show(this, string.Format(Strings.Get("dlg.scanfail"), e.Error.Message),
                            Strings.Get("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _lastScan = (ScanResult)e.Result;
                _dashboard.ShowResult(_lastScan);
                _dashboard.SetScanDuration(sw.Elapsed);

                if (_autoContinue && HandleAutoContinue())
                    return;

                SetNavEnabled(true);
                _dashboard.SetBusy(false);
            };
            worker.RunWorkerAsync();
        }

        private bool HandleAutoContinue()
        {
            string phase = AutomationState.Load();

            if (phase == AutomationState.PrepareDone && _lastScan.IsSafeMode)
            {
                _logger.Info("Auto-continue: Prepare done, now in Safe Mode. Starting Clean phase.");
                _dashboard.ApplySelectionTo(_lastScan);
                RunClean();
                return true;
            }

            if (phase == AutomationState.CleanDone && !_lastScan.IsSafeMode)
            {
                _logger.Info("Auto-continue: Clean done, back in Normal Mode. Showing verification.");
                AutomationState.Clear();
                AutoStartHelper.UnregisterRunOnce();

                if (_lastScan.IsClean)
                    _logger.Info("Verification: system is clean. All Bkav components removed.");
                else
                    _logger.Warn("Verification: " + _lastScan.TotalCount + " item(s) still found.");

                return false;
            }

            if (phase != null)
            {
                _logger.Warn("Auto-continue: unexpected state '" + phase + "' (SafeMode=" + _lastScan.IsSafeMode + "). Clearing.");
                AutomationState.Clear();
                AutoStartHelper.UnregisterRunOnce();
            }
            return false;
        }

        // ---------------- Clean / Prepare ----------------

        private void Dashboard_CleanRequested(object sender, EventArgs e)
        {
            if (_lastScan == null || _lastScan.TotalCount == 0) return;
            _dashboard.ApplySelectionTo(_lastScan);

            bool needsSafeMode = !_lastScan.IsSafeMode && _lastScan.HasKernelDriver;

            if (needsSafeMode && _dashboard.AutoSafeMode)
            {
                DialogResult confirm = MessageBox.Show(this,
                    Strings.Get("dlg.confirm.safemode"),
                    Strings.Get("dlg.confirm.safemode.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                RunPrepare();
                return;
            }

            if (!needsSafeMode)
            {
                DialogResult proceed = MessageBox.Show(this,
                    string.Format(Strings.Get("dlg.confirm.clean"), _lastScan.TotalCount),
                    Strings.Get("dlg.confirm.clean.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (proceed != DialogResult.Yes) return;

                RunClean();
                return;
            }

            DialogResult manual = MessageBox.Show(this,
                string.Format(Strings.Get("dlg.confirm.manual"), _lastScan.TotalCount),
                Strings.Get("dlg.confirm.clean.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (manual != DialogResult.Yes) return;

            RunClean();
        }

        private void RunPrepare()
        {
            SetNavEnabled(false);
            ShowView(_cleaning);
            SetActiveNav(null);
            _cleaning.Reset(Strings.Get("clean.prepare"));

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs e)
            {
                string error;
                bool ok = new RemovalOrchestrator(_logger).Prepare(_lastScan, out error);
                e.Result = new object[] { ok, error };
            };
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
            {
                object[] r = (object[])e.Result;
                bool ok = (bool)r[0];
                string error = (string)r[1];

                if (!ok)
                {
                    SetNavEnabled(true);
                    _cleaning.Complete(false, "Preparation failed: " + error);
                    _logger.Error("Prepare failed: " + error);
                    return;
                }

                _cleaning.Complete(true, "Prepare done. Rebooting into Safe Mode in 10 seconds...");
                _logger.Info("Prepare complete. Saving state and scheduling auto-restart.");

                AutomationState.Save(AutomationState.PrepareDone);
                AutoStartHelper.RegisterRunOnce(Application.ExecutablePath);
                _logger.Info("RunOnce registered. Rebooting into Safe Mode...");

                Timer rebootTimer = new Timer();
                rebootTimer.Interval = 3000;
                rebootTimer.Tick += delegate
                {
                    rebootTimer.Stop();
                    rebootTimer.Dispose();
                    SystemActions.Reboot(5, "Restarting into Safe Mode for Bkav removal");
                };
                rebootTimer.Start();
            };
            worker.RunWorkerAsync();
        }

        private void RunClean()
        {
            SetNavEnabled(false);
            ShowView(_cleaning);
            SetActiveNav(null);
            _cleaning.Reset(_lastScan.IsSafeMode
                ? Strings.Get("clean.safe")
                : Strings.Get("clean.normal"));

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs e)
            {
                string error;
                bool ok = new RemovalOrchestrator(_logger).Clean(_lastScan, out error);
                e.Result = new object[] { ok, error };
            };
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
            {
                object[] r = (object[])e.Result;
                bool ok = (bool)r[0];
                string error = (string)r[1];

                if (!ok)
                {
                    SetNavEnabled(true);
                    _cleaning.Complete(false, "Cleanup failed: " + error);
                    _logger.Error("Clean failed: " + error);
                    AutomationState.Clear();
                    AutoStartHelper.UnregisterRunOnce();
                    return;
                }

                if (_lastScan.IsSafeMode)
                {
                    _cleaning.Complete(true, "Clean done. Rebooting to Normal Mode in 10 seconds...");
                    _logger.Info("Clean complete in Safe Mode. Saving state and scheduling reboot to Normal.");

                    AutomationState.Save(AutomationState.CleanDone);
                    AutoStartHelper.RegisterRunOnce(Application.ExecutablePath);

                    Timer rebootTimer = new Timer();
                    rebootTimer.Interval = 3000;
                    rebootTimer.Tick += delegate
                    {
                        rebootTimer.Stop();
                        rebootTimer.Dispose();
                        SystemActions.Reboot(5, "Restarting to Normal Mode after Bkav cleanup");
                    };
                    rebootTimer.Start();
                }
                else
                {
                    SetNavEnabled(true);
                    _cleaning.Complete(true, "Cleanup finished. Click 'Back to Dashboard' to verify.");
                    _logger.Info("Clean complete in Normal Mode.");
                    AutomationState.Clear();
                    AutoStartHelper.UnregisterRunOnce();
                }
            };
            worker.RunWorkerAsync();
        }

        private void Cleaning_FinishRequested(object sender, EventArgs e)
        {
            ShowView(_dashboard);
            SetActiveNav(_navDashboard);
            RunScan();
        }
    }
}
