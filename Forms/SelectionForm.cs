using System;
using System.Drawing;
using System.Windows.Forms;
using ShareShot.Core;

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
        private Bitmap? bufferBitmap;
        private Graphics? bufferGraphics;

        public event Action<Rectangle>? ScreenshotTaken;

        public SelectionForm(Bitmap screenCapture, Rectangle totalBounds)
        {
            this.screenCapture = screenCapture;
            this.totalBounds = totalBounds;
            ConfigureForm();
            SetupEventHandlers();
            InitializeBuffer();
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

        private void InitializeBuffer()
        {
            bufferBitmap = new Bitmap(Width, Height);
            bufferGraphics = Graphics.FromImage(bufferBitmap);
            bufferGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            bufferGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            if (bufferGraphics == null || bufferBitmap == null) return;

            // Clear the buffer
            bufferGraphics.Clear(Color.Black);
            
            // Draw the captured screen
            bufferGraphics.DrawImage(screenCapture, 0, 0);
            
            // Draw semi-transparent overlay
            using var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            bufferGraphics.FillRectangle(brush, 0, 0, Width, Height);

            if (isDrawing)
            {
                // Clear the selection area to show the original content
                bufferGraphics.SetClip(selectionRect);
                bufferGraphics.DrawImage(screenCapture, 0, 0);
                bufferGraphics.ResetClip();

                // Draw selection border
                using var pen = new Pen(Color.Red, 2);
                bufferGraphics.DrawRectangle(pen, selectionRect);
            }

            // Draw the buffer to the screen
            e.Graphics.DrawImage(bufferBitmap, 0, 0);
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
                    ScreenshotTaken?.Invoke(selectionRect);
                }

                Close();
            }
        }

        private bool IsValidSelection()
        {
            return selectionRect.Width > Constants.Screenshot.MinimumSelectionSize && 
                   selectionRect.Height > Constants.Screenshot.MinimumSelectionSize;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bufferGraphics?.Dispose();
                bufferBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 