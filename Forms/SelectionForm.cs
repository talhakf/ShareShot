using System;
using System.Drawing;
using System.Windows.Forms;
using ShareShot.Core;
using ShareShot.Services;

namespace ShareShot.Forms
{
    public class SelectionForm : Form
    {
        private readonly Rectangle totalBounds;
        private readonly Bitmap screenCapture;
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelecting;
        private bool isDrawing;
        private Bitmap? selectedScreenshot;

        public event Action<Rectangle>? ScreenshotTaken;

        public SelectionForm(Bitmap screenCapture, Rectangle totalBounds)
        {
            this.screenCapture = screenCapture;
            this.totalBounds = totalBounds;
            ConfigureForm();
            SetupEventHandlers();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        private void ConfigureForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            BackColor = Color.Black;
            Opacity = 1.0;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            Bounds = totalBounds;
            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint, true);
        }

        private void SetupEventHandlers()
        {
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }

        // draw captured screen and selection rect
        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw the captured screen
            e.Graphics.DrawImage(screenCapture, 0, 0);
            
            // Draw semi-transparent overlay
            using var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            e.Graphics.FillRectangle(brush, 0, 0, Width, Height);

            if (isDrawing)
            {
                // Clear the selection area to show the original content
                e.Graphics.SetClip(selectionRect);
                e.Graphics.DrawImage(screenCapture, 0, 0);
                e.Graphics.ResetClip();

                // Draw selection border
                using var pen = new Pen(Color.Red, 2);
                e.Graphics.DrawRectangle(pen, selectionRect);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selectionRect = new Rectangle(startPoint, Size.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                isDrawing = true;
                UpdateSelectionRect(e.Location);
                Invalidate();
            }
        }

        private void UpdateSelectionRect(Point currentPoint)
        {
            selectionRect = new Rectangle(
                Math.Min(startPoint.X, currentPoint.X),
                Math.Min(startPoint.Y, currentPoint.Y),
                Math.Abs(currentPoint.X - startPoint.X),
                Math.Abs(currentPoint.Y - startPoint.Y)
            );
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                isSelecting = false;
                isDrawing = false;

                if (IsValidSelection())
                {
                    var adjustedRect = new Rectangle(
                        selectionRect.X + 2,
                        selectionRect.Y + 2,
                        selectionRect.Width - 4,
                        selectionRect.Height - 4
                    );

                    selectedScreenshot = new Bitmap(adjustedRect.Width, adjustedRect.Height);
                    using var g = Graphics.FromImage(selectedScreenshot);
                    g.DrawImage(screenCapture,
                        new Rectangle(0, 0, adjustedRect.Width, adjustedRect.Height),
                        adjustedRect,
                        GraphicsUnit.Pixel);

                    try
                    {
                        Clipboard.SetImage(selectedScreenshot);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to copy screenshot to clipboard: {ex.Message}", 
                            "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    ScreenshotTaken?.Invoke(adjustedRect);
                    var screenshot = selectedScreenshot;
                    Close();

                    if (screenshot != null)
                    {
                        try
                        {
                            var clientId = ConfigurationService.Instance.GetImgurClientId();
                            var optionsForm = new ScreenshotOptionsForm(screenshot, clientId);
                            optionsForm.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error showing options: {ex.Message}", "Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        private bool IsValidSelection()
        {
            return selectionRect.Width > Constants.Screenshot.MinimumSelectionSize && 
                   selectionRect.Height > Constants.Screenshot.MinimumSelectionSize;
        }
    }
} 