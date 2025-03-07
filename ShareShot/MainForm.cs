using System;
using System.Drawing;
using System.Windows.Forms;
using ShareShot.Core;
using ShareShot.Interfaces;
using ShareShot.Services;

namespace ShareShot
{
    public partial class MainForm : Form
    {
        #region Fields
        private NotifyIcon? trayIcon;
        private IKeyboardHook? keyboardHook;
        private IScreenshotService? screenshotService;
        #endregion

        public MainForm()
        {
            DPIHelper.SetDPIAwareness();
            InitializeComponent();
            InitializeTrayIcon();
            InitializeServices();
            ConfigureForm();
        }

        private void ConfigureForm()
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(1, 1);
            Location = new Point(-100, -100);
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = Constants.UI.ApplicationName,
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Capture Screenshot", null, (_, _) => screenshotService?.CaptureScreen());
            contextMenu.Items.Add("Exit", null, (_, _) => Application.Exit());
            trayIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializeServices()
        {
            try
            {
                keyboardHook = new KeyboardHook();
                keyboardHook.KeyPressed += (_, _) => screenshotService?.CaptureScreen();
                screenshotService = new ScreenshotService(trayIcon);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing services: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            keyboardHook?.Dispose();
            trayIcon?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
