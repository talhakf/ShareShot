using System;
using System.Drawing;
using System.Windows.Forms;
using ShareShot.Core;

namespace ShareShot.Forms
{
    public class SelectionForm : Form
    {
        private readonly Bitmap screenCapture;
        private readonly Rectangle totalBounds;
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelecting;
        private bool isDrawing;
        private Bitmap? result;

        public event Action<Bitmap>? ScreenshotTaken;

        public SelectionForm(Bitmap screenCapture, Rectangle totalBounds)
        {
            this.screenCapture = screenCapture;
            this.totalBounds = totalBounds;
            ConfigureForm();
            SetupEventHandlers();
        }

        private void ConfigureForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            BackColor = Color.Black;
            Opacity = 0.5;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            Bounds = totalBounds;
            StartPosition = FormStartPosition.Manual;
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
            base.OnPaint(e);
            if (isDrawing)
            {
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
                    CaptureSelectedArea();
                }

                Close();
            }
        }

        private bool IsValidSelection()
        {
            return selectionRect.Width > Constants.Screenshot.MinimumSelectionSize && 
                   selectionRect.Height > Constants.Screenshot.MinimumSelectionSize;
        }

        private void CaptureSelectedArea()
        {
            result = new Bitmap(selectionRect.Width, selectionRect.Height);
            using var g = Graphics.FromImage(result);
            g.DrawImage(screenCapture,
                new Rectangle(0, 0, selectionRect.Width, selectionRect.Height),
                new Rectangle(selectionRect.X, selectionRect.Y, selectionRect.Width, selectionRect.Height),
                GraphicsUnit.Pixel);
            ScreenshotTaken?.Invoke(result);
        }
    }
} 