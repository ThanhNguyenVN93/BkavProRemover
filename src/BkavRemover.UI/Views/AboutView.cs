using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BkavRemover.UI.Views
{
    internal class AboutView : UserControl
    {
        private Label _titleLabel;
        private Label _appName, _appVer;
        private Label _sysLabel, _sysDetail;
        private Label _howTitle, _howBody;
        private Label _safeTitle, _safeBody;
        private Label _devLabel;

        public AboutView()
        {
            BackColor = UiTheme.Background;
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        public void ApplyLanguage()
        {
            _titleLabel.Text = Strings.Get("about.title");
            _appName.Text = Strings.Get("about.appname");
            _sysLabel.Text = Strings.Get("about.sysinfo");
            _howTitle.Text = Strings.Get("about.howworks");
            _howBody.Text = Strings.Get("about.howbody");
            _safeTitle.Text = Strings.Get("about.whysafe");
            _safeBody.Text = Strings.Get("about.whybody");
            _devLabel.Text = Strings.Get("about.dev");
        }

        private void BuildLayout()
        {
            Panel outer = new Panel { Dock = DockStyle.Fill };
            Panel content = new Panel
            {
                Size = new Size(560, 510),
                AutoScroll = true,
                Anchor = AnchorStyles.None
            };

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string bitness = IntPtr.Size == 8 ? "x64" : "x86";

            _titleLabel = new Label
            {
                Text = Strings.Get("about.title"),
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            Panel appCard = MakeCard(UiTheme.Accent, new Point(0, 42), 540, 68);
            Label appIcon = new Label
            {
                Text = "B",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UiTheme.Accent,
                Size = new Size(40, 40),
                Location = new Point(16, 14),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _appName = new Label
            {
                Text = Strings.Get("about.appname"),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(66, 12)
            };
            _appVer = new Label
            {
                Text = string.Format("Version {0}  |  {1}  |  .NET Framework 3.5  |  CLR {2}",
                    version, bitness, Environment.Version),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(66, 36)
            };
            appCard.Controls.Add(appIcon);
            appCard.Controls.Add(_appName);
            appCard.Controls.Add(_appVer);

            Panel sysCard = MakeCard(UiTheme.CardAccent1, new Point(0, 120), 540, 68);
            _sysLabel = new Label
            {
                Text = Strings.Get("about.sysinfo"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            _sysDetail = new Label
            {
                Text = string.Format("{0}  |  {1}  |  {2} processors",
                    Environment.OSVersion, Environment.MachineName, Environment.ProcessorCount),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                Location = new Point(14, 34)
            };
            sysCard.Controls.Add(_sysLabel);
            sysCard.Controls.Add(_sysDetail);

            Panel howCard = MakeCard(UiTheme.CardAccent3, new Point(0, 198), 540, 130);
            _howTitle = new Label
            {
                Text = Strings.Get("about.howworks"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            _howBody = new Label
            {
                Text = Strings.Get("about.howbody"),
                Font = UiTheme.FontSmall,
                ForeColor = ColorTranslator.FromHtml("#4A5568"),
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                Location = new Point(14, 34)
            };
            howCard.Controls.Add(_howTitle);
            howCard.Controls.Add(_howBody);

            Panel safeCard = MakeCard(UiTheme.CardAccent2, new Point(0, 338), 540, 80);
            _safeTitle = new Label
            {
                Text = Strings.Get("about.whysafe"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            _safeBody = new Label
            {
                Text = Strings.Get("about.whybody"),
                Font = UiTheme.FontSmall,
                ForeColor = ColorTranslator.FromHtml("#4A5568"),
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                Location = new Point(14, 34)
            };
            safeCard.Controls.Add(_safeTitle);
            safeCard.Controls.Add(_safeBody);

            Panel devCard = MakeCard(UiTheme.CardAccent4, new Point(0, 428), 540, 50);
            _devLabel = new Label
            {
                Text = Strings.Get("about.dev"),
                Font = UiTheme.FontRegular,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(14, 16)
            };
            devCard.Controls.Add(_devLabel);

            content.Controls.Add(_titleLabel);
            content.Controls.Add(appCard);
            content.Controls.Add(sysCard);
            content.Controls.Add(howCard);
            content.Controls.Add(safeCard);
            content.Controls.Add(devCard);

            outer.Controls.Add(content);
            outer.Resize += delegate
            {
                content.Location = new Point(
                    Math.Max(0, (outer.ClientSize.Width - content.Width) / 2),
                    Math.Max(16, (outer.ClientSize.Height - content.Height) / 2));
            };

            Controls.Add(outer);
        }

        private static Panel MakeCard(Color accentColor, Point location, int width, int height)
        {
            Panel card = new Panel
            {
                BackColor = Color.White,
                Location = location,
                Size = new Size(width, height)
            };
            Panel accent = new Panel
            {
                BackColor = accentColor,
                Dock = DockStyle.Left,
                Width = 3
            };
            card.Controls.Add(accent);
            return card;
        }
    }
}
