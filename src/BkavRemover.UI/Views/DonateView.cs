using System.Drawing;
using System.Windows.Forms;
using BkavRemover.Core;

namespace BkavRemover.UI.Views
{
    internal class DonateView : UserControl
    {
        private Label _iconLabel, _titleLabel, _bodyLabel, _thanksLabel;
        private Button _btnDonate;

        public DonateView()
        {
            BackColor = UiTheme.Background;
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        public void ApplyLanguage()
        {
            _iconLabel.Text = Strings.Get("donate.icon");
            _titleLabel.Text = Strings.Get("donate.title");
            _bodyLabel.Text = Strings.Get("donate.body");
            _btnDonate.Text = Strings.Get("donate.btn");
            _thanksLabel.Text = Strings.Get("donate.thanks");
        }

        private void BuildLayout()
        {
            Panel center = new Panel { Dock = DockStyle.Fill };

            Panel card = new Panel
            {
                BackColor = Color.White,
                Size = new Size(420, 280),
                Anchor = AnchorStyles.None
            };

            _iconLabel = new Label
            {
                Text = Strings.Get("donate.icon"),
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = UiTheme.Danger,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _titleLabel = new Label
            {
                Text = Strings.Get("donate.title"),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _bodyLabel = new Label
            {
                Text = Strings.Get("donate.body"),
                Font = UiTheme.FontRegular,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                MaximumSize = new Size(360, 0)
            };

            _btnDonate = new Button
            {
                Text = Strings.Get("donate.btn"),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Danger,
                ForeColor = Color.White,
                Font = UiTheme.FontBold,
                Size = new Size(300, 42),
                Cursor = Cursors.Hand
            };
            _btnDonate.FlatAppearance.BorderSize = 0;
            _btnDonate.Click += delegate { SystemActions.OpenUrl(AppSettings.DonateUrl); };

            _thanksLabel = new Label
            {
                Text = Strings.Get("donate.thanks"),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.Add(_iconLabel);
            card.Controls.Add(_titleLabel);
            card.Controls.Add(_bodyLabel);
            card.Controls.Add(_btnDonate);
            card.Controls.Add(_thanksLabel);

            center.Controls.Add(card);
            center.Resize += delegate
            {
                card.Location = new Point(
                    (center.ClientSize.Width - card.Width) / 2,
                    (center.ClientSize.Height - card.Height) / 2);

                int cx = card.Width / 2;
                _iconLabel.Location = new Point(cx - _iconLabel.Width / 2, 20);
                _titleLabel.Location = new Point(cx - _titleLabel.Width / 2, 72);
                _bodyLabel.Location = new Point(cx - _bodyLabel.Width / 2, 104);
                _btnDonate.Location = new Point(cx - _btnDonate.Width / 2, 184);
                _thanksLabel.Location = new Point(cx - _thanksLabel.Width / 2, 236);
            };

            Controls.Add(center);
        }
    }
}
