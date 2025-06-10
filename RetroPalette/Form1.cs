using System;
using System.Drawing;
using System.Windows.Forms;

namespace RetroPalette
{
    public partial class Form1 : Form
    {
        private PixelCanvas canvas;
        private bool isMiddleMouseDown = false;
        private Point lastMousePosition;
        private Tool currentTool = Tool.Pen;

        public Form1()
        {
            InitializeComponent();
            InitializeCanvas();
            InitializeToolStrip();

            this.Resize += Form1_Resize;
        }

        private void InitializeCanvas()
        {
            canvas = new PixelCanvas(16, 16);
            canvas.Dock = DockStyle.Fill;
            canvas.BackColor = Color.DarkGray;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            this.Controls.Add(canvas);
        }

        private void InitializeToolStrip()
        {
            ToolStrip toolStrip = new ToolStrip();
            
            var penButton = new ToolStripButton("Pen");
            penButton.Click += (s, e) => currentTool = Tool.Pen;
            
            var eraserButton = new ToolStripButton("Eraser");
            eraserButton.Click += (s, e) => currentTool = Tool.Eraser;

            toolStrip.Items.AddRange(new ToolStripItem[] { penButton, eraserButton });
            toolStrip.Dock = DockStyle.Top;
            this.Controls.Add(toolStrip);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            canvas.Invalidate();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isMiddleMouseDown = true;
                lastMousePosition = e.Location;
            }
            else if (e.Button == MouseButtons.Left)
            {
                canvas.DrawPixel(e.Location, currentTool == Tool.Pen ? Color.Black : Color.Empty);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMiddleMouseDown)
            {
                int deltaX = e.X - lastMousePosition.X;
                int deltaY = e.Y - lastMousePosition.Y;
                canvas.Pan(deltaX, deltaY);
                lastMousePosition = e.Location;
            }
            else if (e.Button == MouseButtons.Left)
            {
                canvas.DrawPixel(e.Location, currentTool == Tool.Pen ? Color.Black : Color.Empty);
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isMiddleMouseDown = false;
            }
        }
    }

    public enum Tool
    {
        Pen,
        Eraser
    }
}
