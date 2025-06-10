using System;
using System.Drawing;
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