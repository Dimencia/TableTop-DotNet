using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace TableTop
{
    
    public partial class FormMain : Form
    {
        // This form will contain a ZoomableGrid, Menus at the top, a chat bar at the bottom, and any relevant 'windows' we come up with
        // Other than menus, all of these will be resizeable and relocateable, and this should handle that
        // It will also make calls to the netcode and handle user settings

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        public ZoomableGrid ZoomGrid;
        private string Username = "MingeBag";
        private Color FontColor;
        private Color NameColor;
        private Font ChatFont = SystemFonts.DefaultFont;
        private MenuStrip MenuStripMain;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem changeNameToolStripMenuItem;
        private ColorDialog _ColorDialog;
        private TextBox ChatEntryBox;
        private RichTextBox ChatBox;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel LabelStatusLeft;
        private ToolStripStatusLabel LabelStatusRight;
        private ToolStripDropDownButton toolStripDropDownButton1;
        private ToolStripMenuItem changeNameColorToolStripMenuItem;
        private ToolStripMenuItem changeFontColorToolStripMenuItem;
        private bool AutoScrollOnChat = true; // We can change this later with a button if we want

        public FormMain()
        {
            InitializeComponent();

            SuspendLayout(); // No need for it to screw with anything until we're done adding them

            ZoomGrid = new ZoomableGrid();
            ZoomGrid.Anchor = AnchorStyles.None;
            ZoomGrid.Dock = DockStyle.Fill;

            Controls.Add(ZoomGrid);

            // Menus are already on here from the Designer
            // Chat box too, but
            // TODO: Make chat box its own object so it can be resizeable and moveable and contain the input box within itself
            // And contain color selection and stuff
            ChatBox.MouseDown += _ChatBox_MouseDown;
            ChatEntryBox.KeyDown += _KeyDown;
            ChatBox.ReadOnly = true;

            ResumeLayout();

            Random seed = new Random();
            FontColor = Color.FromArgb(seed.Next(255), seed.Next(255), seed.Next(255));
            NameColor = Color.FromArgb(seed.Next(255), seed.Next(255), seed.Next(255));
            ChatFont = new Font(ChatFont.FontFamily, 11f);

            if (!Properties.Settings.Default.Username.Equals(""))
            {
                Username = Properties.Settings.Default.Username;
            }
            else
                Properties.Settings.Default.Username = Username;
            if (!Properties.Settings.Default.NameColor.Equals(""))
            {
                NameColor = ColorTranslator.FromHtml(Properties.Settings.Default.NameColor);
            }
            else
                Properties.Settings.Default.NameColor = ColorTranslator.ToHtml(NameColor);
            if (!Properties.Settings.Default.FontColor.Equals(""))
            {
                FontColor = ColorTranslator.FromHtml(Properties.Settings.Default.FontColor);
            }
            else
                Properties.Settings.Default.FontColor = ColorTranslator.ToHtml(FontColor);
            Properties.Settings.Default.Save();

            ZoomGrid.TimerMain.Tick += OnTick;
            ZoomGrid.MouseMove += _MouseMove;
            ZoomGrid.ZoomChanged += _ZoomChanged;
        }

        private void _ZoomChanged(object sender, EventArgs e)
        {
            LabelStatusLeft.Text = "Zoom: " + ZoomGrid.zoomMult + "x";
            
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            // We want to get the coords of the mouse and put them in a status, and refresh it
            LabelStatusRight.Text = Math.Round(ZoomGrid.PositionInternal.X,2) + ", " + Math.Round(ZoomGrid.PositionInternal.Y,2);
            LabelStatusRight.Invalidate();
        }

        private void OnTick(object sender, EventArgs e)
        {
            
        }

        private void _ChatBox_MouseDown(object sender, MouseEventArgs e)
        {
            HideCaret(ChatBox.Handle);
        }

        private void _KeyDown(object sender, KeyEventArgs e)
        {
            if (sender.Equals(ChatEntryBox))
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
                {
                    ChatBox.AppendText(Username + ": ", NameColor, new Font(ChatFont, FontStyle.Bold));
                    ChatBox.AppendText(ChatEntryBox.Text + "\n", FontColor, ChatFont);
                    if(AutoScrollOnChat)
                        ChatBox.ScrollToCaret();
                    ChatEntryBox.ResetText();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        public string QuestionDialog(string question, string defaultText = "")
        {
            using (Form testDialog = new Form())
            {
                Label qLabel = new Label();
                qLabel.Text = question;
                TextBox responseBox = new TextBox();
                responseBox.Text = defaultText;
                Button submitButton = new Button();
                submitButton.Text = "Submit";

                FlowLayoutPanel formPanel = new FlowLayoutPanel();
                formPanel.FlowDirection = FlowDirection.TopDown;
                formPanel.Controls.Add(qLabel);
                formPanel.Controls.Add(responseBox);
                formPanel.Controls.Add(submitButton);
                testDialog.Controls.Add(formPanel);
                testDialog.AcceptButton = submitButton;
                formPanel.Dock = DockStyle.Fill;
                submitButton.DialogResult = DialogResult.OK;

                // Show testDialog as a modal dialog and determine if DialogResult = OK.
                if (testDialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Read the contents of testDialog's TextBox.
                    return testDialog.Controls[0].Controls[1].Text; // Since we made this thing, we know the text box is control #2 inside the panel, which is control #1
                }
                else
                {
                    return "";
                }
            }
        }

        public void MessageChatWindow(string s)
        {
            // Normally the onsubmit function of the chat entry box should do this
            // This is for code calls where I want to display something
            Invoke((MethodInvoker)delegate
            {
                ChatBox.AppendText(s + "\n");
                ChatBox.ScrollToCaret();
            });
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.MenuStripMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._ColorDialog = new System.Windows.Forms.ColorDialog();
            this.ChatEntryBox = new System.Windows.Forms.TextBox();
            this.ChatBox = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.LabelStatusLeft = new System.Windows.Forms.ToolStripStatusLabel();
            this.LabelStatusRight = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.changeFontColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeNameColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripMain.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuStripMain
            // 
            this.MenuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.MenuStripMain.Location = new System.Drawing.Point(5, 5);
            this.MenuStripMain.Name = "MenuStripMain";
            this.MenuStripMain.Size = new System.Drawing.Size(1070, 24);
            this.MenuStripMain.TabIndex = 0;
            this.MenuStripMain.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeNameToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // changeNameToolStripMenuItem
            // 
            this.changeNameToolStripMenuItem.Name = "changeNameToolStripMenuItem";
            this.changeNameToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.changeNameToolStripMenuItem.Text = "Change Name...";
            this.changeNameToolStripMenuItem.Click += new System.EventHandler(this.changeNameToolStripMenuItem_Click);
            // 
            // ChatEntryBox
            // 
            this.ChatEntryBox.AcceptsTab = true;
            this.ChatEntryBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ChatEntryBox.Location = new System.Drawing.Point(2, 580);
            this.ChatEntryBox.Name = "ChatEntryBox";
            this.ChatEntryBox.Size = new System.Drawing.Size(1073, 20);
            this.ChatEntryBox.TabIndex = 1;
            // 
            // ChatBox
            // 
            this.ChatBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ChatBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.ChatBox.Location = new System.Drawing.Point(2, 455);
            this.ChatBox.Name = "ChatBox";
            this.ChatBox.ReadOnly = true;
            this.ChatBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.ChatBox.Size = new System.Drawing.Size(1073, 125);
            this.ChatBox.TabIndex = 2;
            this.ChatBox.Text = "";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LabelStatusLeft,
            this.LabelStatusRight,
            this.toolStripDropDownButton1});
            this.statusStrip1.Location = new System.Drawing.Point(5, 600);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1070, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // LabelStatusLeft
            // 
            this.LabelStatusLeft.Name = "LabelStatusLeft";
            this.LabelStatusLeft.Size = new System.Drawing.Size(0, 17);
            // 
            // LabelStatusRight
            // 
            this.LabelStatusRight.Name = "LabelStatusRight";
            this.LabelStatusRight.Size = new System.Drawing.Size(995, 17);
            this.LabelStatusRight.Spring = true;
            this.LabelStatusRight.Text = "LabelStatusRight";
            this.LabelStatusRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeNameColorToolStripMenuItem,
            this.changeFontColorToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(29, 20);
            this.toolStripDropDownButton1.Text = "toolStripDropDownButton1";
            // 
            // changeFontColorToolStripMenuItem
            // 
            this.changeFontColorToolStripMenuItem.Name = "changeFontColorToolStripMenuItem";
            this.changeFontColorToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.changeFontColorToolStripMenuItem.Text = "Change Font Color";
            this.changeFontColorToolStripMenuItem.Click += new System.EventHandler(this.changeFontColorToolStripMenuItem_Click_1);
            // 
            // changeNameColorToolStripMenuItem
            // 
            this.changeNameColorToolStripMenuItem.Name = "changeNameColorToolStripMenuItem";
            this.changeNameColorToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.changeNameColorToolStripMenuItem.Text = "Change Name Color";
            this.changeNameColorToolStripMenuItem.Click += new System.EventHandler(this.changeNameColorToolStripMenuItem_Click_1);
            // 
            // FormMain
            // 
            this.ClientSize = new System.Drawing.Size(1080, 627);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.ChatBox);
            this.Controls.Add(this.ChatEntryBox);
            this.Controls.Add(this.MenuStripMain);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.MenuStripMain;
            this.Name = "FormMain";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.Text = "TabletTop.Net";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.MenuStripMain.ResumeLayout(false);
            this.MenuStripMain.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void changeNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string result = QuestionDialog("New Name:", Username);
            if (!result.Equals(""))
            {
                Username = result;
                Properties.Settings.Default.Username = Username;
                Properties.Settings.Default.Save();
            }
        }

        private void changeFontColorToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            _ColorDialog.Color = FontColor;
            if (_ColorDialog.ShowDialog(this) == DialogResult.OK)
            {
                FontColor = _ColorDialog.Color;
                Properties.Settings.Default.FontColor = ColorTranslator.ToHtml(FontColor);
                Properties.Settings.Default.Save();
            }
        }

        private void changeNameColorToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            _ColorDialog.Color = NameColor;
            if (_ColorDialog.ShowDialog(this) == DialogResult.OK)
            {
                NameColor = _ColorDialog.Color;
                Properties.Settings.Default.NameColor = ColorTranslator.ToHtml(NameColor);
                Properties.Settings.Default.Save();
            }
        }
    }
}
