using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace RetroPalette
{
    public partial class Form1 : Form
    {
        private PixelCanvas canvas;
        private bool isMiddleMouseDown = false;
        private Point lastMousePosition;
        private Tool currentTool = Tool.Pen;
        private Panel toolPanel;
        private Form fileMenu = null;
        private bool isClosingFileMenu = false;

        public Form1()
        {
            InitializeComponent();
            InitializeCanvas();
            InitializeUI();

            this.Resize += Form1_Resize;
        }

        private void InitializeCanvas()
        {
            canvas = new PixelCanvas(16, 16);
            canvas.Dock = DockStyle.Fill;
            canvas.BackColor = ColorTranslator.FromHtml("#292929");
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            
            Panel canvasContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#292929"),
                Padding = new Padding(10)
            };
            canvasContainer.Controls.Add(canvas);
            this.Controls.Add(canvasContainer);
        }

        private void InitializeUI()
        {
            // Create top menu panel
            Panel topMenu = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = SystemColors.Control
            };

            Button fileButton = new Button
            {
                Text = "File",
                Width = 60,
                Height = 25,
                Location = new Point(5, 2)
            };
            fileButton.Click += FileButton_Click;
            topMenu.Controls.Add(fileButton);

            // Create right tool panel
            toolPanel = new Panel
            {
                Width = 150,
                Dock = DockStyle.Right,
                BackColor = SystemColors.Control,
                Padding = new Padding(10)
            };

            // Add tool buttons
            Button penButton = new Button
            {
                Text = "Pen",
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10)
            };
            penButton.Click += (s, e) => currentTool = Tool.Pen;

            Button eraserButton = new Button
            {
                Text = "Eraser",
                Dock = DockStyle.Top,
                Height = 40
            };
            eraserButton.Click += (s, e) => currentTool = Tool.Eraser;

            toolPanel.Controls.Add(eraserButton);
            toolPanel.Controls.Add(penButton);

            this.Controls.Add(toolPanel);
            this.Controls.Add(topMenu);
        }

        private void CloseFileMenu()
        {
            if (fileMenu != null && !fileMenu.IsDisposed)
            {
                isClosingFileMenu = true;
                Form menuToClose = fileMenu;
                fileMenu = null;
                menuToClose.Close();
                isClosingFileMenu = false;
            }
        }

        private void FileButton_Click(object sender, EventArgs e)
        {
            if (fileMenu != null && !fileMenu.IsDisposed)
            {
                CloseFileMenu();
                return;
            }

            fileMenu = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(150, 120),
                Owner = this
            };

            Button newSpriteBtn = new Button
            {
                Text = "New sprite",
                Dock = DockStyle.Top,
                Height = 40
            };
            newSpriteBtn.Click += NewSprite_Click;

            Button openSpriteBtn = new Button
            {
                Text = "Open sprite",
                Dock = DockStyle.Top,
                Height = 40
            };
            openSpriteBtn.Click += OpenSprite_Click;

            Button exportSpriteBtn = new Button
            {
                Text = "Export sprite",
                Dock = DockStyle.Top,
                Height = 40
            };
            exportSpriteBtn.Click += ExportSprite_Click;

            fileMenu.Controls.Add(exportSpriteBtn);
            fileMenu.Controls.Add(openSpriteBtn);
            fileMenu.Controls.Add(newSpriteBtn);

            Point buttonLocation = ((Button)sender).PointToScreen(Point.Empty);
            fileMenu.Location = new Point(buttonLocation.X, buttonLocation.Y + ((Button)sender).Height);

            fileMenu.Deactivate += (s, args) => 
            {
                if (!isClosingFileMenu)
                {
                    CloseFileMenu();
                }
            };
            
            fileMenu.Show();
        }

        private void NewSprite_Click(object sender, EventArgs e)
        {
            try
            {
                // Close the file menu first
                CloseFileMenu();

                if (canvas.HasContent())
                {
                    DialogResult result = MessageBox.Show(this, 
                        "Do you want to save the current sprite?", 
                        "Save Changes", 
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        ExportSprite_Click(sender, e);
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                
                canvas.Clear();
                canvas.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Error creating new sprite: {ex.Message}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void OpenSprite_Click(object sender, EventArgs e)
        {
            try
            {
                CloseFileMenu();

                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
                    openFileDialog.Title = "Select an image";

                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        if (File.Exists(openFileDialog.FileName))
                        {
                            canvas.LoadImage(openFileDialog.FileName);
                        }
                        else
                        {
                            MessageBox.Show("Selected file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportSprite_Click(object sender, EventArgs e)
        {
            try
            {
                CloseFileMenu();

                using (Form exportForm = new Form())
                {
                    exportForm.Text = "Export Settings";
                    exportForm.Size = new Size(300, 150);
                    exportForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    exportForm.StartPosition = FormStartPosition.CenterParent;
                    exportForm.MaximizeBox = false;
                    exportForm.MinimizeBox = false;
                    exportForm.Owner = this;

                    NumericUpDown scaleInput = new NumericUpDown
                    {
                        Location = new Point(120, 20),
                        Minimum = 1,
                        Maximum = 10,
                        Value = 1
                    };

                    exportForm.Controls.Add(new Label
                    {
                        Text = "Scale (1-10):",
                        Location = new Point(20, 22)
                    });
                    exportForm.Controls.Add(scaleInput);

                    Button exportButton = new Button
                    {
                        Text = "Export",
                        Location = new Point(100, 70)
                    };
                    exportButton.Click += (s, args) =>
                    {
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "PNG Image|*.png";
                            saveFileDialog.Title = "Save sprite as";
                            if (saveFileDialog.ShowDialog(exportForm) == DialogResult.OK)
                            {
                                canvas.ExportImage(saveFileDialog.FileName, (int)scaleInput.Value);
                                exportForm.Close();
                            }
                        }
                    };
                    exportForm.Controls.Add(exportButton);

                    exportForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting sprite: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
