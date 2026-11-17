using System;
using System.Drawing;
using System.Windows.Forms;

namespace BkavRemover.UI.Views
{
    internal class CleaningView : UserControl
    {
        public event EventHandler FinishRequested;

        private Label _title;
        private Label _subtitle;
        private ProgressBar _progress;
        private RichTextBox _log;
        private Button _btnFinish;

        public CleaningView()
        {
            BackColor = UiTheme.Background;
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        public void ApplyLanguage()
        {
            _title.Text = Strings.Get("clean.title");
            if (_btnFinish.Enabled)
                _btnFinish.Text = Strings.Get("clean.btn.back");
        }

        private void BuildLayout()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(28, 18, 28, 0) };
            _title = new Label { Text = Strings.Get("clean.title"), Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, AutoSize = true };
            _subtitle = new Label { Text = "", Font = UiTheme.FontSmall, ForeColor = UiTheme.TextSecondary, AutoSize = true, Location = new Point(2, 26) };
            header.Controls.Add(_title);
            header.Controls.Add(_subtitle);

            _progress = new ProgressBar { Dock = DockStyle.Top, Height = 10, Margin = new Padding(28, 10, 28, 0), Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30 };
            Panel progressWrap = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(28, 10, 28, 0) };
            progressWrap.Controls.Add(_progress);

            _log = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.LogBackground,
                ForeColor = Color.White,
                Font = UiTheme.FontMono,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(28, 10, 28, 10)
            };
            Panel logWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 10, 28, 10) };
            logWrap.Controls.Add(_log);

            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(28, 10, 28, 10) };
            _btnFinish = new Button
            {
                Text = Strings.Get("clean.btn.working"),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                Font = UiTheme.FontBold,
                Size = new Size(170, 34),
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnFinish.FlatAppearance.BorderSize = 0;
            _btnFinish.Click += delegate { EventHandler h = FinishRequested; if (h != null) h(this, EventArgs.Empty); };
            footer.Controls.Add(_btnFinish);
            footer.Resize += delegate { _btnFinish.Location = new Point(footer.ClientSize.Width - _btnFinish.Width - 28, 13); };

            Controls.Add(logWrap);
            Controls.Add(progressWrap);
            Controls.Add(header);
            Controls.Add(footer);
        }

        public void Reset(string subtitle)
        {
            _subtitle.Text = subtitle;
            _log.Clear();
            _progress.Style = ProgressBarStyle.Marquee;
            _btnFinish.Enabled = false;
            _btnFinish.Text = Strings.Get("clean.btn.working");
        }

        public void AppendLog(string text, Color color)
        {
            if (_log.InvokeRequired)
            {
                _log.Invoke(new Action<string, Color>(AppendLog), text, color);
                return;
            }
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = color;
            _log.AppendText(text + Environment.NewLine);
            _log.ScrollToCaret();
        }

        public void Complete(bool success, string summary)
        {
            if (InvokeRequired) { Invoke(new Action<bool, string>(Complete), success, summary); return; }
            _subtitle.Text = summary;
            _progress.Style = ProgressBarStyle.Blocks;
            _progress.Value = 100;
            _btnFinish.Enabled = true;
            _btnFinish.Text = Strings.Get("clean.btn.back");
        }
    }
}
