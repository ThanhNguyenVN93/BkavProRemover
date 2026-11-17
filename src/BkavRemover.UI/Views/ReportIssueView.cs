using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using BkavRemover.Core;
using BkavRemover.Core.Reporting;

namespace BkavRemover.UI.Views
{
    internal class ReportIssueView : UserControl
    {
        private TextBox _subject;
        private TextBox _description;
        private CheckBox _attachLog;
        private Label _titleLabel, _subtitleLabel, _sysTitle, _sysInfo;
        private Label _subjLabel, _descLabel, _hint, _pendingLabel;
        private Button _btnSave, _btnLog, _btnReports;
        private Func<string> _getLogFilePath;

        public ReportIssueView()
        {
            BackColor = UiTheme.Background;
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        public void SetLogFileProvider(Func<string> provider)
        {
            _getLogFilePath = provider;
        }

        public void ApplyLanguage()
        {
            _titleLabel.Text = Strings.Get("report.title");
            _subtitleLabel.Text = Strings.Get("report.subtitle");
            _sysTitle.Text = Strings.Get("report.sysinfo");
            _subjLabel.Text = Strings.Get("report.subject");
            _descLabel.Text = Strings.Get("report.description");
            _attachLog.Text = Strings.Get("report.attachlog");
            _btnSave.Text = Strings.Get("report.btn.save");
            _btnLog.Text = Strings.Get("report.btn.log");
            _hint.Text = string.IsNullOrEmpty(AppSettings.ReportIssueUrl)
                ? Strings.Get("report.hint.nogit")
                : Strings.Get("report.hint.git");
            _btnReports.Text = Strings.Get("report.btn.reports");
            UpdatePendingCount();
        }

        private void BuildLayout()
        {
            Panel outer = new Panel { Dock = DockStyle.Fill };
            Panel content = new Panel
            {
                Size = new Size(560, 520),
                AutoScroll = true,
                Anchor = AnchorStyles.None
            };

            _titleLabel = new Label
            {
                Text = Strings.Get("report.title"),
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            _subtitleLabel = new Label
            {
                Text = Strings.Get("report.subtitle"),
                Font = UiTheme.FontRegular,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                Location = new Point(2, 28)
            };

            Panel sysCard = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#EDF2F7"),
                Location = new Point(0, 60),
                Size = new Size(540, 70),
                Padding = new Padding(16, 10, 16, 10)
            };
            Panel sysAccent = new Panel { BackColor = UiTheme.CardAccent1, Dock = DockStyle.Left, Width = 3 };
            string bitness = IntPtr.Size == 8 ? "x64" : "x86";
            string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _sysTitle = new Label
            {
                Text = Strings.Get("report.sysinfo"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 8)
            };
            _sysInfo = new Label
            {
                Text = string.Format("OS: {0}  |  {1}  |  .NET CLR {2}  |  Tool v{3} ({4})",
                    Environment.OSVersion, Environment.MachineName, Environment.Version, ver, bitness),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                Location = new Point(14, 30)
            };
            sysCard.Controls.Add(_sysTitle);
            sysCard.Controls.Add(_sysInfo);
            sysCard.Controls.Add(sysAccent);

            _subjLabel = new Label
            {
                Text = Strings.Get("report.subject"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 148)
            };
            _subject = new TextBox
            {
                Location = new Point(0, 170),
                Size = new Size(540, 26),
                Font = UiTheme.FontRegular,
                BackColor = Color.White
            };

            _descLabel = new Label
            {
                Text = Strings.Get("report.description"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 206)
            };
            _description = new TextBox
            {
                Location = new Point(0, 228),
                Size = new Size(540, 130),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiTheme.FontRegular,
                BackColor = Color.White
            };

            _attachLog = new CheckBox
            {
                Text = Strings.Get("report.attachlog"),
                Checked = true,
                AutoSize = true,
                Font = UiTheme.FontRegular,
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(0, 370)
            };

            _btnSave = new Button
            {
                Text = Strings.Get("report.btn.save"),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                Font = UiTheme.FontBold,
                Size = new Size(140, 36),
                Location = new Point(0, 402),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += Send_Click;

            _btnLog = new Button
            {
                Text = Strings.Get("report.btn.log"),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontBold,
                Size = new Size(150, 36),
                Location = new Point(150, 402),
                Cursor = Cursors.Hand
            };
            _btnLog.FlatAppearance.BorderColor = UiTheme.CardBorder;
            _btnLog.Click += delegate
            {
                string logPath = _getLogFilePath != null ? _getLogFilePath() : null;
                if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
                {
                    string dir = Path.GetDirectoryName(logPath);
                    if (Directory.Exists(dir))
                        Process.Start("explorer.exe", "/select,\"" + logPath + "\"");
                }
                else
                {
                    string logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "BkavRemover");
                    if (Directory.Exists(logDir))
                        Process.Start("explorer.exe", logDir);
                    else
                        MessageBox.Show(Strings.Get("report.nolog"), Strings.Get("dlg.info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _btnReports = new Button
            {
                Text = Strings.Get("report.btn.reports"),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontBold,
                Size = new Size(170, 36),
                Location = new Point(310, 402),
                Cursor = Cursors.Hand
            };
            _btnReports.FlatAppearance.BorderColor = UiTheme.CardBorder;
            _btnReports.Click += delegate
            {
                string dir = BugReportBuilder.ReportsFolder;
                if (Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
                else
                    MessageBox.Show(Strings.Get("report.noreports"), Strings.Get("dlg.info"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            _hint = new Label
            {
                Text = string.IsNullOrEmpty(AppSettings.ReportIssueUrl)
                    ? Strings.Get("report.hint.nogit")
                    : Strings.Get("report.hint.git"),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                Location = new Point(0, 448)
            };

            _pendingLabel = new Label
            {
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                Location = new Point(0, 472)
            };
            UpdatePendingCount();

            content.Controls.Add(_titleLabel);
            content.Controls.Add(_subtitleLabel);
            content.Controls.Add(sysCard);
            content.Controls.Add(_subjLabel);
            content.Controls.Add(_subject);
            content.Controls.Add(_descLabel);
            content.Controls.Add(_description);
            content.Controls.Add(_attachLog);
            content.Controls.Add(_btnSave);
            content.Controls.Add(_btnLog);
            content.Controls.Add(_btnReports);
            content.Controls.Add(_hint);
            content.Controls.Add(_pendingLabel);

            outer.Controls.Add(content);
            outer.Resize += delegate
            {
                content.Location = new Point(
                    Math.Max(0, (outer.ClientSize.Width - content.Width) / 2),
                    Math.Max(16, (outer.ClientSize.Height - content.Height) / 2));
            };

            Controls.Add(outer);
        }

        private void UpdatePendingCount()
        {
            int count = BugReportBuilder.PendingCount;
            _pendingLabel.Text = count > 0
                ? string.Format(Strings.Get("report.pending"), count, BugReportBuilder.ReportsFolder)
                : Strings.Get("report.pending.none");
        }

        private void Send_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(AppSettings.ReportIssueUrl) && SystemActions.OpenUrl(AppSettings.ReportIssueUrl))
                return;

            string logPath = _getLogFilePath != null ? _getLogFilePath() : null;
            string reportDir = BugReportBuilder.BuildBundle(_subject.Text, _description.Text, logPath, _attachLog.Checked);
            UpdatePendingCount();
            _subject.Clear();
            _description.Clear();
            MessageBox.Show(
                string.Format(Strings.Get("report.saved.msg"), reportDir),
                Strings.Get("report.saved.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
