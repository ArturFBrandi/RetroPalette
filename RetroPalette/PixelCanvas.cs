using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace RetroPalette
{
    public class PixelCanvas : Control
    {
        private int gridWidth;
        private int gridHeight;
        private Color[,] pixels;
        private Point viewOffset = new Point(0, 0);
        private float zoom = 1.0f;
        
        private static readonly Color CHECKER_COLOR_1 = ColorTranslator.FromHtml("#dfdfdf");
        private static readonly Color CHECKER_COLOR_2 = ColorTranslator.FromHtml("#9a9a9a");

        public PixelCanvas(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            pixels = new Color[width, height];
            
            // Enable double buffering for smooth rendering
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
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

        public void LoadImage(string filePath)
        {
            using (Bitmap bmp = new Bitmap(filePath))
            {
                // Resize image to fit our grid
                using (Bitmap resized = new Bitmap(gridWidth, gridHeight))
                {
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(bmp, 0, 0, gridWidth, gridHeight);
                    }

                    // Copy pixels to our grid
                    for (int x = 0; x < gridWidth; x++)
                    {
                        for (int y = 0; y < gridHeight; y++)
                        {
                            Color pixel = resized.GetPixel(x, y);
                            pixels[x, y] = pixel.A == 0 ? Color.Empty : pixel;
                        }
                    }
                }
            }
            Invalidate();
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
            
            // Calculate cell size based on control size and zoom
            int cellSize = (int)(Math.Min(Width / gridWidth, Height / gridHeight) * zoom);
            
            // Calculate the starting position to center the grid
            int startX = (Width - (gridWidth * cellSize)) / 2 + viewOffset.X;
            int startY = (Height - (gridHeight * cellSize)) / 2 + viewOffset.Y;

            // Draw checkered background
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

                    // Determine checker pattern color
                    Color backgroundColor = ((x + y) % 2 == 0) ? CHECKER_COLOR_1 : CHECKER_COLOR_2;
                    
                    using (SolidBrush brush = new SolidBrush(backgroundColor))
                    {
                        g.FillRectangle(brush, cellRect);
                    }

                    // Draw pixel if it exists
                    if (pixels[x, y] != Color.Empty)
                    {
                        using (SolidBrush brush = new SolidBrush(pixels[x, y]))
                        {
                            g.FillRectangle(brush, cellRect);
                        }
                    }
                }
            }
        }

        public void DrawPixel(Point location, Color color)
        {
            // Calculate cell size and grid position
            int cellSize = (int)(Math.Min(Width / gridWidth, Height / gridHeight) * zoom);
            int startX = (Width - (gridWidth * cellSize)) / 2 + viewOffset.X;
            int startY = (Height - (gridHeight * cellSize)) / 2 + viewOffset.Y;

            // Convert screen coordinates to grid coordinates
            int gridX = (location.X - startX) / cellSize;
            int gridY = (location.Y - startY) / cellSize;

            // Check if the coordinates are within bounds
            if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
            {
                pixels[gridX, gridY] = color;
                Invalidate();
            }
        }

        public void Pan(int deltaX, int deltaY)
        {
            viewOffset.X += deltaX;
            viewOffset.Y += deltaY;
            Invalidate();
        }
    }
} 