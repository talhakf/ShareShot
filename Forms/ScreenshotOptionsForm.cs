using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace ShareShot.Forms
{
    public class RoundedButton : Button
    {
        private int cornerRadius = 6;
        private Color borderColor = Color.Transparent;
        private Color hoverBackColor = Color.Transparent;
        private Color normalBackColor = Color.Transparent;
        private bool isHovering;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.Transparent);

            var rectPath = new GraphicsPath();
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var diameter = cornerRadius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(rect.Location, size);

            // Top left arc
            rectPath.AddArc(arc, 180, 90);

            // Top right arc
            arc.X = rect.Right - diameter;
            rectPath.AddArc(arc, 270, 90);

            // Bottom right arc
            arc.Y = rect.Bottom - diameter;
            rectPath.AddArc(arc, 0, 90);

            // Bottom left arc
            arc.X = rect.Left;
            rectPath.AddArc(arc, 90, 90);

            rectPath.CloseFigure();

            // Fill button
            using (var brush = new SolidBrush(isHovering ? hoverBackColor : normalBackColor))
            {
                e.Graphics.FillPath(brush, rectPath);
            }

            // Draw text
            var textRect = rect;
            textRect.Inflate(-4, -4);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            rectPath.Dispose();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        public void SetColors(Color normal, Color hover)
        {
            normalBackColor = normal;
            hoverBackColor = hover;
            Invalidate();
        }
    }

    public class CloseButton : Control
    {
        private bool isHovering;
        private readonly int size = 20;

        public CloseButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(size, size);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.Clear(Parent.BackColor);
            
            // Draw hover effect
            if (isHovering)
            {
                using var circleBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
                e.Graphics.FillEllipse(circleBrush, 0, 0, size, size);
            }

            // Draw X
            using var pen = new Pen(Color.FromArgb(161, 161, 170), 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            int padding = 6;
            e.Graphics.DrawLine(pen, padding, padding, size - padding, size - padding);
            e.Graphics.DrawLine(pen, size - padding, padding, padding, size - padding);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
                return cp;
            }
        }
    }

    public class ScreenshotOptionsForm : Form
    {
        // Brand Colors
        private static readonly Color BRAND_PRIMARY = Color.FromArgb(255, 99, 102, 241);    // Main brand color - Indigo
        private static readonly Color BRAND_SECONDARY = Color.FromArgb(255, 67, 56, 202);   // Secondary - Darker indigo
        private static readonly Color BRAND_SUCCESS = Color.FromArgb(255, 34, 197, 94);     // Success green
        private static readonly Color BRAND_ERROR = Color.FromArgb(255, 239, 68, 68);       // Error red
        private static readonly Color BRAND_BACKGROUND = Color.FromArgb(255, 24, 24, 27);   // Dark background
        private static readonly Color BRAND_SURFACE = Color.FromArgb(255, 39, 39, 42);      // Surface color
        private static readonly Color BRAND_TEXT = Color.FromArgb(255, 250, 250, 250);      // Primary text
        private static readonly Color BRAND_TEXT_SECONDARY = Color.FromArgb(255, 161, 161, 170); // Secondary text

        private const int CORNER_RADIUS = 12;
        private const int BUTTON_CORNER_RADIUS = 6;
        private const string APP_NAME = "ShareShot";
        private const string APP_TAGLINE = "Quick && Easy Screenshot Sharing";

        private static readonly HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        private readonly Bitmap screenshot;
        private readonly string clientId;
        private RoundedButton uploadButton;
        private readonly Label statusLabel;
        private readonly Panel mainPanel;
        private readonly Label titleLabel;
        private readonly Label taglineLabel;
        private readonly TableLayoutPanel layoutPanel;
        private bool isUploading;

        public ScreenshotOptionsForm(Bitmap screenshot, string clientId)
        {
            this.screenshot = screenshot;
            this.clientId = clientId;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BRAND_BACKGROUND,
                Padding = new Padding(32, 20, 32, 24)
            };

            layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                RowStyles = {
                    new RowStyle(SizeType.Absolute, 35),  // Title
                    new RowStyle(SizeType.Absolute, 25),  // Tagline
                    new RowStyle(SizeType.Absolute, 50),  // Upload button
                    new RowStyle(SizeType.Absolute, 35),  // Status
                }
            };

            titleLabel = new Label
            {
                Text = APP_NAME,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = BRAND_PRIMARY,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.BottomCenter
            };

            taglineLabel = new Label
            {
                Text = APP_TAGLINE,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = BRAND_TEXT_SECONDARY,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter
            };
            
            uploadButton = CreateBrandButton("Upload to Imgur");

            statusLabel = new Label
            {
                Text = "Screenshot copied to clipboard! Click Upload to share online.",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = BRAND_TEXT_SECONDARY,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Padding = new Padding(0, 5, 0, 5),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter
            };
            
            ConfigureForm();
            InitializeControls();
        }

        private RoundedButton CreateBrandButton(string text)
        {
            var btn = new RoundedButton
            {
                Text = text,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = BRAND_TEXT,
                Dock = DockStyle.Fill,
                Height = 44,
                Margin = new Padding(0, 3, 0, 3),
                Padding = new Padding(16, 10, 16, 10)
            };
            btn.SetColors(BRAND_PRIMARY, BRAND_SECONDARY);
            return btn;
        }

        private void ConfigureForm()
        {
            Text = APP_NAME;
            Icon = SystemIcons.Application;  // You might want to set a custom icon here
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(420, 200);
            Size = new Size(420, 200);
            BackColor = BRAND_BACKGROUND;
            ShowInTaskbar = false;
        }

        private void InitializeControls()
        {
            uploadButton.Click += UploadButton_Click;

            layoutPanel.Controls.Add(titleLabel, 0, 0);
            layoutPanel.Controls.Add(taglineLabel, 0, 1);
            layoutPanel.Controls.Add(uploadButton, 0, 2);
            layoutPanel.Controls.Add(statusLabel, 0, 3);
            layoutPanel.Dock = DockStyle.Fill;

            mainPanel.Controls.Add(layoutPanel);
            Controls.Add(mainPanel);

            var closeButton = new CloseButton
            {
                Location = new Point(Width - 30, 10)
            };
            closeButton.Click += (s, e) => Close();
            Controls.Add(closeButton);

            var dragPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.Transparent,
                Cursor = Cursors.SizeAll
            };
            dragPanel.MouseDown += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    const int WM_NCLBUTTONDOWN = 0xA1;
                    const int HT_CAPTION = 0x2;
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            Controls.Add(dragPanel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            using var path = new GraphicsPath();
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            path.AddPath(RoundedRect(bounds, CORNER_RADIUS), true);
            
            Region = new Region(path);

            // Draw shadow border
            using var pen = new Pen(Color.FromArgb(70, 0, 0, 0), 1);
            e.Graphics.DrawPath(pen, path);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);
            var path = new GraphicsPath();

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        private async void UploadButton_Click(object? sender, EventArgs e)
        {
            if (isUploading)
            {
                MessageBox.Show("An upload is already in progress. Please wait.", 
                    "Upload in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                isUploading = true;
                uploadButton.Enabled = false;
                uploadButton.SetColors(Color.FromArgb(200, 200, 200), Color.FromArgb(200, 200, 200));
                statusLabel.Text = "Uploading to Imgur...";

                // Ensure we have a fresh authorization header
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", $"Client-ID {clientId}");

                // Convert image to base64 string
                string base64Image;
                using (var ms = new MemoryStream())
                {
                    screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    base64Image = Convert.ToBase64String(ms.ToArray());
                }

                // Create form content
                var formData = new Dictionary<string, string>
                {
                    { "image", base64Image },
                    { "type", "base64" }
                };

                var response = await client.PostAsync("https://api.imgur.com/3/image", new FormUrlEncodedContent(formData));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(1);
                    statusLabel.Text = "Rate limit exceeded. Please try again later.";
                    MessageBox.Show($"You've hit Imgur's rate limit. Please wait {retryAfter.TotalMinutes:0} minutes before trying again.", 
                        "Rate Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    EnableUploadButton();
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<ImgurResponse>(responseContent, options);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = result?.Data?.Error ?? result?.Error ?? "No error details available";
                    var statusMessage = $"Upload failed ({response.StatusCode})";
                    var errorMessage = $"Error uploading to Imgur:\nStatus: {response.StatusCode}\nDetails: {errorDetails}\nResponse: {responseContent}";
                    
                    statusLabel.Text = statusMessage;
                    MessageBox.Show(errorMessage, "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EnableUploadButton();
                    return;
                }

                if (result?.Success == true && result.Data != null)
                {
                    var link = result.Data.Link;
                    if (!string.IsNullOrEmpty(link))
                    {
                        Clipboard.SetText(link);
                        ReplaceUploadButtonWithSuccess();
                        statusLabel.ForeColor = BRAND_SUCCESS;
                        statusLabel.Text = "Upload successful! Link copied to clipboard.";
                    }
                    else
                    {
                        throw new Exception("Upload succeeded but no link was returned");
                    }
                }
                else
                {
                    var error = result?.Data?.Error ?? result?.Error ?? "Unknown error";
                    statusLabel.ForeColor = BRAND_ERROR;
                    statusLabel.Text = $"Upload failed: {error}";
                    MessageBox.Show($"Imgur API Error:\n{error}\n\nFull Response:\n{responseContent}", 
                        "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EnableUploadButton();
                }
            }
            catch (TaskCanceledException)
            {
                statusLabel.ForeColor = BRAND_ERROR;
                statusLabel.Text = "Upload timed out";
                MessageBox.Show("The upload timed out. Please try again.", 
                    "Upload Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                EnableUploadButton();
            }
            catch (Exception ex)
            {
                statusLabel.ForeColor = BRAND_ERROR;
                statusLabel.Text = "Upload failed. See error details.";
                MessageBox.Show($"Error uploading screenshot:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EnableUploadButton();
            }
            finally
            {
                isUploading = false;
            }
        }

        private void EnableUploadButton()
        {
            uploadButton.Enabled = true;
            uploadButton.SetColors(BRAND_PRIMARY, BRAND_SECONDARY);
        }

        private void ReplaceUploadButtonWithSuccess()
        {
            layoutPanel.Controls.Remove(uploadButton);
            uploadButton.Dispose();

            var successButton = new RoundedButton
            {
                Text = "✓ Link Copied to Clipboard",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = BRAND_TEXT,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 3),
                Enabled = false
            };
            successButton.SetColors(BRAND_SUCCESS, BRAND_SUCCESS);

            layoutPanel.Controls.Add(successButton, 0, 2);
        }

        private class ImgurResponse
        {
            public bool Success { get; set; }
            public ImgurData? Data { get; set; }
            public string? Error { get; set; }
        }

        private class ImgurData
        {
            public string? Id { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Type { get; set; }
            public string? Link { get; set; }
            public string? Error { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Size { get; set; }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
} 