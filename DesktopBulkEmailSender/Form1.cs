using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DesktopBulkEmailSender
{
    public partial class MainForm : Form
    {
        // ── State ────────────────────────────────────────────────────────
        private readonly List<string> _attachments = new List<string>();
        private CancellationTokenSource _cts;
        private bool _isSending = false;

        // ── Controls ─────────────────────────────────────────────────────
        private TabControl tabMain;
        private TabPage tabConfig, tabRecipients, tabBody, tabAttachments, tabSend;

        // Config tab
        private TextBox txtSenderEmail, txtSenderName, txtAppPassword, txtSmtpHost;
        private NumericUpDown nudSmtpPort, nudMinDelay, nudMaxDelay;
        private CheckBox chkShowPassword;

        // Recipients tab
        private DataGridView dgvRecipients;
        private Button btnParsePaste, btnAddRow, btnRemoveSelected, btnClearAll, btnCheckAll, btnUncheckAll, btnDedup;
        private ComboBox cmbGroupBy;
        private Label lblRecipientCount;
        private RichTextBox rtbPaste;

        // Body tab
        private TextBox txtSubject;
        private RichTextBox rtbHtmlBody;
        private Button btnLoadHtmlFile, btnPreviewHtml;

        // Attachments tab
        private ListBox lstAttachments;
        private Button btnAddAttachment, btnRemoveAttachment;

        // Send tab
        private Button btnSend, btnCancel;
        private ProgressBar pbProgress;
        private RichTextBox rtbLog;
        private Label lblProgress, lblSumDetails;

        // Status strip
        private StatusStrip statusStrip;
        private ToolStripStatusLabel tsslStatus;

        // Save buttons
        private Button btnSaveHeader, btnSaveConfig;

        // ── Constructor ──────────────────────────────────────────────────
        public MainForm()
        {
            this.Text          = "Bulk Email Sender";
            this.Size          = new Size(1000, 720);
            this.MinimumSize   = new Size(900, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font          = new Font("Segoe UI", 9.5f);
            this.BackColor     = Color.FromArgb(245, 247, 250);

            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            BuildUI();
            ApplyTheme();
            WireEvents();
            LoadSettings();
        }

        // ════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // Header
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 66,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding   = new Padding(16, 0, 16, 0)
            };

            PictureBox picLogo = null;
            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.png");
            if (File.Exists(logoPath))
            {
                try
                {
                    picLogo = new PictureBox
                    {
                        Image     = Image.FromFile(logoPath),
                        SizeMode  = PictureBoxSizeMode.Zoom,
                        Size      = new Size(42, 42),
                        Location  = new Point(16, 12),
                        BackColor = Color.Transparent
                    };
                }
                catch { }
            }

            int titleX = picLogo != null ? 68 : 16;
            var lblTitle = new Label
            {
                Text      = picLogo != null ? "Bulk Email Sender" : "✉  Bulk Email Sender",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 16.5f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(titleX, 10)
            };
            var lblSub = new Label
            {
                Text      = "Send personalized bulk emails with attachments, grouped by company",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = true,
                Location  = new Point(titleX + 2, 40)
            };

            btnSaveHeader = new Button
            {
                Text      = "💾  Save All",
                Size      = new Size(115, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSaveHeader.FlatAppearance.BorderSize = 0;
            btnSaveHeader.Location = new Point(header.ClientSize.Width - 135, 14);
            header.Resize += (s, e) => {
                btnSaveHeader.Location = new Point(header.ClientSize.Width - 135, 14);
            };

            if (picLogo != null)
                header.Controls.Add(picLogo);
            header.Controls.AddRange(new Control[] { lblTitle, lblSub, btnSaveHeader });

            // Status strip
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(30, 41, 59) };
            tsslStatus  = new ToolStripStatusLabel("Ready") { ForeColor = Color.FromArgb(148, 163, 184) };
            statusStrip.Items.Add(tsslStatus);

            // Tab control
            tabMain = new TabControl
            {
                Dock    = DockStyle.Fill,
                Padding = new Point(16, 6),
                Font    = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            tabConfig      = new TabPage("⚙  Config");
            tabRecipients  = new TabPage("👥  Recipients");
            tabBody        = new TabPage("📝  Email Body");
            tabAttachments = new TabPage("📎  Attachments");
            tabSend        = new TabPage("🚀  Send");

            tabMain.TabPages.AddRange(new[] { tabConfig, tabRecipients, tabBody, tabAttachments, tabSend });

            BuildConfigTab();
            BuildRecipientsTab();
            BuildBodyTab();
            BuildAttachmentsTab();
            BuildSendTab();

            this.Controls.Add(tabMain);
            this.Controls.Add(header);
            this.Controls.Add(statusStrip);
        }

        // ── Config Tab ───────────────────────────────────────────────────
        private void BuildConfigTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tabConfig.Controls.Add(pnl);

            int y = 20;

            AddSectionLabel(pnl, "Sender Credentials", ref y);

            AddLabel(pnl, "Email Address:", 20, y);
            txtSenderEmail = AddTextBox(pnl, 185, y, 340);
            txtSenderEmail.PlaceholderText = "e.g. yourname@gmail.com or user@company.com";
            y += 36;

            AddLabel(pnl, "Display Name:", 20, y);
            txtSenderName = AddTextBox(pnl, 185, y, 340);
            txtSenderName.PlaceholderText = "e.g. Your Name";
            y += 36;

            AddLabel(pnl, "Password / App Pass:", 20, y);
            txtAppPassword = AddTextBox(pnl, 185, y, 280);
            txtAppPassword.PlaceholderText = "App Password or SMTP Password";
            txtAppPassword.UseSystemPasswordChar = true;
            chkShowPassword = new CheckBox { Text = "Show", Location = new Point(471, y + 2), AutoSize = true };
            pnl.Controls.Add(chkShowPassword);
            y += 48;

            AddSectionLabel(pnl, "SMTP Settings", ref y);

            AddLabel(pnl, "SMTP Host:", 20, y);
            txtSmtpHost = AddTextBox(pnl, 185, y, 240);
            txtSmtpHost.Text = "smtp.gmail.com";
            AddLabel(pnl, "Port:", 440, y);
            nudSmtpPort = new NumericUpDown { Location = new Point(476, y), Width = 70, Minimum = 1, Maximum = 65535, Value = 587, Font = this.Font };
            pnl.Controls.Add(nudSmtpPort);
            y += 48;

            AddSectionLabel(pnl, "Delay Between Emails (seconds)", ref y);

            AddLabel(pnl, "Min Delay:", 20, y);
            nudMinDelay = new NumericUpDown { Location = new Point(185, y), Width = 70, Minimum = 0, Maximum = 300, Value = 5, Font = this.Font };
            pnl.Controls.Add(nudMinDelay);
            AddLabel(pnl, "Max Delay:", 280, y);
            nudMaxDelay = new NumericUpDown { Location = new Point(370, y), Width = 70, Minimum = 0, Maximum = 300, Value = 15, Font = this.Font };
            pnl.Controls.Add(nudMaxDelay);
            y += 48;

            var lnk = new LinkLabel
            {
                Text      = "How to create a Gmail App Password →",
                Location  = new Point(20, y),
                AutoSize  = true,
                LinkColor = Color.FromArgb(59, 130, 246)
            };
            lnk.LinkClicked += (s, e) =>
                OpenInBrowser("https://support.google.com/accounts/answer/185833");
            pnl.Controls.Add(lnk);
            y += 34;

            btnSaveConfig = Btn("💾  Save All Settings", Color.FromArgb(37, 99, 235));
            btnSaveConfig.Location = new Point(20, y);
            btnSaveConfig.Width = 190;
            btnSaveConfig.Height = 36;
            pnl.Controls.Add(btnSaveConfig);
        }

        // ── Recipients Tab ───────────────────────────────────────────────
        private void BuildRecipientsTab()
        {
            // Use TableLayoutPanel instead of SplitContainer to avoid SplitterDistance exceptions
            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ── Left: grid panel ────────────────────────────────────────
            var leftPanel = new Panel { Dock = DockStyle.Fill };

            var topToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 42,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding   = new Padding(8, 6, 8, 4)
            };

            lblRecipientCount = new Label
            {
                Text      = "Recipients: 0",
                Location  = new Point(8, 11),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            var lblGroup = new Label
            {
                Text     = "Sort/Group:",
                Location = new Point(200, 11),
                AutoSize = true,
                Font     = new Font("Segoe UI", 9f)
            };
            cmbGroupBy = new ComboBox
            {
                Location      = new Point(280, 8),
                Width         = 185,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9f)
            };
            cmbGroupBy.Items.AddRange(new object[] {
                "None (original order)",
                "Company (domain)",
                "Email A → Z",
                "Email Z → A"
            });
            cmbGroupBy.SelectedIndex = 0;

            topToolbar.Controls.AddRange(new Control[] { lblRecipientCount, lblGroup, cmbGroupBy });

            // Bottom toolbar for action buttons (aesthetic footer)
            var bottomToolbar = new FlowLayoutPanel
            {
                Dock         = DockStyle.Bottom,
                Height       = 44,
                BackColor    = Color.FromArgb(241, 245, 249),
                Padding      = new Padding(6, 6, 6, 6),
                WrapContents = false,
                AutoScroll   = false
            };

            btnAddRow         = SmBtn("+ Add",   Color.FromArgb(34, 197, 94),   0, 0);
            btnAddRow.Width   = 64;
            btnAddRow.Height  = 30;
            btnAddRow.Margin  = new Padding(3, 1, 3, 1);

            btnRemoveSelected = SmBtn("Remove",  Color.FromArgb(239, 68, 68),   0, 0);
            btnRemoveSelected.Width = 74;
            btnRemoveSelected.Height = 30;
            btnRemoveSelected.Margin = new Padding(3, 1, 3, 1);

            btnClearAll       = SmBtn("Clear",   Color.FromArgb(107, 114, 128), 0, 0);
            btnClearAll.Width = 62;
            btnClearAll.Height = 30;
            btnClearAll.Margin = new Padding(3, 1, 3, 1);

            btnCheckAll       = SmBtn("✔ All",  Color.FromArgb(59, 130, 246),  0, 0);
            btnCheckAll.Width = 66;
            btnCheckAll.Height = 30;
            btnCheckAll.Margin = new Padding(3, 1, 3, 1);

            btnUncheckAll     = SmBtn("✖ None", Color.FromArgb(100, 116, 139), 0, 0);
            btnUncheckAll.Width = 74;
            btnUncheckAll.Height = 30;
            btnUncheckAll.Margin = new Padding(3, 1, 3, 1);

            btnDedup          = SmBtn("Dedup",   Color.FromArgb(14, 165, 233),  0, 0);
            btnDedup.Width    = 70;
            btnDedup.Height   = 30;
            btnDedup.Margin   = new Padding(3, 1, 3, 1);

            bottomToolbar.Controls.AddRange(new Control[] {
                btnAddRow,
                btnRemoveSelected,
                btnClearAll,
                btnCheckAll,
                btnUncheckAll,
                btnDedup
            });

            dgvRecipients = new DataGridView
            {
                Dock                    = DockStyle.Fill,
                AllowUserToAddRows      = true,
                AllowUserToDeleteRows   = true,
                MultiSelect             = true,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible       = false,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor         = Color.White,
                BorderStyle             = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight     = 34
            };

            var colSend = new DataGridViewCheckBoxColumn
            {
                Name         = "Send",
                HeaderText   = "Send?",
                Width        = 55,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                TrueValue    = true,
                FalseValue   = false,
                ThreeState   = false
            };
            dgvRecipients.Columns.Add(colSend);
            dgvRecipients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email",   HeaderText = "Email Address",    FillWeight = 55 });
            dgvRecipients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Company", HeaderText = "Company / Domain", FillWeight = 30, ReadOnly = true });

            var colDuplicates = new DataGridViewTextBoxColumn
            {
                Name         = "Duplicates",
                HeaderText   = "Duplicates",
                Width        = 85,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly     = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };
            dgvRecipients.Columns.Add(colDuplicates);

            leftPanel.Controls.Add(dgvRecipients);
            leftPanel.Controls.Add(bottomToolbar);
            leftPanel.Controls.Add(topToolbar);

            // ── Right: bulk paste panel ─────────────────────────────────
            var rightPanel = new Panel { Dock = DockStyle.Fill };

            var lblPaste = new Label
            {
                Text      = "Bulk Paste / Import",
                Dock      = DockStyle.Top,
                Height    = 28,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding   = new Padding(6, 4, 0, 0)
            };
            var lblHint = new Label
            {
                Text      = "Paste emails here — one per line, comma-separated, or CSV.\nLines starting with # are skipped automatically.",
                Dock      = DockStyle.Top,
                Height    = 38,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font      = new Font("Segoe UI", 8.5f),
                Padding   = new Padding(6, 0, 0, 0)
            };

            rtbPaste = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Consolas", 9f),
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor   = Color.FromArgb(248, 250, 252)
            };

            btnParsePaste = new Button
            {
                Text      = "⬅  Add to Recipients",
                Dock      = DockStyle.Bottom,
                Height    = 38,
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnParsePaste.FlatAppearance.BorderSize = 0;

            rightPanel.Controls.Add(rtbPaste);
            rightPanel.Controls.Add(btnParsePaste);
            rightPanel.Controls.Add(lblHint);
            rightPanel.Controls.Add(lblPaste);

            layout.Controls.Add(leftPanel,  0, 0);
            layout.Controls.Add(rightPanel, 1, 0);

            tabRecipients.Controls.Add(layout);
        }

        // ── Body Tab ─────────────────────────────────────────────────────
        private void BuildBodyTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tabBody.Controls.Add(pnl);

            AddLabel(pnl, "Subject:", 10, 14);
            txtSubject = new TextBox
            {
                Location        = new Point(82, 10),
                Width           = 720,
                Font            = new Font("Segoe UI", 11f),
                Anchor          = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Enter email subject..."
            };
            pnl.Controls.Add(txtSubject);

            var btnBar = new FlowLayoutPanel { Location = new Point(10, 44), Height = 36, Width = 500, AutoSize = true };
            btnLoadHtmlFile = Btn("📂  Load HTML File", Color.FromArgb(99, 102, 241));
            btnPreviewHtml  = Btn("👁  Preview in Browser", Color.FromArgb(20, 184, 166));
            btnBar.Controls.AddRange(new Control[] { btnLoadHtmlFile, btnPreviewHtml });
            pnl.Controls.Add(btnBar);

            var lblBody = new Label { Text = "HTML Body:", Location = new Point(10, 88), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnl.Controls.Add(lblBody);

            rtbHtmlBody = new RichTextBox
            {
                Location    = new Point(10, 110),
                Font        = new Font("Consolas", 9.5f),
                ScrollBars  = RichTextBoxScrollBars.Both,
                WordWrap    = false,
                BackColor   = Color.FromArgb(15, 23, 42),
                ForeColor   = Color.FromArgb(226, 232, 240),
                BorderStyle = BorderStyle.None,
                Anchor      = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(rtbHtmlBody);

            pnl.Resize += (s, e) =>
            {
                txtSubject.Width   = pnl.ClientSize.Width - 94;
                rtbHtmlBody.Width  = pnl.ClientSize.Width - 20;
                rtbHtmlBody.Height = pnl.ClientSize.Height - 120;
            };
        }

        // ── Attachments Tab ──────────────────────────────────────────────
        private void BuildAttachmentsTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tabAttachments.Controls.Add(pnl);

            var lbl = new Label
            {
                Text      = "Files listed below will be attached to every email sent.",
                Location  = new Point(10, 10),
                AutoSize  = true,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnl.Controls.Add(lbl);

            btnAddAttachment    = Btn("➕  Add File(s)", Color.FromArgb(59, 130, 246));
            btnAddAttachment.Location = new Point(10, 36);
            btnRemoveAttachment = Btn("✖  Remove Selected", Color.FromArgb(239, 68, 68));
            btnRemoveAttachment.Location = new Point(178, 36);
            pnl.Controls.AddRange(new Control[] { btnAddAttachment, btnRemoveAttachment });

            lstAttachments = new ListBox
            {
                Location      = new Point(10, 80),
                Font          = new Font("Segoe UI", 9.5f),
                SelectionMode = SelectionMode.MultiSimple,
                BorderStyle   = BorderStyle.FixedSingle,
                Anchor        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(lstAttachments);

            pnl.Resize += (s, e) =>
            {
                lstAttachments.Width  = pnl.ClientSize.Width - 20;
                lstAttachments.Height = pnl.ClientSize.Height - 90;
            };
        }

        // ── Send Tab ─────────────────────────────────────────────────────
        private void BuildSendTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tabSend.Controls.Add(pnl);

            // Summary box
            var pnlSum = new Panel
            {
                Location    = new Point(10, 10),
                Width       = 860,
                Height      = 60,
                BackColor   = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var lblSumTitle = new Label
            {
                Text      = "Send Summary",
                Location  = new Point(8, 6),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            lblSumDetails = new Label
            {
                Text      = "Configure all tabs, then press Send.",
                Location  = new Point(8, 30),
                AutoSize  = true,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnlSum.Controls.AddRange(new Control[] { lblSumTitle, lblSumDetails });
            pnl.Controls.Add(pnlSum);

            btnSend = new Button
            {
                Text      = "▶  Send Emails",
                Location  = new Point(10, 82),
                Size      = new Size(170, 44),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Text      = "⏹  Stop",
                Location  = new Point(196, 82),
                Size      = new Size(120, 44),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Enabled   = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            pnl.Controls.AddRange(new Control[] { btnSend, btnCancel });

            lblProgress = new Label
            {
                Text      = "0 / 0 sent",
                Location  = new Point(10, 138),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnl.Controls.Add(lblProgress);

            pbProgress = new ProgressBar
            {
                Location = new Point(10, 158),
                Width    = 860,
                Height   = 18,
                Style    = ProgressBarStyle.Continuous,
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(pbProgress);

            var lblLog = new Label { Text = "Send Log:", Location = new Point(10, 188), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnl.Controls.Add(lblLog);

            rtbLog = new RichTextBox
            {
                Location    = new Point(10, 208),
                ReadOnly    = true,
                Font        = new Font("Consolas", 9f),
                BackColor   = Color.FromArgb(15, 23, 42),
                ForeColor   = Color.FromArgb(226, 232, 240),
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Both,
                WordWrap    = true,
                Anchor      = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(rtbLog);

            pnl.Resize += (s, e) =>
            {
                pnlSum.Width    = pnl.ClientSize.Width - 20;
                pbProgress.Width = pnl.ClientSize.Width - 20;
                rtbLog.Width    = pnl.ClientSize.Width - 20;
                rtbLog.Height   = pnl.ClientSize.Height - 218;
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  THEME
        // ════════════════════════════════════════════════════════════════
        private void ApplyTheme()
        {
            foreach (TabPage tp in tabMain.TabPages)
            {
                tp.BackColor = Color.FromArgb(245, 247, 250);
                tp.Padding   = new Padding(8);
            }

            dgvRecipients.EnableHeadersVisualStyles = false;
            dgvRecipients.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(30, 41, 59);
            dgvRecipients.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            dgvRecipients.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvRecipients.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(59, 130, 246);
            dgvRecipients.DefaultCellStyle.SelectionForeColor      = Color.White;
            dgvRecipients.RowsDefaultCellStyle.BackColor           = Color.White;
            dgvRecipients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        }

        // ════════════════════════════════════════════════════════════════
        //  EVENTS
        // ════════════════════════════════════════════════════════════════
        private void WireEvents()
        {
            chkShowPassword.CheckedChanged      += (s, e) => txtAppPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            btnCheckAll.Click                   += (s, e) => SetAllRecipientsChecked(true);
            btnUncheckAll.Click                 += (s, e) => SetAllRecipientsChecked(false);
            btnDedup.Click                      += (s, e) =>
            {
                int removed = DeduplicateRecipients();
                MessageBox.Show(removed > 0 
                    ? $"Removed {removed} duplicate email(s)." 
                    : "No duplicate emails found.", "Deduplication", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            dgvRecipients.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvRecipients.IsCurrentCellDirty)
                    dgvRecipients.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvRecipients.CellValueChanged      += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvRecipients.Columns[e.ColumnIndex].Name == "Send")
                    UpdateRecipientCount();
            };
            btnAddRow.Click                     += (s, e) => AddNewRow();
            btnRemoveSelected.Click             += BtnRemoveSelected_Click;
            btnClearAll.Click                   += BtnClearAll_Click;
            cmbGroupBy.SelectedIndexChanged     += (s, e) => ApplySorting();
            dgvRecipients.CellEndEdit           += DgvRecipients_CellEndEdit;
            dgvRecipients.CellFormatting        += DgvRecipients_CellFormatting;
            btnParsePaste.Click                 += (s, e) => ParseAndAddRecipients(rtbPaste.Text);

            btnLoadHtmlFile.Click               += BtnLoadHtmlFile_Click;
            btnPreviewHtml.Click                += BtnPreviewHtml_Click;

            btnAddAttachment.Click              += BtnAddAttachment_Click;
            btnRemoveAttachment.Click           += (s, e) => RemoveSelectedAttachments();

            btnSaveHeader.Click                 += (s, e) => SaveSettings(showNotification: true);
            btnSaveConfig.Click                 += (s, e) => SaveSettings(showNotification: true);

            btnSend.Click                       += BtnSend_Click;
            btnCancel.Click                     += (s, e) => _cts?.Cancel();

            tabMain.SelectedIndexChanged        += (s, e) =>
            {
                if (tabMain.SelectedTab == tabSend) UpdateSendSummary();
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  RECIPIENT LOGIC
        // ════════════════════════════════════════════════════════════════
        private void SetAllRecipientsChecked(bool check)
        {
            foreach (DataGridViewRow row in dgvRecipients.Rows)
            {
                if (!row.IsNewRow)
                    row.Cells["Send"].Value = check;
            }
            UpdateRecipientCount();
        }

        private void AddNewRow()
        {
            int idx = dgvRecipients.Rows.Add();
            dgvRecipients.Rows[idx].Cells["Send"].Value       = true;
            dgvRecipients.Rows[idx].Cells["Duplicates"].Value = 0;
            dgvRecipients.CurrentCell = dgvRecipients.Rows[idx].Cells["Email"];
            dgvRecipients.BeginEdit(true);
            UpdateDuplicateCounts();
        }

        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvRecipients.SelectedRows)
                if (!row.IsNewRow) dgvRecipients.Rows.Remove(row);
            UpdateRecipientCount();
            UpdateDuplicateCounts();
        }

        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all recipients?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dgvRecipients.Rows.Clear();
                UpdateRecipientCount();
                UpdateDuplicateCounts();
            }
        }

        private void ParseAndAddRecipients(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show("Please enter or paste at least one email address in the Bulk Paste box.", "No Recipients", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Remove existing recipients in the grid and replace with the new list
            dgvRecipients.Rows.Clear();

            var tokens = raw.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            int added = 0;

            foreach (var token in tokens)
            {
                var trimmed = token.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("-") || trimmed.StartsWith("=")) continue;

                bool isEnabled = !trimmed.StartsWith("#");
                var email = CleanEmail(trimmed);
                if (string.IsNullOrWhiteSpace(email)) continue;

                var company = ExtractCompany(email);
                int idx = dgvRecipients.Rows.Add();
                dgvRecipients.Rows[idx].Cells["Send"].Value       = isEnabled;
                dgvRecipients.Rows[idx].Cells["Email"].Value      = email;
                dgvRecipients.Rows[idx].Cells["Company"].Value    = company;
                dgvRecipients.Rows[idx].Cells["Duplicates"].Value = 0;
                added++;
            }

            ApplySorting();
            UpdateRecipientCount();
            UpdateDuplicateCounts();

            int dupCount = dgvRecipients.Rows.Cast<DataGridViewRow>()
                .Count(r => !r.IsNewRow && (r.Cells["Duplicates"].Value is int n) && n > 0);

            if (dupCount > 0)
                SetStatus($"Loaded {added} recipient(s). {dupCount} duplicate email(s) highlighted in red.");
            else
                SetStatus($"Loaded {added} recipient(s).");
        }

        private void DgvRecipients_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvRecipients.Rows.Count) return;
            var row = dgvRecipients.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            if (e.ColumnIndex == dgvRecipients.Columns["Email"].Index)
            {
                var raw = row.Cells["Email"].Value?.ToString() ?? "";
                var clean = CleanEmail(raw);

                if (string.IsNullOrEmpty(clean))
                {
                    row.Cells["Company"].Value    = "";
                    row.Cells["Duplicates"].Value = 0;
                    UpdateRecipientCount();
                    UpdateDuplicateCounts();
                    return;
                }

                row.Cells["Email"].Value   = clean;
                row.Cells["Company"].Value = ExtractCompany(clean);
                UpdateRecipientCount();
                UpdateDuplicateCounts();
            }
        }

        private void DgvRecipients_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvRecipients.Rows.Count) return;
            var row = dgvRecipients.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            var val = row.Cells["Duplicates"].Value;
            int n = 0;
            if (val is int intVal) n = intVal;
            else if (val != null && int.TryParse(val.ToString(), out int parsed)) n = parsed;

            if (n > 0)
            {
                e.CellStyle.BackColor = Color.FromArgb(254, 226, 226); // soft coral/red
                e.CellStyle.ForeColor = Color.FromArgb(153, 27, 27);   // dark red text
                e.CellStyle.SelectionBackColor = Color.FromArgb(239, 68, 68); // selected red
                e.CellStyle.SelectionForeColor = Color.White;
            }
        }

        private void UpdateDuplicateCounts()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // First pass: count frequencies of each cleaned email
            foreach (DataGridViewRow row in dgvRecipients.Rows)
            {
                if (row.IsNewRow) continue;
                var email = CleanEmail(row.Cells["Email"].Value?.ToString() ?? "");
                if (!string.IsNullOrEmpty(email))
                {
                    counts[email] = counts.TryGetValue(email, out int c) ? c + 1 : 1;
                }
            }

            // Second pass: assign n = (total - 1) duplicates to each row
            foreach (DataGridViewRow row in dgvRecipients.Rows)
            {
                if (row.IsNewRow) continue;
                var email = CleanEmail(row.Cells["Email"].Value?.ToString() ?? "");
                int total = (!string.IsNullOrEmpty(email) && counts.TryGetValue(email, out int c)) ? c : 0;
                int n = total > 1 ? (total - 1) : 0;
                row.Cells["Duplicates"].Value = n;
            }

            dgvRecipients.Invalidate();
        }

        private int DeduplicateRecipients()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toRemove = new List<DataGridViewRow>();

            foreach (DataGridViewRow row in dgvRecipients.Rows)
            {
                if (row.IsNewRow) continue;
                var email = CleanEmail(row.Cells["Email"].Value?.ToString() ?? "");
                if (string.IsNullOrEmpty(email) || !seen.Add(email))
                {
                    toRemove.Add(row);
                }
                else
                {
                    row.Cells["Email"].Value   = email;
                    row.Cells["Company"].Value = ExtractCompany(email);
                }
            }

            foreach (var row in toRemove)
            {
                dgvRecipients.Rows.Remove(row);
            }

            UpdateRecipientCount();
            UpdateDuplicateCounts();
            return toRemove.Count;
        }

        private void RefreshCompanyColumn()
        {
            foreach (DataGridViewRow row in dgvRecipients.Rows)
            {
                if (row.IsNewRow) continue;
                var email = CleanEmail(row.Cells["Email"].Value?.ToString() ?? "");
                row.Cells["Company"].Value = !string.IsNullOrEmpty(email) ? ExtractCompany(email) : "";
            }
            UpdateRecipientCount();
            UpdateDuplicateCounts();
        }

        private void ApplySorting()
        {
            var rows = dgvRecipients.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => (
                    Send:    (bool)(r.Cells["Send"].Value ?? false),
                    Email:   CleanEmail(r.Cells["Email"].Value?.ToString() ?? ""),
                    Company: r.Cells["Company"].Value?.ToString() ?? ""
                ))
                .Where(r => !string.IsNullOrEmpty(r.Email))
                .ToList();

            switch (cmbGroupBy.SelectedIndex)
            {
                case 1: rows = rows.OrderBy(r => r.Company).ThenBy(r => r.Email).ToList();      break;
                case 2: rows = rows.OrderBy(r => r.Email).ToList();                              break;
                case 3: rows = rows.OrderByDescending(r => r.Email).ToList();                   break;
            }

            dgvRecipients.Rows.Clear();
            foreach (var (send, email, company) in rows)
            {
                int idx = dgvRecipients.Rows.Add();
                dgvRecipients.Rows[idx].Cells["Send"].Value       = send;
                dgvRecipients.Rows[idx].Cells["Email"].Value      = email;
                dgvRecipients.Rows[idx].Cells["Company"].Value    = company;
                dgvRecipients.Rows[idx].Cells["Duplicates"].Value = 0;
            }
            UpdateRecipientCount();
            UpdateDuplicateCounts();
        }

        private HashSet<string> GetCurrentEmails()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow r in dgvRecipients.Rows)
            {
                if (r.IsNewRow) continue;
                var email = CleanEmail(r.Cells["Email"].Value?.ToString() ?? "");
                if (!string.IsNullOrEmpty(email))
                    set.Add(email);
            }
            return set;
        }

        private static string CleanEmail(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var match = System.Text.RegularExpressions.Regex.Match(raw, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            return match.Success ? match.Value.ToLowerInvariant() : "";
        }

        private void UpdateRecipientCount()
        {
            int total = 0;
            int sendCount = 0;
            foreach (DataGridViewRow r in dgvRecipients.Rows)
            {
                if (r.IsNewRow) continue;
                var email = r.Cells["Email"].Value?.ToString() ?? "";
                if (email.Contains('@'))
                {
                    total++;
                    bool isChecked = (r.Cells["Send"].Value as bool?) ?? false;
                    if (isChecked) sendCount++;
                }
            }
            lblRecipientCount.Text = $"Recipients: {total} ({sendCount} to send)";
        }

        private static string ExtractCompany(string email)
        {
            if (!email.Contains('@')) return "";
            var domain = email.Split('@')[1].ToLowerInvariant();
            var parts  = domain.Split('.');
            return (parts.Length >= 2
                ? string.Join(".", parts.Take(parts.Length - 1))
                : domain).ToUpper();
        }

        // ════════════════════════════════════════════════════════════════
        //  BODY LOGIC
        // ════════════════════════════════════════════════════════════════
        private void BtnLoadHtmlFile_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Title = "Select HTML Body File", Filter = "HTML Files|*.html;*.htm|All Files|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                rtbHtmlBody.Text = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                SetStatus($"Loaded: {Path.GetFileName(dlg.FileName)}");
            }
        }

        private void BtnPreviewHtml_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(rtbHtmlBody.Text))
            {
                MessageBox.Show("No HTML body to preview.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var tmp = Path.Combine(Path.GetTempPath(), "email_preview.html");
            File.WriteAllText(tmp, rtbHtmlBody.Text, Encoding.UTF8);
            OpenInBrowser(tmp);
        }

        // ════════════════════════════════════════════════════════════════
        //  ATTACHMENTS LOGIC
        // ════════════════════════════════════════════════════════════════
        private void BtnAddAttachment_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Title = "Select File(s) to Attach", Filter = "All Files|*.*", Multiselect = true };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (var f in dlg.FileNames)
                {
                    if (!_attachments.Contains(f))
                    {
                        _attachments.Add(f);
                        lstAttachments.Items.Add(f);
                    }
                }
                SetStatus($"{_attachments.Count} attachment(s) configured.");
            }
        }

        private void RemoveSelectedAttachments()
        {
            var selected = lstAttachments.SelectedItems.Cast<string>().ToList();
            foreach (var f in selected)
            {
                _attachments.Remove(f);
                lstAttachments.Items.Remove(f);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  SEND LOGIC
        // ════════════════════════════════════════════════════════════════
        private void UpdateSendSummary()
        {
            int total = 0;
            int sendCount = 0;
            foreach (DataGridViewRow r in dgvRecipients.Rows)
            {
                if (r.IsNewRow) continue;
                var email = r.Cells["Email"].Value?.ToString() ?? "";
                if (email.Contains('@'))
                {
                    total++;
                    bool isChecked = (r.Cells["Send"].Value as bool?) ?? false;
                    if (isChecked) sendCount++;
                }
            }

            lblSumDetails.Text =
                $"From: {txtSenderEmail.Text}  |  Recipients: {sendCount} of {total} selected  |  Attachments: {_attachments.Count}  |  Subject: \"{txtSubject.Text}\"";
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtSenderEmail.Text) || string.IsNullOrWhiteSpace(txtAppPassword.Text))
            {
                MessageBox.Show("Please fill in Sender Email and App Password in the Config tab.", "Missing Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSubject.Text))
            {
                MessageBox.Show("Email subject is empty.", "Missing Subject", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(rtbHtmlBody.Text))
            {
                MessageBox.Show("Email body is empty.", "Missing Body", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var recipients = dgvRecipients.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow && ((r.Cells["Send"].Value as bool?) == true))
                .Select(r => r.Cells["Email"].Value?.ToString()?.Trim() ?? "")
                .Where(r => r.Contains('@'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count == 0)
            {
                MessageBox.Show("No checked recipients found to send to.\nPlease check the box next to at least one recipient.", "No Recipients Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Auto-save settings, body, and recipients when send is triggered
            SaveSettings(showNotification: false);

            var confirm = MessageBox.Show(
                $"Send to {recipients.Count} recipient(s)?\n\nFrom:    {txtSenderEmail.Text}\nSubject: {txtSubject.Text}",
                "Confirm Send", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            // Snapshot settings
            _isSending        = true;
            _cts              = new CancellationTokenSource();
            btnSend.Enabled   = false;
            btnCancel.Enabled = true;

            rtbLog.Clear();
            pbProgress.Minimum = 0;
            pbProgress.Maximum = recipients.Count;
            pbProgress.Value   = 0;

            int  sent = 0, failed = 0;
            var  random      = new Random();
            var  htmlBody    = rtbHtmlBody.Text;
            var  subject     = txtSubject.Text;
            var  senderEmail = txtSenderEmail.Text.Trim();
            var  senderName  = txtSenderName.Text.Trim();
            var  appPassword = txtAppPassword.Text;
            var  smtpHost    = txtSmtpHost.Text.Trim();
            var  smtpPort    = (int)nudSmtpPort.Value;
            var  minDelay    = (int)nudMinDelay.Value;
            var  maxDelay    = (int)nudMaxDelay.Value;
            var  attachments = _attachments.ToList();

            AppendLog($"═══ Starting — {recipients.Count} recipients   {DateTime.Now:HH:mm:ss} ═══",
                      Color.FromArgb(96, 165, 250));

            await Task.Run(async () =>
            {
                for (int i = 0; i < recipients.Count; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    var toEmail = recipients[i];
                    try
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(senderName, senderEmail));
                        message.To.Add(new MailboxAddress(string.Empty, toEmail));
                        message.Subject = subject;

                        var uniqueBody = htmlBody +
                            $"<div style=\"display:none;color:transparent;width:0;height:0;\">{Guid.NewGuid()}</div>";
                        var builder = new BodyBuilder { HtmlBody = uniqueBody };

                        foreach (var att in attachments)
                            if (File.Exists(att))
                                builder.Attachments.Add(att);

                        message.Body = builder.ToMessageBody();

                        using var client = new SmtpClient();
                        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls, _cts.Token);
                        await client.AuthenticateAsync(senderEmail, appPassword, _cts.Token);
                        await client.SendAsync(message, _cts.Token);
                        await client.DisconnectAsync(true, _cts.Token);

                        sent++;
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] ✔  {i + 1}/{recipients.Count}  →  {toEmail}",
                                  Color.FromArgb(134, 239, 172));

                        Invoke((MethodInvoker)(() =>
                        {
                            MarkEmailAsSent(toEmail);
                        }));
                    }
                    catch (OperationCanceledException)
                    {
                        AppendLog("⏹  Sending cancelled by user.", Color.FromArgb(251, 191, 36));
                        break;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] ✘  {i + 1}/{recipients.Count}  →  {toEmail}  ({ex.Message})",
                                  Color.FromArgb(252, 165, 165));
                    }

                    Invoke((MethodInvoker)(() =>
                    {
                        pbProgress.Value = Math.Min(i + 1, recipients.Count);
                        lblProgress.Text = $"{i + 1} / {recipients.Count} processed   ✔ {sent} sent   ✘ {failed} failed";
                        SetStatus($"Sending... {i + 1}/{recipients.Count}");
                    }));

                    if (i < recipients.Count - 1 && !_cts.Token.IsCancellationRequested)
                    {
                        int delay = maxDelay > minDelay ? random.Next(minDelay, maxDelay + 1) : minDelay;
                        if (delay > 0)
                        {
                            AppendLog($"         ⏳  Waiting {delay}s before next email...", Color.FromArgb(148, 163, 184));
                            await Task.Delay(TimeSpan.FromSeconds(delay), _cts.Token).ContinueWith(_ => { });
                        }
                    }
                }

                AppendLog($"═══ Done  |  Sent: {sent}  |  Failed: {failed}  |  Total: {recipients.Count} ═══",
                          Color.FromArgb(96, 165, 250));
            });

            _isSending        = false;
            btnSend.Enabled   = true;
            btnCancel.Enabled = false;
            SetStatus($"Done — Sent: {sent}  |  Failed: {failed}  |  Total: {recipients.Count}");
        }

        private void MarkEmailAsSent(string sentEmail)
        {
            if (string.IsNullOrWhiteSpace(sentEmail)) return;

            // 1. Uncheck Send in the grid
            foreach (DataGridViewRow row in dgvRecipients.Rows)
            {
                if (row.IsNewRow) continue;
                var email = CleanEmail(row.Cells["Email"].Value?.ToString() ?? "");
                if (string.Equals(email, sentEmail, StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["Send"].Value = false;
                }
            }
            UpdateRecipientCount();

            // 2. Prepend '#' before this email in the paste box (rtbPaste)
            if (!string.IsNullOrEmpty(rtbPaste.Text))
            {
                var lines = rtbPaste.Lines;
                bool modified = false;
                for (int idx = 0; idx < lines.Length; idx++)
                {
                    var line = lines[idx];
                    var trimmed = line.Trim();
                    if (trimmed.IndexOf(sentEmail, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!trimmed.StartsWith("#"))
                        {
                            int indentLen = line.Length - line.TrimStart().Length;
                            string indent = line.Substring(0, indentLen);
                            string rest = line.Substring(indentLen);
                            lines[idx] = indent + "#" + rest;
                            modified = true;
                        }
                    }
                }
                if (modified)
                {
                    rtbPaste.Lines = lines;
                }
            }

            // 3. Persist immediately to settings.json
            SaveSettings(showNotification: false);
        }

        private void AppendLog(string text, Color color)
        {
            if (rtbLog.InvokeRequired) { rtbLog.Invoke((MethodInvoker)(() => AppendLog(text, color))); return; }
            rtbLog.SelectionStart  = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor  = color;
            rtbLog.AppendText(text + Environment.NewLine);
            rtbLog.SelectionColor  = rtbLog.ForeColor;
            rtbLog.ScrollToCaret();
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════
        private void AddLabel(Panel pnl, string text, int x, int y)
        {
            pnl.Controls.Add(new Label
            {
                Text      = text,
                Location  = new Point(x, y + 3),
                AutoSize  = true,
                ForeColor = Color.FromArgb(51, 65, 85),
                Font      = new Font("Segoe UI", 9.5f)
            });
        }

        private void AddSectionLabel(Panel pnl, string text, ref int y)
        {
            y += 10;
            pnl.Controls.Add(new Label
            {
                Text      = text,
                Location  = new Point(20, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            });
            pnl.Controls.Add(new Panel
            {
                Location  = new Point(20, y + 22),
                Size      = new Size(720, 2),
                BackColor = Color.FromArgb(226, 232, 240)
            });
            y += 34;
        }

        private TextBox AddTextBox(Panel pnl, int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location    = new Point(x, y),
                Width       = width,
                Font        = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnl.Controls.Add(txt);
            return txt;
        }

        private Button Btn(string text, Color bg)
        {
            var b = new Button
            {
                Text      = text,
                Width     = 160,
                Height    = 32,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Button SmBtn(string text, Color bg, int x, int y)
        {
            var b = new Button
            {
                Text      = text,
                Location  = new Point(x, y),
                Width     = 72,
                Height    = 26,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void SetStatus(string text)
        {
            if (statusStrip.InvokeRequired)
                statusStrip.Invoke((MethodInvoker)(() => tsslStatus.Text = text));
            else
                tsslStatus.Text = text;
        }

        // ════════════════════════════════════════════════════════════════
        //  PERSISTENCE (SETTINGS & DATA)
        // ════════════════════════════════════════════════════════════════
        private static string GetSettingsFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopBulkEmailSender");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "settings.json");
        }

        private void SaveSettings(bool showNotification = true)
        {
            try
            {
                var recipientItems = new List<RecipientItem>();
                foreach (DataGridViewRow r in dgvRecipients.Rows)
                {
                    if (r.IsNewRow) continue;
                    var clean = CleanEmail(r.Cells["Email"].Value?.ToString() ?? "");
                    if (string.IsNullOrEmpty(clean)) continue;

                    recipientItems.Add(new RecipientItem
                    {
                        Email     = clean,
                        IsEnabled = (r.Cells["Send"].Value as bool?) ?? true
                    });
                }

                var settings = new AppSettings
                {
                    SenderEmail   = txtSenderEmail.Text.Trim(),
                    SenderName    = txtSenderName.Text.Trim(),
                    AppPassword   = txtAppPassword.Text,
                    SmtpHost      = txtSmtpHost.Text.Trim(),
                    SmtpPort      = (int)nudSmtpPort.Value,
                    MinDelay      = (int)nudMinDelay.Value,
                    MaxDelay      = (int)nudMaxDelay.Value,
                    Subject       = txtSubject.Text,
                    HtmlBody      = rtbHtmlBody.Text,
                    Attachments   = _attachments.ToList(),
                    Recipients    = recipientItems,
                    RawPasteText  = rtbPaste.Text,
                    GroupByIndex  = cmbGroupBy.SelectedIndex
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(GetSettingsFilePath(), json, Encoding.UTF8);

                SetStatus($"Settings & data saved ({DateTime.Now:HH:mm:ss})");

                if (showNotification)
                {
                    MessageBox.Show(
                        "All configurations, email content, and recipients have been saved successfully!\nThey will be loaded automatically next time.",
                        "Saved Successfully",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (showNotification)
                {
                    MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadSettings()
        {
            try
            {
                var file = GetSettingsFilePath();
                if (!File.Exists(file)) return;

                var json = File.ReadAllText(file, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return;

                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings == null) return;

                // Config tab
                if (!string.IsNullOrEmpty(settings.SenderEmail))
                    txtSenderEmail.Text = settings.SenderEmail;
                if (!string.IsNullOrEmpty(settings.SenderName))
                    txtSenderName.Text = settings.SenderName;
                if (!string.IsNullOrEmpty(settings.AppPassword))
                    txtAppPassword.Text = settings.AppPassword;
                if (!string.IsNullOrEmpty(settings.SmtpHost))
                    txtSmtpHost.Text = settings.SmtpHost;
                if (settings.SmtpPort > 0)
                    nudSmtpPort.Value = Math.Clamp(settings.SmtpPort, (int)nudSmtpPort.Minimum, (int)nudSmtpPort.Maximum);
                if (settings.MinDelay >= nudMinDelay.Minimum && settings.MinDelay <= nudMinDelay.Maximum)
                    nudMinDelay.Value = settings.MinDelay;
                if (settings.MaxDelay >= nudMaxDelay.Minimum && settings.MaxDelay <= nudMaxDelay.Maximum)
                    nudMaxDelay.Value = settings.MaxDelay;

                // Subject & Body
                if (!string.IsNullOrEmpty(settings.Subject))
                    txtSubject.Text = settings.Subject;
                if (!string.IsNullOrEmpty(settings.HtmlBody))
                    rtbHtmlBody.Text = settings.HtmlBody;

                // Attachments
                _attachments.Clear();
                lstAttachments.Items.Clear();
                if (settings.Attachments != null)
                {
                    foreach (var att in settings.Attachments)
                    {
                        if (File.Exists(att))
                        {
                            _attachments.Add(att);
                            lstAttachments.Items.Add(att);
                        }
                    }
                }

                // Raw paste box
                if (!string.IsNullOrEmpty(settings.RawPasteText))
                    rtbPaste.Text = settings.RawPasteText;

                // Recipients
                dgvRecipients.Rows.Clear();
                if (settings.Recipients != null && settings.Recipients.Count > 0)
                {
                    foreach (var item in settings.Recipients)
                    {
                        var clean = CleanEmail(item.Email);
                        if (string.IsNullOrWhiteSpace(clean)) continue;

                        int idx = dgvRecipients.Rows.Add();
                        dgvRecipients.Rows[idx].Cells["Send"].Value       = item.IsEnabled;
                        dgvRecipients.Rows[idx].Cells["Email"].Value      = clean;
                        dgvRecipients.Rows[idx].Cells["Company"].Value    = ExtractCompany(clean);
                        dgvRecipients.Rows[idx].Cells["Duplicates"].Value = 0;
                    }
                }

                // Sorting / Grouping
                if (settings.GroupByIndex >= 0 && settings.GroupByIndex < cmbGroupBy.Items.Count)
                {
                    cmbGroupBy.SelectedIndex = settings.GroupByIndex;
                    ApplySorting();
                }
                UpdateRecipientCount();
                UpdateDuplicateCounts();

                SetStatus("Loaded saved settings and recipients.");
            }
            catch (Exception ex)
            {
                SetStatus($"Could not load previous settings: {ex.Message}");
            }
        }

        private static void OpenInBrowser(string urlOrPath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = urlOrPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open:\n{ex.Message}", "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isSending)
            {
                if (MessageBox.Show("Emails are still sending. Cancel and exit?", "Exit",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                _cts?.Cancel();
            }
            SaveSettings(showNotification: false);
            base.OnFormClosing(e);
        }
    }
}

