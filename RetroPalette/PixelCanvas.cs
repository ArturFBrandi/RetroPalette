using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.ComponentModel;
using System.Linq;

namespace RetroPalette
{
    public class PixelCanvas : Control
    {
        private int gridWidth;
        private int gridHeight;
        private Color[,] pixels;
        private Point viewOffset = new Point(0, 0);
        private float zoom = 1.0f;
        private Point lastMousePosition;
        private Point lastZoomCenter;
        private Stack<IUndoAction> undoStack = new Stack<IUndoAction>();
        private List<SinglePixelUndoAction> currentGroupedAction = null;
        private const int MAX_UNDO = 9999;
        
        private static readonly Color CHECKER_COLOR_1 = ColorTranslator.FromHtml("#dfdfdf");
        private static readonly Color CHECKER_COLOR_2 = ColorTranslator.FromHtml("#9a9a9a");

        // Marquee selection properties
        private Point? selectionStart = null;
        private Point? selectionEnd = null;
        private Rectangle? selectionRect = null;
        private Dictionary<Point, Color> selectedPixels = new Dictionary<Point, Color>();
        private Point? dragStart = null;
        private bool isDraggingSelection = false;
        private Dictionary<Point, Color> clipboardPixels = null;

        public bool HasSelection => selectionRect.HasValue;
        public bool IsDraggingSelection => isDraggingSelection;
        public bool HasClipboardContent => clipboardPixels != null && clipboardPixels.Count > 0;

        private interface IUndoAction
        {
            void Undo(Color[,] pixels);
        }

        private class SinglePixelUndoAction : IUndoAction
        {
            public Point Position { get; set; }
            public Color OldColor { get; set; }
            public Color NewColor { get; set; }

            public SinglePixelUndoAction(Point position, Color oldColor, Color newColor)
            {
                Position = position;
                OldColor = oldColor;
                NewColor = newColor;
            }

            public void Undo(Color[,] pixels)
            {
                pixels[Position.X, Position.Y] = OldColor;
            }
        }

        private class GroupedUndoAction : IUndoAction
        {
            private readonly List<SinglePixelUndoAction> actions;

            public GroupedUndoAction(List<SinglePixelUndoAction> actions)
            {
                this.actions = actions;
            }

            public void Undo(Color[,] pixels)
            {
                foreach (var action in actions)
                {
                    action.Undo(pixels);
                }
            }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Gets or sets the minimum zoom level")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public float MinZoom { get; set; } = 0.25f;

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Gets or sets the maximum zoom level")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public float MaxZoom { get; set; } = 10.0f;

        public bool CanUndo => undoStack.Count > 0;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public PixelCanvas(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            pixels = new Color[width, height];
            
            // Enable double buffering for smooth rendering
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.MouseWheel += PixelCanvas_MouseWheel;
        }

        private void PixelCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoom;
            
            // Calculate zoom factor
            if (e.Delta > 0)
                zoom = Math.Min(zoom * 1.2f, MaxZoom);
            else
                zoom = Math.Max(zoom / 1.2f, MinZoom);

            if (oldZoom != zoom)
            {
                // Store mouse position relative to control
                lastZoomCenter = e.Location;
                
                // Adjust view offset to keep the point under cursor fixed
                float zoomFactor = zoom / oldZoom;
                int deltaX = (int)(lastZoomCenter.X - (lastZoomCenter.X * zoomFactor));
                int deltaY = (int)(lastZoomCenter.Y - (lastZoomCenter.Y * zoomFactor));
                
                viewOffset.X += deltaX;
                viewOffset.Y += deltaY;
                
                Invalidate();
            }
        }

        public bool HasContent()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (pixels[x, y] != Color.Empty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Clear()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    pixels[x, y] = Color.Empty;
                }
            }
            Invalidate();
        }

        public Color GetPixelColor(Point location)
        {
            var gridCoords = ScreenToGrid(location);
            if (gridCoords.X >= 0 && gridCoords.X < gridWidth && 
                gridCoords.Y >= 0 && gridCoords.Y < gridHeight)
            {
                return pixels[gridCoords.X, gridCoords.Y];
            }
            return Color.Empty;
        }

        public void FloodFill(Point location, Color targetColor)
        {
            var gridCoords = ScreenToGrid(location);
            if (gridCoords.X >= 0 && gridCoords.X < gridWidth &&
                gridCoords.Y >= 0 && gridCoords.Y < gridHeight)
            {
                Color oldColor = pixels[gridCoords.X, gridCoords.Y];
                if (oldColor == targetColor) return; // No need to fill if colors match

                Stack<Point> pixelsToCheck = new Stack<Point>();
                pixelsToCheck.Push(gridCoords);
                List<SinglePixelUndoAction> fillActions = new List<SinglePixelUndoAction>();

                while (pixelsToCheck.Count > 0)
                {
                    Point current = pixelsToCheck.Pop();
                    if (current.X < 0 || current.X >= gridWidth ||
                        current.Y < 0 || current.Y >= gridHeight)
                        continue;

                    if (pixels[current.X, current.Y] != oldColor)
                        continue;

                    fillActions.Add(new SinglePixelUndoAction(current, pixels[current.X, current.Y], targetColor));
                    pixels[current.X, current.Y] = targetColor;

                    pixelsToCheck.Push(new Point(current.X + 1, current.Y));
                    pixelsToCheck.Push(new Point(current.X - 1, current.Y));
                    pixelsToCheck.Push(new Point(current.X, current.Y + 1));
                    pixelsToCheck.Push(new Point(current.X, current.Y - 1));
                }

                // Add all fill actions as a single grouped undo action
                if (fillActions.Count > 0)
                {
                    AddUndoAction(new GroupedUndoAction(fillActions));
                }

                Invalidate();
            }
        }

        public Point ScreenToGrid(Point screenPoint)
        {
            int cellSize = (int)(Math.Min(Width / gridWidth, Height / gridHeight) * zoom);
            int startX = (Width - (gridWidth * cellSize)) / 2 + viewOffset.X;
            int startY = (Height - (gridHeight * cellSize)) / 2 + viewOffset.Y;

            return new Point(
                (screenPoint.X - startX) / cellSize,
                (screenPoint.Y - startY) / cellSize
            );
        }

        public void LoadImage(string filePath)
        {
            int originalWidth;
            int originalHeight;
            
            using (Bitmap bmp = new Bitmap(filePath))
            {
                originalWidth = bmp.Width;
                originalHeight = bmp.Height;
                
                int newWidth = bmp.Width;
                int newHeight = bmp.Height;
                bool needsScaling = false;

                // Check if image is larger than 1024x1024
                if (bmp.Width > 1024 || bmp.Height > 1024)
                {
                    needsScaling = true;
                    newWidth = bmp.Width / 10;
                    newHeight = bmp.Height / 10;
                }

                // Update canvas size to match image dimensions
                gridWidth = newWidth;
                gridHeight = newHeight;
                pixels = new Color[newWidth, newHeight];

                if (needsScaling)
                {
                    // Create a scaled-down version of the image
                    using (Bitmap scaledBmp = new Bitmap(bmp, newWidth, newHeight))
                    {
                        // Copy pixels from the scaled image
                        for (int x = 0; x < newWidth; x++)
                        {
                            for (int y = 0; y < newHeight; y++)
                            {
                                Color pixel = scaledBmp.GetPixel(x, y);
                                pixels[x, y] = pixel.A == 0 ? Color.Empty : pixel;
                            }
                        }
                    }
                }
                else
                {
                    // Copy pixels directly without resizing
                    for (int x = 0; x < newWidth; x++)
                    {
                        for (int y = 0; y < newHeight; y++)
                        {
                            Color pixel = bmp.GetPixel(x, y);
                            pixels[x, y] = pixel.A == 0 ? Color.Empty : pixel;
                        }
                    }
                }
            }
            Invalidate();

            // Show a message if the image was scaled down
            if (gridWidth != originalWidth || gridHeight != originalHeight)
            {
                MessageBox.Show(
                    $"The image was automatically scaled down from {originalWidth}x{originalHeight} to {gridWidth}x{gridHeight} because it exceeded the maximum recommended size.",
                    "Image Scaled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        public void ExportImage(string filePath, int scale)
        {
            using (Bitmap bmp = new Bitmap(gridWidth * scale, gridHeight * scale))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    
                    // Draw pixels
                    for (int x = 0; x < gridWidth; x++)
                    {
                        for (int y = 0; y < gridHeight; y++)
                        {
                            if (pixels[x, y] != Color.Empty)
                            {
                                using (SolidBrush brush = new SolidBrush(pixels[x, y]))
                                {
                                    g.FillRectangle(brush, x * scale, y * scale, scale, scale);
                                }
                            }
                        }
                    }
                }
                bmp.Save(filePath, ImageFormat.Png);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            
            // Calculate cell size based on control size and zoom
            int cellSize = (int)(Math.Min(Width / gridWidth, Height / gridHeight) * zoom);
            
            // Calculate the starting position to center the grid
            int startX = (Width - (gridWidth * cellSize)) / 2 + viewOffset.X;
            int startY = (Height - (gridHeight * cellSize)) / 2 + viewOffset.Y;

            // Draw checkered background and base pixels
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Rectangle cellRect = new Rectangle(
                        startX + (x * cellSize),
                        startY + (y * cellSize),
                        cellSize,
                        cellSize
                    );

                    // Determine checker pattern color based on pattern size
                    Color backgroundColor = (((x / BackgroundSettings.PatternSize) + (y / BackgroundSettings.PatternSize)) % 2 == 0) 
                        ? BackgroundSettings.Color1 
                        : BackgroundSettings.Color2;
                    
                    using (SolidBrush brush = new SolidBrush(backgroundColor))
                    {
                        g.FillRectangle(brush, cellRect);
                    }

                    // Draw base pixel if it exists
                    Point currentPoint = new Point(x, y);
                    if (pixels[x, y] != Color.Empty)
                    {
                        // Only skip drawing if this pixel is part of the selection and we're dragging
                        bool isPartOfDraggedSelection = isDraggingSelection && 
                            selectedPixels.ContainsKey(currentPoint) && 
                            selectedPixels[currentPoint] != Color.Empty;

                        if (!isPartOfDraggedSelection)
                        {
                            using (SolidBrush brush = new SolidBrush(pixels[x, y]))
                            {
                                g.FillRectangle(brush, cellRect);
                            }
                        }
                    }
                }
            }

            // Draw selected pixels
            if (HasSelection)
            {
                foreach (var kvp in selectedPixels)
                {
                    if (IsValidGridCoordinate(kvp.Key) && kvp.Value != Color.Empty)
                    {
                        Rectangle cellRect = new Rectangle(
                            startX + (kvp.Key.X * cellSize),
                            startY + (kvp.Key.Y * cellSize),
                            cellSize,
                            cellSize
                        );

                        using (SolidBrush brush = new SolidBrush(kvp.Value))
                        {
                            g.FillRectangle(brush, cellRect);
                        }
                    }
                }
            }

            // Draw selection rectangle if it exists
            if (selectionRect.HasValue)
            {
                Rectangle visualRect = new Rectangle(
                    startX + (selectionRect.Value.X * cellSize),
                    startY + (selectionRect.Value.Y * cellSize),
                    (selectionRect.Value.Width + 1) * cellSize,
                    (selectionRect.Value.Height + 1) * cellSize
                );

                // Draw semi-transparent overlay for selected area
                using (SolidBrush selectionBrush = new SolidBrush(Color.FromArgb(32, 0, 120, 215)))
                {
                    g.FillRectangle(selectionBrush, visualRect);
                }

                // Draw selection border
                using (Pen selectionPen = new Pen(Color.FromArgb(255, 0, 120, 215), 2))
                {
                    selectionPen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(selectionPen, visualRect);
                }
            }
        }

        public void DrawPixel(Point location, Color color)
        {
            var gridCoords = ScreenToGrid(location);
            if (gridCoords.X >= 0 && gridCoords.X < gridWidth && 
                gridCoords.Y >= 0 && gridCoords.Y < gridHeight)
            {
                Color oldColor = pixels[gridCoords.X, gridCoords.Y];
                if (oldColor != color) // Only add to undo stack if the color actually changes
                {
                    if (currentGroupedAction == null)
                    {
                        currentGroupedAction = new List<SinglePixelUndoAction>();
                    }
                    currentGroupedAction.Add(new SinglePixelUndoAction(gridCoords, oldColor, color));
                    pixels[gridCoords.X, gridCoords.Y] = color;
                    Invalidate();
                }
            }
        }

        public void Pan(int deltaX, int deltaY)
        {
            viewOffset.X += deltaX;
            viewOffset.Y += deltaY;
            Invalidate();
        }

        public void Undo()
        {
            if (CanUndo)
            {
                IUndoAction action = undoStack.Pop();
                action.Undo(pixels);
                Invalidate();
            }
        }

        private void AddUndoAction(IUndoAction action)
        {
            if (undoStack.Count >= MAX_UNDO)
            {
                // Remove oldest action if we've reached the limit
                var tempStack = new Stack<IUndoAction>();
                for (int i = 0; i < MAX_UNDO - 1; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack.Clear();
                while (tempStack.Count > 0)
                {
                    undoStack.Push(tempStack.Pop());
                }
            }
            undoStack.Push(action);
        }

        public void BeginInteraction()
        {
            currentGroupedAction = new List<SinglePixelUndoAction>();
        }

        public void EndInteraction()
        {
            if (currentGroupedAction != null && currentGroupedAction.Count > 0)
            {
                AddUndoAction(new GroupedUndoAction(currentGroupedAction));
                currentGroupedAction = null;
            }
        }

        public void StartMarqueeSelection(Point location)
        {
            var gridCoords = ScreenToGrid(location);
            if (IsValidGridCoordinate(gridCoords))
            {
                selectionStart = gridCoords;
                selectionEnd = gridCoords;
                UpdateSelectionRect();
                selectedPixels.Clear();
                isDraggingSelection = false;
                Invalidate();
            }
        }

        public void UpdateMarqueeSelection(Point location)
        {
            var gridCoords = ScreenToGrid(location);
            if (IsValidGridCoordinate(gridCoords) && selectionStart.HasValue)
            {
                selectionEnd = gridCoords;
                UpdateSelectionRect();
                Invalidate();
            }
        }

        public void EndMarqueeSelection()
        {
            if (selectionRect.HasValue)
            {
                // Store selected pixels
                selectedPixels.Clear();
                for (int x = selectionRect.Value.Left; x <= selectionRect.Value.Right; x++)
                {
                    for (int y = selectionRect.Value.Top; y <= selectionRect.Value.Bottom; y++)
                    {
                        if (IsValidGridCoordinate(new Point(x, y)))
                        {
                            selectedPixels[new Point(x, y)] = pixels[x, y];
                        }
                    }
                }
            }
        }

        public void StartDraggingSelection(Point location)
        {
            var gridCoords = ScreenToGrid(location);
            if (selectionRect.HasValue && IsValidGridCoordinate(gridCoords))
            {
                if (IsPointInSelection(gridCoords))
                {
                    // Store only non-empty pixels in the selection
                    var newSelectedPixels = new Dictionary<Point, Color>();
                    foreach (var kvp in selectedPixels)
                    {
                        if (kvp.Value != Color.Empty)
                        {
                            newSelectedPixels[kvp.Key] = kvp.Value;
                            // Clear the original position
                            pixels[kvp.Key.X, kvp.Key.Y] = Color.Empty;
                        }
                    }
                    selectedPixels = newSelectedPixels;
                    
                    isDraggingSelection = true;
                    dragStart = gridCoords;
                }
            }
        }

        public void DragSelection(Point location)
        {
            if (isDraggingSelection && dragStart.HasValue && selectionRect.HasValue)
            {
                var gridCoords = ScreenToGrid(location);
                if (IsValidGridCoordinate(gridCoords))
                {
                    int deltaX = gridCoords.X - dragStart.Value.X;
                    int deltaY = gridCoords.Y - dragStart.Value.Y;

                    // Update selection rectangle
                    selectionRect = new Rectangle(
                        selectionRect.Value.X + deltaX,
                        selectionRect.Value.Y + deltaY,
                        selectionRect.Value.Width,
                        selectionRect.Value.Height
                    );

                    // Update start and end points
                    selectionStart = new Point(selectionStart.Value.X + deltaX, selectionStart.Value.Y + deltaY);
                    selectionEnd = new Point(selectionEnd.Value.X + deltaX, selectionEnd.Value.Y + deltaY);

                    // Update selected pixels positions (only non-empty pixels)
                    var newSelectedPixels = new Dictionary<Point, Color>();
                    foreach (var kvp in selectedPixels)
                    {
                        if (kvp.Value != Color.Empty)
                        {
                            Point newPos = new Point(
                                kvp.Key.X + deltaX,
                                kvp.Key.Y + deltaY
                            );
                            if (IsValidGridCoordinate(newPos))
                            {
                                newSelectedPixels[newPos] = kvp.Value;
                            }
                        }
                    }
                    selectedPixels = newSelectedPixels;

                    dragStart = gridCoords;
                    Invalidate();
                }
            }
        }

        public void EndDraggingSelection()
        {
            if (isDraggingSelection)
            {
                isDraggingSelection = false;
                dragStart = null;

                // Create undo action for the move
                var actions = new List<SinglePixelUndoAction>();

                // Store the old pixels that will be overwritten and apply the new pixels
                foreach (var kvp in selectedPixels)
                {
                    if (IsValidGridCoordinate(kvp.Key) && kvp.Value != Color.Empty)
                    {
                        // Add undo action for the destination pixel
                        actions.Add(new SinglePixelUndoAction(kvp.Key, pixels[kvp.Key.X, kvp.Key.Y], kvp.Value));
                        // Apply the new pixel
                        pixels[kvp.Key.X, kvp.Key.Y] = kvp.Value;
                    }
                }

                if (actions.Count > 0)
                {
                    AddUndoAction(new GroupedUndoAction(actions));
                }

                Invalidate();
            }
        }

        public void DeleteSelection()
        {
            if (selectionRect.HasValue && selectedPixels.Count > 0)
            {
                var actions = new List<SinglePixelUndoAction>();

                foreach (var kvp in selectedPixels)
                {
                    if (IsValidGridCoordinate(kvp.Key))
                    {
                        actions.Add(new SinglePixelUndoAction(kvp.Key, kvp.Value, Color.Empty));
                        pixels[kvp.Key.X, kvp.Key.Y] = Color.Empty;
                    }
                }

                if (actions.Count > 0)
                {
                    AddUndoAction(new GroupedUndoAction(actions));
                }

                ClearSelection();
                Invalidate();
            }
        }

        public void ClearSelection()
        {
            selectionStart = null;
            selectionEnd = null;
            selectionRect = null;
            selectedPixels.Clear();
            isDraggingSelection = false;
            dragStart = null;
            Invalidate();
        }

        private void UpdateSelectionRect()
        {
            if (selectionStart.HasValue && selectionEnd.HasValue)
            {
                int x = Math.Min(selectionStart.Value.X, selectionEnd.Value.X);
                int y = Math.Min(selectionStart.Value.Y, selectionEnd.Value.Y);
                int width = Math.Abs(selectionEnd.Value.X - selectionStart.Value.X);
                int height = Math.Abs(selectionEnd.Value.Y - selectionStart.Value.Y);
                selectionRect = new Rectangle(x, y, width, height);
            }
            else
            {
                selectionRect = null;
            }
        }

        public bool IsPointInSelection(Point point)
        {
            return selectionRect.HasValue && selectionRect.Value.Contains(point);
        }

        private bool IsValidGridCoordinate(Point point)
        {
            return point.X >= 0 && point.X < gridWidth && point.Y >= 0 && point.Y < gridHeight;
        }

        public void CopySelection()
        {
            if (HasSelection && selectedPixels.Count > 0)
            {
                clipboardPixels = new Dictionary<Point, Color>();
                Point topLeft = new Point(selectionRect.Value.X, selectionRect.Value.Y);

                // Store pixels relative to top-left corner of selection
                foreach (var kvp in selectedPixels)
                {
                    Point relativePos = new Point(
                        kvp.Key.X - topLeft.X,
                        kvp.Key.Y - topLeft.Y
                    );
                    clipboardPixels[relativePos] = kvp.Value;
                }
            }
        }

        public void PasteFromClipboard(Point targetLocation)
        {
            if (clipboardPixels != null && clipboardPixels.Count > 0)
            {
                // Clear current selection
                ClearSelection();

                // Calculate bounds of pasted content
                int minX = clipboardPixels.Keys.Min(p => p.X);
                int minY = clipboardPixels.Keys.Min(p => p.Y);
                int maxX = clipboardPixels.Keys.Max(p => p.X);
                int maxY = clipboardPixels.Keys.Max(p => p.Y);

                // Create new selection at paste location
                var gridCoords = ScreenToGrid(targetLocation);
                selectionStart = gridCoords;
                selectionEnd = new Point(
                    gridCoords.X + (maxX - minX),
                    gridCoords.Y + (maxY - minY)
                );
                UpdateSelectionRect();

                // Create new selected pixels at target location
                selectedPixels.Clear();
                foreach (var kvp in clipboardPixels)
                {
                    Point newPos = new Point(
                        gridCoords.X + kvp.Key.X,
                        gridCoords.Y + kvp.Key.Y
                    );

                    if (IsValidGridCoordinate(newPos))
                    {
                        selectedPixels[newPos] = kvp.Value;
                    }
                }

                Invalidate();
            }
        }

        public void ApplySelection()
        {
            if (HasSelection && selectedPixels.Count > 0)
            {
                var actions = new List<SinglePixelUndoAction>();

                foreach (var kvp in selectedPixels)
                {
                    if (IsValidGridCoordinate(kvp.Key))
                    {
                        actions.Add(new SinglePixelUndoAction(kvp.Key, pixels[kvp.Key.X, kvp.Key.Y], kvp.Value));
                        pixels[kvp.Key.X, kvp.Key.Y] = kvp.Value;
                    }
                }

                if (actions.Count > 0)
                {
                    AddUndoAction(new GroupedUndoAction(actions));
                }

                Invalidate();
            }
        }

        public void ResizeCanvas(int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0)
                throw new ArgumentException("Canvas dimensions must be greater than 0");

            // Create new pixel array
            Color[,] newPixels = new Color[newWidth, newHeight];

            // Copy existing pixels that fit in the new dimensions
            int copyWidth = Math.Min(gridWidth, newWidth);
            int copyHeight = Math.Min(gridHeight, newHeight);

            for (int x = 0; x < copyWidth; x++)
            {
                for (int y = 0; y < copyHeight; y++)
                {
                    newPixels[x, y] = pixels[x, y];
                }
            }

            // Update dimensions and pixels
            gridWidth = newWidth;
            gridHeight = newHeight;
            pixels = newPixels;

            // Clear selection as it might be invalid now
            ClearSelection();

            // Adjust view to center the new canvas
            Invalidate();
        }

        public void ResizeSprite(int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0)
                throw new ArgumentException("Sprite dimensions must be greater than 0");

            // Create new pixel array
            Color[,] newPixels = new Color[newWidth, newHeight];

            // Calculate scaling factors
            float scaleX = (float)gridWidth / newWidth;
            float scaleY = (float)gridHeight / newHeight;

            // Resize using nearest neighbor interpolation
            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    int sourceX = (int)(x * scaleX);
                    int sourceY = (int)(y * scaleY);

                    // Ensure we don't go out of bounds
                    sourceX = Math.Min(sourceX, gridWidth - 1);
                    sourceY = Math.Min(sourceY, gridHeight - 1);

                    newPixels[x, y] = pixels[sourceX, sourceY];
                }
            }

            // Update dimensions and pixels
            gridWidth = newWidth;
            gridHeight = newHeight;
            pixels = newPixels;

            // Clear selection as it's no longer valid
            ClearSelection();

            // Adjust view to center the new sprite
            Invalidate();
        }
    }
} 