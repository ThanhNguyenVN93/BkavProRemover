using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BkavRemover.Core.Models;

namespace BkavRemover.UI.Views
{
    internal class DashboardView : UserControl
    {
        public event EventHandler ScanRequested;
        public event EventHandler CleanRequested;

        private Label _title, _subtitle;
        private Label _badge;
        private Label _lblServices, _lblProcesses, _lblFiles, _lblRegistry;
        private Label _cardServices, _cardProcesses, _cardFiles, _cardRegistry;
        private Label _listLabel;
        private Label _scanInfo;
        private Label _cleanMsg, _cleanSub;
        private ListView _list;
        private Panel _listArea;
        private Panel _cleanStatePanel;
        private Panel _toolbar;
        private Button _btnScan;
        private Button _btnClean;
        private Button _btnExport;
        private CheckBox _chkSelectAll;
        private CheckBox _chkAutoSafeMode;
        private ComboBox _cboFilter;
        private Panel _header;
        private bool _hasItems;
        private ScanResult _currentResult;
        private List<ListViewItem> _allRows;

        public DashboardView()
        {
            BackColor = UiTheme.Background;
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        public bool AutoSafeMode { get { return _chkAutoSafeMode.Checked; } }

        public void SetBusy(bool busy)
        {
            _btnScan.Enabled = !busy;
            _btnClean.Enabled = !busy && _hasItems;
            _chkAutoSafeMode.Enabled = !busy;
            _btnExport.Enabled = !busy && _hasItems;
            _cboFilter.Enabled = !busy;
            _chkSelectAll.Enabled = !busy && _hasItems;
        }

        public void ApplyLanguage()
        {
            _title.Text = Strings.Get("dash.title");
            _subtitle.Text = Strings.Get("dash.subtitle");
            _cardServices.Text = Strings.Get("dash.card.services");
            _cardProcesses.Text = Strings.Get("dash.card.processes");
            _cardFiles.Text = Strings.Get("dash.card.files");
            _cardRegistry.Text = Strings.Get("dash.card.registry");
            _btnScan.Text = Strings.Get("dash.btn.scan");
            _chkAutoSafeMode.Text = Strings.Get("dash.autosafemode");
            _btnExport.Text = Strings.Get("dash.export");
            _cleanMsg.Text = Strings.Get("dash.cleanstate.title");
            _cleanSub.Text = Strings.Get("dash.cleanstate.sub");

            _chkSelectAll.Text = _chkSelectAll.Checked
                ? Strings.Get("dash.selectall") : Strings.Get("dash.deselectall");

            _cboFilter.Items.Clear();
            _cboFilter.Items.AddRange(new object[] {
                Strings.Get("dash.filter.all"),
                Strings.Get("dash.filter.services"),
                Strings.Get("dash.filter.processes"),
                Strings.Get("dash.filter.files"),
                Strings.Get("dash.filter.registry")
            });
            _cboFilter.SelectedIndex = 0;

            if (_currentResult != null)
                ShowResult(_currentResult);
        }

        private void BuildLayout()
        {
            _header = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(28, 16, 28, 0) };
            _title = new Label
            {
                Text = Strings.Get("dash.title"),
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            _subtitle = new Label
            {
                Text = Strings.Get("dash.subtitle"),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(2, 28)
            };
            _badge = new Label
            {
                Text = "",
                AutoSize = true,
                Font = UiTheme.FontBold,
                BackColor = UiTheme.WarningBg,
                ForeColor = ColorTranslator.FromHtml("#8A5A0E"),
                Padding = new Padding(12, 5, 12, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _header.Controls.Add(_title);
            _header.Controls.Add(_subtitle);
            _header.Controls.Add(_badge);
            _header.Resize += delegate { RepositionBadge(); };

            TableLayoutPanel cards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 92,
                ColumnCount = 4,
                Padding = new Padding(26, 6, 26, 6)
            };
            for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _lblServices = AddCard(cards, Strings.Get("dash.card.services"), 0, UiTheme.CardAccent1, out _cardServices);
            _lblProcesses = AddCard(cards, Strings.Get("dash.card.processes"), 1, UiTheme.CardAccent2, out _cardProcesses);
            _lblFiles = AddCard(cards, Strings.Get("dash.card.files"), 2, UiTheme.CardAccent3, out _cardFiles);
            _lblRegistry = AddCard(cards, Strings.Get("dash.card.registry"), 3, UiTheme.CardAccent4, out _cardRegistry);

            _listLabel = new Label
            {
                Text = Strings.Get("dash.detected"),
                Font = UiTheme.FontBold,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 8, 0, 4)
            };

            _toolbar = new Panel { Dock = DockStyle.Top, Height = 32 };

            _cboFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiTheme.FontSmall,
                Size = new Size(120, 24),
                Location = new Point(0, 4)
            };
            _cboFilter.Items.AddRange(new object[] {
                Strings.Get("dash.filter.all"),
                Strings.Get("dash.filter.services"),
                Strings.Get("dash.filter.processes"),
                Strings.Get("dash.filter.files"),
                Strings.Get("dash.filter.registry")
            });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += delegate { ApplyFilter(); };

            _chkSelectAll = new CheckBox
            {
                Text = Strings.Get("dash.selectall"),
                Checked = true,
                AutoSize = true,
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(132, 7),
                Enabled = false
            };
            _chkSelectAll.CheckedChanged += delegate
            {
                bool check = _chkSelectAll.Checked;
                _chkSelectAll.Text = check ? Strings.Get("dash.selectall") : Strings.Get("dash.deselectall");
                foreach (ListViewItem item in _list.Items)
                    item.Checked = check;
            };

            _btnExport = new Button
            {
                Text = Strings.Get("dash.export"),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontSmall,
                Size = new Size(70, 24),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            _btnExport.FlatAppearance.BorderColor = UiTheme.CardBorder;
            _btnExport.Click += delegate { ExportResults(); };

            _scanInfo = new Label
            {
                Text = "",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _toolbar.Controls.Add(_cboFilter);
            _toolbar.Controls.Add(_chkSelectAll);
            _toolbar.Controls.Add(_btnExport);
            _toolbar.Controls.Add(_scanInfo);
            _toolbar.Resize += delegate
            {
                _btnExport.Location = new Point(_toolbar.ClientSize.Width - _btnExport.Width, 4);
                _scanInfo.Location = new Point(_btnExport.Left - _scanInfo.Width - 10, 7);
            };

            _list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                Font = UiTheme.FontRegular,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _list.Columns.Add("Item", 200);
            _list.Columns.Add("Detail", 440);
            _list.Columns.Add("Kind", 110);

            _listArea = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 28, 0) };
            _listArea.Controls.Add(_list);
            _listArea.Controls.Add(_toolbar);
            _listArea.Controls.Add(_listLabel);

            _cleanStatePanel = BuildCleanStatePanel();

            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(28, 12, 28, 12) };
            _chkAutoSafeMode = new CheckBox
            {
                Text = Strings.Get("dash.autosafemode"),
                Checked = true,
                AutoSize = true,
                Font = UiTheme.FontSmall,
                ForeColor = ColorTranslator.FromHtml("#5A6476"),
                Location = new Point(0, 16)
            };

            _btnScan = new Button
            {
                Text = Strings.Get("dash.btn.scan"),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontBold,
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnScan.FlatAppearance.BorderColor = UiTheme.CardBorder;
            _btnScan.Click += delegate { OnScanRequested(); };

            _btnClean = new Button
            {
                Text = Strings.Get("dash.btn.clean"),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                Font = UiTheme.FontBold,
                Size = new Size(160, 36),
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClean.FlatAppearance.BorderSize = 0;
            _btnClean.Click += delegate { OnCleanRequested(); };

            footer.Controls.Add(_chkAutoSafeMode);
            footer.Controls.Add(_btnScan);
            footer.Controls.Add(_btnClean);
            footer.Resize += delegate
            {
                _btnClean.Location = new Point(footer.ClientSize.Width - _btnClean.Width - 28, 15);
                _btnScan.Location = new Point(_btnClean.Left - _btnScan.Width - 10, 15);
            };

            Panel contentArea = new Panel { Dock = DockStyle.Fill };
            contentArea.Controls.Add(_cleanStatePanel);
            contentArea.Controls.Add(_listArea);

            Controls.Add(contentArea);
            Controls.Add(cards);
            Controls.Add(_header);
            Controls.Add(footer);
        }

        private Panel BuildCleanStatePanel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = UiTheme.Background };

            Label checkLabel = new Label
            {
                Text = "OK",
                Font = new Font("Segoe UI", 36f, FontStyle.Bold),
                ForeColor = UiTheme.Success,
                AutoSize = true
            };
            _cleanMsg = new Label
            {
                Text = Strings.Get("dash.cleanstate.title"),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true
            };
            _cleanSub = new Label
            {
                Text = Strings.Get("dash.cleanstate.sub"),
                Font = UiTheme.FontRegular,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true
            };

            panel.Controls.Add(checkLabel);
            panel.Controls.Add(_cleanMsg);
            panel.Controls.Add(_cleanSub);

            panel.Resize += delegate
            {
                int cx = panel.ClientSize.Width / 2;
                int cy = panel.ClientSize.Height / 2 - 20;
                checkLabel.Location = new Point(cx - checkLabel.Width / 2, cy - 55);
                _cleanMsg.Location = new Point(cx - _cleanMsg.Width / 2, cy + 5);
                _cleanSub.Location = new Point(cx - _cleanSub.Width / 2, cy + 35);
            };

            return panel;
        }

        private static Label AddCard(TableLayoutPanel parent, string label, int column, Color accentColor, out Label caption)
        {
            Panel card = new Panel
            {
                BackColor = Color.White,
                Margin = new Padding(2, 0, 8, 0),
                Dock = DockStyle.Fill
            };
            Panel accent = new Panel
            {
                BackColor = accentColor,
                Dock = DockStyle.Top,
                Height = 3
            };
            caption = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 7f),
                ForeColor = ColorTranslator.FromHtml("#8891A0"),
                AutoSize = true,
                Location = new Point(14, 12)
            };
            Label count = new Label
            {
                Text = "-",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(12, 34)
            };
            card.Controls.Add(caption);
            card.Controls.Add(count);
            card.Controls.Add(accent);
            parent.Controls.Add(card, column, 0);
            return count;
        }

        private void RepositionBadge()
        {
            if (_header != null && _badge != null)
                _badge.Location = new Point(_header.ClientSize.Width - _badge.Width - 28, 10);
        }

        private void OnScanRequested()
        {
            EventHandler h = ScanRequested;
            if (h != null) h(this, EventArgs.Empty);
        }

        private void OnCleanRequested()
        {
            EventHandler h = CleanRequested;
            if (h != null) h(this, EventArgs.Empty);
        }

        public void SetScanning()
        {
            _hasItems = false;
            _currentResult = null;
            _allRows = null;
            _badge.Text = Strings.Get("dash.scanning");
            _badge.BackColor = UiTheme.WarningBg;
            _badge.ForeColor = ColorTranslator.FromHtml("#8A5A0E");
            RepositionBadge();
            _list.Items.Clear();
            _btnScan.Enabled = false;
            _btnClean.Enabled = false;
            _btnClean.Text = Strings.Get("dash.btn.clean");
            _btnExport.Enabled = false;
            _chkSelectAll.Enabled = false;
            _cboFilter.SelectedIndex = 0;
            _scanInfo.Text = Strings.Get("dash.scaninfo.scanning");
            _listLabel.Text = Strings.Get("dash.detected");
            _listArea.Visible = true;
            _cleanStatePanel.Visible = false;
        }

        public void ShowResult(ScanResult result)
        {
            _currentResult = result;
            _lblServices.Text = result.Services.Count.ToString();
            _lblProcesses.Text = result.Processes.Count.ToString();
            _lblFiles.Text = result.Files.Count.ToString();
            _lblRegistry.Text = (result.RegistryKeys.Count + result.RegistryValues.Count).ToString();

            bool isClean = result.TotalCount == 0;
            _hasItems = !isClean;

            _listArea.Visible = !isClean;
            _cleanStatePanel.Visible = isClean;
            _toolbar.Visible = !isClean;

            if (isClean)
            {
                _badge.Text = Strings.Get("dash.clean");
                _badge.BackColor = UiTheme.SuccessBg;
                _badge.ForeColor = UiTheme.Success;
            }
            else if (result.IsSafeMode)
            {
                _badge.Text = Strings.Get("dash.safemode");
                _badge.BackColor = UiTheme.SuccessBg;
                _badge.ForeColor = UiTheme.Accent;
            }
            else
            {
                _badge.Text = result.HasKernelDriver
                    ? Strings.Get("dash.normalmode.kernel")
                    : Strings.Get("dash.normalmode");
                _badge.BackColor = UiTheme.WarningBg;
                _badge.ForeColor = ColorTranslator.FromHtml("#8A5A0E");
            }
            RepositionBadge();

            _allRows = new List<ListViewItem>();
            if (!isClean)
            {
                foreach (ServiceFinding f in result.Services)
                    _allRows.Add(MakeRow(f.Name, "Service - " + f.DisplayName, "Service", f));
                foreach (ProcessFinding f in result.Processes)
                    _allRows.Add(MakeRow(f.Name, f.Path ?? ("PID " + f.Id), "Process", f));
                foreach (FileFinding f in result.Files)
                    _allRows.Add(MakeRow(System.IO.Path.GetFileName(f.Path), f.Path, f.IsDirectory ? "Folder" : "File", f));
                foreach (ScheduledTaskFinding f in result.ScheduledTasks)
                    _allRows.Add(MakeRow(f.TaskName, "Task - " + f.TaskPath, "Task", f));
                foreach (RegistryKeyFinding f in result.RegistryKeys)
                    _allRows.Add(MakeRow(ShortKeyName(f.FullKeyPath), f.FullKeyPath, "Reg key", f));
                foreach (RegistryValueFinding f in result.RegistryValues)
                    _allRows.Add(MakeRow(f.ValueName, f.FullKeyPath, "Reg value", f));

                _list.Items.Clear();
                _list.Items.AddRange(_allRows.ToArray());
                _listLabel.Text = string.Format(Strings.Get("dash.detected.n"), result.TotalCount);
            }

            _btnClean.Text = _hasItems
                ? string.Format(Strings.Get("dash.btn.clean.n"), result.TotalCount)
                : Strings.Get("dash.btn.clean");
            _btnClean.Enabled = _hasItems;
            _btnScan.Enabled = true;
            _btnExport.Enabled = _hasItems;
            _chkSelectAll.Enabled = _hasItems;
            _chkSelectAll.Checked = true;
        }

        public void SetScanDuration(TimeSpan duration)
        {
            _scanInfo.Text = string.Format(Strings.Get("dash.scaninfo.done"), duration.TotalSeconds);
        }

        private void ApplyFilter()
        {
            if (_allRows == null || _allRows.Count == 0) return;

            int idx = _cboFilter.SelectedIndex;
            _list.Items.Clear();

            List<ListViewItem> filtered = new List<ListViewItem>();
            foreach (ListViewItem row in _allRows)
            {
                string kind = row.SubItems[2].Text;
                bool match;
                switch (idx)
                {
                    case 1: match = kind == "Service"; break;
                    case 2: match = kind == "Process"; break;
                    case 3: match = kind == "File" || kind == "Folder"; break;
                    case 4: match = kind == "Reg key" || kind == "Reg value"; break;
                    default: match = true; break;
                }
                if (match) filtered.Add(row);
            }

            _list.Items.AddRange(filtered.ToArray());
            if (idx == 0)
                _listLabel.Text = string.Format(Strings.Get("dash.detected.n"), _allRows.Count);
            else
                _listLabel.Text = string.Format(Strings.Get("dash.filter.detail"),
                    _cboFilter.SelectedItem, filtered.Count, _allRows.Count);
        }

        private void ExportResults()
        {
            if (_currentResult == null) return;

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Text files|*.txt|All files|*.*",
                FileName = "BkavScan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt",
                Title = Strings.Get("dlg.export.title")
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                using (StreamWriter w = new StreamWriter(dlg.FileName))
                {
                    w.WriteLine("Bkav Pro / BluPro Remover - Scan Results");
                    w.WriteLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    w.WriteLine(string.Format("OS: {0}  |  {1}  |  {2}",
                        Environment.OSVersion, Environment.MachineName,
                        IntPtr.Size == 8 ? "x64" : "x86"));
                    w.WriteLine();
                    w.WriteLine(string.Format("Services/Drivers: {0}", _currentResult.Services.Count));
                    w.WriteLine(string.Format("Processes: {0}", _currentResult.Processes.Count));
                    w.WriteLine(string.Format("Files/Folders: {0}", _currentResult.Files.Count));
                    w.WriteLine(string.Format("Registry: {0}", _currentResult.RegistryKeys.Count + _currentResult.RegistryValues.Count));
                    w.WriteLine(string.Format("Total: {0}", _currentResult.TotalCount));

                    if (_currentResult.Services.Count > 0)
                    {
                        w.WriteLine();
                        w.WriteLine("--- Services / Drivers ---");
                        foreach (ServiceFinding f in _currentResult.Services)
                            w.WriteLine(string.Format("  {0} ({1})", f.Name, f.DisplayName));
                    }
                    if (_currentResult.Processes.Count > 0)
                    {
                        w.WriteLine();
                        w.WriteLine("--- Processes ---");
                        foreach (ProcessFinding f in _currentResult.Processes)
                            w.WriteLine(string.Format("  {0} (PID {1}) - {2}", f.Name, f.Id, f.Path ?? "?"));
                    }
                    if (_currentResult.Files.Count > 0)
                    {
                        w.WriteLine();
                        w.WriteLine("--- Files / Folders ---");
                        foreach (FileFinding f in _currentResult.Files)
                            w.WriteLine(string.Format("  [{0}] {1}", f.IsDirectory ? "DIR" : "FILE", f.Path));
                    }
                    if (_currentResult.ScheduledTasks.Count > 0)
                    {
                        w.WriteLine();
                        w.WriteLine("--- Scheduled Tasks ---");
                        foreach (ScheduledTaskFinding f in _currentResult.ScheduledTasks)
                            w.WriteLine(string.Format("  {0} ({1})", f.TaskName, f.TaskPath));
                    }
                    if (_currentResult.RegistryKeys.Count > 0)
                    {
                        w.WriteLine();
                        w.WriteLine("--- Registry Keys ---");
                        foreach (RegistryKeyFinding f in _currentResult.RegistryKeys)
                            w.WriteLine("  " + f.FullKeyPath);
                    }
                    if (_currentResult.RegistryValues.Count > 0)
                    {
                        w.WriteLine();
                        w.WriteLine("--- Registry Values ---");
                        foreach (RegistryValueFinding f in _currentResult.RegistryValues)
                            w.WriteLine(string.Format("  {0}\\{1} = {2}", f.FullKeyPath, f.ValueName, f.Data));
                    }
                }

                MessageBox.Show(string.Format(Strings.Get("dlg.export.done"), dlg.FileName),
                    Strings.Get("dlg.export.done.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Strings.Get("dlg.export.fail"), ex.Message),
                    Strings.Get("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ShortKeyName(string fullKeyPath)
        {
            int idx = fullKeyPath.LastIndexOf('\\');
            return idx >= 0 ? fullKeyPath.Substring(idx + 1) : fullKeyPath;
        }

        private static ListViewItem MakeRow(string name, string detail, string kind, object tag)
        {
            ListViewItem item = new ListViewItem(new[] { name, detail, kind });
            item.Checked = true;
            item.Tag = tag;
            return item;
        }

        public void ApplySelectionTo(ScanResult result)
        {
            HashSet<object> checkedTags = new HashSet<object>();
            foreach (ListViewItem item in _list.Items)
                if (item.Checked) checkedTags.Add(item.Tag);

            foreach (ServiceFinding f in result.Services) f.Selected = checkedTags.Contains(f);
            foreach (ProcessFinding f in result.Processes) f.Selected = checkedTags.Contains(f);
            foreach (FileFinding f in result.Files) f.Selected = checkedTags.Contains(f);
            foreach (ScheduledTaskFinding f in result.ScheduledTasks) f.Selected = checkedTags.Contains(f);
            foreach (RegistryKeyFinding f in result.RegistryKeys) f.Selected = checkedTags.Contains(f);
            foreach (RegistryValueFinding f in result.RegistryValues) f.Selected = checkedTags.Contains(f);
        }
    }
}
