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
        private Form spriteMenu = null;
        private Form preferencesMenu = null;
        private bool isClosingFileMenu = false;
        private bool isClosingSpriteMenu = false;
        private bool isClosingPreferencesMenu = false;
        private ColorSelector colorSelector;
        private bool isAltPressed = false;
        private Tool previousTool;

        public Form1()
        {
            InitializeComponent();
            InitializeCanvas();
            InitializeUI();

            this.Resize += Form1_Resize;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
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

            Button spriteButton = new Button
            {
                Text = "Sprite",
                Width = 60,
                Height = 25,
                Location = new Point(70, 2)
            };
            spriteButton.Click += SpriteButton_Click;
            topMenu.Controls.Add(spriteButton);

            Button preferencesButton = new Button
            {
                Text = "Preferences",
                Width = 80,
                Height = 25,
                Location = new Point(135, 2)
            };
            preferencesButton.Click += PreferencesButton_Click;
            topMenu.Controls.Add(preferencesButton);

            // Create color selector panel
            Panel leftPanel = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = SystemColors.Control,
                Padding = new Padding(10)
            };

            colorSelector = new ColorSelector();
            colorSelector.Dock = DockStyle.Top;
            colorSelector.ColorChanged += (s, color) => { /* Color will be used by tools */ };
            leftPanel.Controls.Add(colorSelector);

            // Create right tool panel
            toolPanel = new Panel
            {
                Width = 150,
                Dock = DockStyle.Right,
                BackColor = SystemColors.Control,
                Padding = new Padding(10)
            };

            // Add tool buttons
            Button marqueeButton = new Button
            {
                Text = "Marquee (M)",
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10)
            };
            marqueeButton.Click += (s, e) => {
                currentTool = Tool.Marquee;
                UpdateToolButtonStates();
            };

            Button penButton = new Button
            {
                Text = "Pen (B)",
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10)
            };
            penButton.Click += (s, e) => {
                currentTool = Tool.Pen;
                UpdateToolButtonStates();
            };

            Button eraserButton = new Button
            {
                Text = "Eraser (E)",
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10)
            };
            eraserButton.Click += (s, e) => {
                currentTool = Tool.Eraser;
                UpdateToolButtonStates();
            };

            Button bucketButton = new Button
            {
                Text = "Bucket (G)",
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10)
            };
            bucketButton.Click += (s, e) => {
                currentTool = Tool.Bucket;
                UpdateToolButtonStates();
            };

            Button pickerButton = new Button
            {
                Text = "Color Picker (Alt)",
                Dock = DockStyle.Top,
                Height = 40
            };
            pickerButton.Click += (s, e) => {
                currentTool = Tool.ColorPicker;
                UpdateToolButtonStates();
            };

            toolPanel.Controls.Add(pickerButton);
            toolPanel.Controls.Add(bucketButton);
            toolPanel.Controls.Add(eraserButton);
            toolPanel.Controls.Add(penButton);
            toolPanel.Controls.Add(marqueeButton);

            this.Controls.Add(toolPanel);
            this.Controls.Add(leftPanel);
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

        private void CloseSpriteMenu()
        {
            if (spriteMenu != null && !spriteMenu.IsDisposed)
            {
                isClosingSpriteMenu = true;
                Form menuToClose = spriteMenu;
                spriteMenu = null;
                menuToClose.Close();
                isClosingSpriteMenu = false;
            }
        }

        private void ClosePreferencesMenu()
        {
            if (preferencesMenu != null && !preferencesMenu.IsDisposed)
            {
                isClosingPreferencesMenu = true;
                Form menuToClose = preferencesMenu;
                preferencesMenu = null;
                menuToClose.Close();
                isClosingPreferencesMenu = false;
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

        private void SpriteButton_Click(object sender, EventArgs e)
        {
            if (spriteMenu != null && !spriteMenu.IsDisposed)
            {
                CloseSpriteMenu();
                return;
            }

            Button btn = (Button)sender;
            Point menuPosition = PointToScreen(new Point(btn.Left, btn.Bottom));

            spriteMenu = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = menuPosition,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                BackColor = SystemColors.Control,
                Size = new Size(150, 70)
            };

            Button resizeCanvasButton = new Button
            {
                Text = "Resize Canvas",
                Dock = DockStyle.Top,
                Height = 30
            };
            resizeCanvasButton.Click += (s, args) =>
            {
                CloseSpriteMenu();
                ShowResizeDialog(true);
            };

            Button resizeSpriteButton = new Button
            {
                Text = "Resize Sprite",
                Dock = DockStyle.Top,
                Height = 30
            };
            resizeSpriteButton.Click += (s, args) =>
            {
                CloseSpriteMenu();
                ShowResizeDialog(false);
            };

            spriteMenu.Controls.Add(resizeSpriteButton);
            spriteMenu.Controls.Add(resizeCanvasButton);

            spriteMenu.Deactivate += (s, args) => 
            {
                if (!isClosingSpriteMenu)
                {
                    CloseSpriteMenu();
                }
            };

            spriteMenu.Show(this);
        }

        private void ShowResizeDialog(bool isCanvas)
        {
            using (Form resizeForm = new Form())
            {
                resizeForm.Text = isCanvas ? "Resize Canvas" : "Resize Sprite";
                resizeForm.Size = new Size(300, 150);
                resizeForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                resizeForm.StartPosition = FormStartPosition.CenterParent;
                resizeForm.MaximizeBox = false;
                resizeForm.MinimizeBox = false;
                resizeForm.Owner = this;

                // Width input
                Label widthLabel = new Label
                {
                    Text = "Width:",
                    Location = new Point(20, 20),
                    AutoSize = true
                };

                NumericUpDown widthInput = new NumericUpDown
                {
                    Location = new Point(120, 18),
                    Minimum = 1,
                    Maximum = 8192,
                    Value = canvas.GridWidth
                };

                // Height input
                Label heightLabel = new Label
                {
                    Text = "Height:",
                    Location = new Point(20, 50),
                    AutoSize = true
                };

                NumericUpDown heightInput = new NumericUpDown
                {
                    Location = new Point(120, 48),
                    Minimum = 1,
                    Maximum = 8192,
                    Value = canvas.GridHeight
                };

                Button resizeButton = new Button
                {
                    Text = "Resize",
                    Location = new Point(100, 80)
                };

                resizeButton.Click += (s, args) =>
                {
                    try
                    {
                        int newWidth = (int)widthInput.Value;
                        int newHeight = (int)heightInput.Value;

                        if (isCanvas)
                        {
                            canvas.ResizeCanvas(newWidth, newHeight);
                        }
                        else
                        {
                            canvas.ResizeSprite(newWidth, newHeight);
                        }

                        // Adjust pattern size if it's larger than the new dimensions
                        int maxPatternSize = Math.Min(newWidth, newHeight);
                        if (BackgroundSettings.PatternSize > maxPatternSize)
                        {
                            BackgroundSettings.PatternSize = maxPatternSize;
                            canvas.Invalidate();
                        }

                        resizeForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, 
                            $"Error resizing: {ex.Message}", 
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                    }
                };

                resizeForm.Controls.AddRange(new Control[] { 
                    widthLabel, widthInput, 
                    heightLabel, heightInput, 
                    resizeButton 
                });

                resizeForm.ShowDialog(this);
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
                if (currentTool == Tool.Marquee)
                {
                    if (canvas.HasSelection && canvas.IsPointInSelection(canvas.ScreenToGrid(e.Location)))
                    {
                        canvas.StartDraggingSelection(e.Location);
                    }
                    else
                    {
                        canvas.StartMarqueeSelection(e.Location);
                    }
                }
                else
                {
                    canvas.BeginInteraction();
                    switch (currentTool)
                    {
                        case Tool.Pen:
                            canvas.DrawPixel(e.Location, colorSelector.CurrentColor);
                            break;
                        case Tool.Eraser:
                            canvas.DrawPixel(e.Location, Color.Empty);
                            break;
                        case Tool.Bucket:
                            canvas.FloodFill(e.Location, colorSelector.CurrentColor);
                            break;
                        case Tool.ColorPicker:
                            Color pickedColor = canvas.GetPixelColor(e.Location);
                            if (pickedColor != Color.Empty)
                            {
                                colorSelector.CurrentColor = pickedColor;
                            }
                            break;
                    }
                }
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
                if (currentTool == Tool.Marquee)
                {
                    if (canvas.IsDraggingSelection)
                    {
                        canvas.DragSelection(e.Location);
                    }
                    else
                    {
                        canvas.UpdateMarqueeSelection(e.Location);
                    }
                }
                else
                {
                    switch (currentTool)
                    {
                        case Tool.Pen:
                            canvas.DrawPixel(e.Location, colorSelector.CurrentColor);
                            break;
                        case Tool.Eraser:
                            canvas.DrawPixel(e.Location, Color.Empty);
                            break;
                    }
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isMiddleMouseDown = false;
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (currentTool == Tool.Marquee)
                {
                    if (canvas.IsDraggingSelection)
                    {
                        canvas.EndDraggingSelection();
                    }
                    else
                    {
                        canvas.EndMarqueeSelection();
                    }
                }
                else
                {
                    canvas.EndInteraction();
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && !isAltPressed)
            {
                isAltPressed = true;
                previousTool = currentTool;
                currentTool = Tool.ColorPicker;
                UpdateToolButtonStates();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                canvas.Undo();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                if (currentTool == Tool.Marquee && canvas.HasSelection)
                {
                    canvas.CopySelection();
                    e.Handled = true;
                }
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                if (canvas.HasClipboardContent)
                {
                    currentTool = Tool.Marquee;
                    UpdateToolButtonStates();
                    canvas.PasteFromClipboard(PointToClient(Cursor.Position));
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                if (currentTool == Tool.Marquee && canvas.HasSelection)
                {
                    canvas.DeleteSelection();
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (canvas.HasSelection)
                {
                    canvas.ClearSelection();
                    e.Handled = true;
                }
            }
            else if (!e.Alt)
            {
                switch (char.ToLower((char)e.KeyCode))
                {
                    case 'b':
                        currentTool = Tool.Pen;
                        UpdateToolButtonStates();
                        break;
                    case 'e':
                        currentTool = Tool.Eraser;
                        UpdateToolButtonStates();
                        break;
                    case 'g':
                        currentTool = Tool.Bucket;
                        UpdateToolButtonStates();
                        break;
                    case 'm':
                        currentTool = Tool.Marquee;
                        UpdateToolButtonStates();
                        break;
                }
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (!e.Alt && isAltPressed) // Changed condition to check !e.Alt
            {
                isAltPressed = false;
                currentTool = previousTool;
                UpdateToolButtonStates();
                e.Handled = true;
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            // Reset ALT state when window loses focus
            if (isAltPressed)
            {
                isAltPressed = false;
                currentTool = previousTool;
                UpdateToolButtonStates();
            }
        }

        private void UpdateToolButtonStates()
        {
            foreach (Control control in toolPanel.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = button.Text.ToLower().Contains(currentTool.ToString().ToLower()) 
                        ? SystemColors.ControlDark 
                        : SystemColors.Control;
                }
            }
        }

        private void PreferencesButton_Click(object sender, EventArgs e)
        {
            if (preferencesMenu != null && !preferencesMenu.IsDisposed)
            {
                ClosePreferencesMenu();
                return;
            }

            Button btn = (Button)sender;
            Point menuPosition = PointToScreen(new Point(btn.Left, btn.Bottom));

            preferencesMenu = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = menuPosition,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                BackColor = SystemColors.Control,
                Size = new Size(150, 40)
            };

            Button backgroundPatternButton = new Button
            {
                Text = "Background Pattern",
                Dock = DockStyle.Top,
                Height = 30
            };
            backgroundPatternButton.Click += (s, args) =>
            {
                ClosePreferencesMenu();
                ShowBackgroundPatternDialog();
            };

            preferencesMenu.Controls.Add(backgroundPatternButton);

            preferencesMenu.Deactivate += (s, args) => 
            {
                if (!isClosingPreferencesMenu)
                {
                    ClosePreferencesMenu();
                }
            };

            preferencesMenu.Show(this);
        }

        private void ShowBackgroundPatternDialog()
        {
            using (Form patternForm = new Form())
            {
                patternForm.Text = "Background Pattern Settings";
                patternForm.Size = new Size(300, 200);
                patternForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                patternForm.StartPosition = FormStartPosition.CenterParent;
                patternForm.MaximizeBox = false;
                patternForm.MinimizeBox = false;
                patternForm.Owner = this;

                // Pattern Size
                Label sizeLabel = new Label
                {
                    Text = "Pattern Size:",
                    Location = new Point(20, 20),
                    AutoSize = true
                };

                NumericUpDown sizeInput = new NumericUpDown
                {
                    Location = new Point(120, 18),
                    Minimum = 1,
                    Maximum = Math.Min(canvas.GridWidth, canvas.GridHeight),
                    Value = Math.Min(BackgroundSettings.PatternSize, Math.Min(canvas.GridWidth, canvas.GridHeight))
                };
                sizeInput.ValueChanged += (s, e) => 
                {
                    BackgroundSettings.PatternSize = (int)sizeInput.Value;
                    canvas.Invalidate();
                };

                // Color 1
                Label color1Label = new Label
                {
                    Text = "Color 1:",
                    Location = new Point(20, 60),
                    AutoSize = true
                };

                Panel color1Preview = new Panel
                {
                    Location = new Point(120, 58),
                    Size = new Size(40, 20),
                    BackColor = BackgroundSettings.Color1
                };

                Button color1Button = new Button
                {
                    Text = "Change",
                    Location = new Point(170, 56),
                    Size = new Size(70, 25)
                };

                // Color 2
                Label color2Label = new Label
                {
                    Text = "Color 2:",
                    Location = new Point(20, 100),
                    AutoSize = true
                };

                Panel color2Preview = new Panel
                {
                    Location = new Point(120, 98),
                    Size = new Size(40, 20),
                    BackColor = BackgroundSettings.Color2
                };

                Button color2Button = new Button
                {
                    Text = "Change",
                    Location = new Point(170, 96),
                    Size = new Size(70, 25)
                };

                // OK button
                Button okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(110, 140),
                    Size = new Size(70, 30)
                };

                Form? colorPickerForm = null;

                void ShowColorPicker(Color initialColor, Action<Color> onColorSelected, Point buttonLocation)
                {
                    // Close existing color picker if open
                    if (colorPickerForm != null && !colorPickerForm.IsDisposed)
                    {
                        colorPickerForm.Close();
                        return;
                    }

                    // Create new color picker form
                    colorPickerForm = new Form
                    {
                        Text = "Select Color",
                        StartPosition = FormStartPosition.Manual,
                        FormBorderStyle = FormBorderStyle.None,
                        ShowInTaskbar = false,
                        Size = new Size(220, 300),
                        Owner = patternForm
                    };

                    ColorSelector colorSelector = new ColorSelector
                    {
                        Dock = DockStyle.Fill,
                        CurrentColor = initialColor
                    };

                    colorSelector.ColorChanged += (s, color) =>
                    {
                        onColorSelected(color);
                    };

                    colorPickerForm.Controls.Add(colorSelector);

                    // Position the form next to the button
                    Point screenPoint = patternForm.PointToScreen(buttonLocation);
                    colorPickerForm.Location = new Point(screenPoint.X + 80, screenPoint.Y);

                    // Close color picker when focus is lost
                    colorPickerForm.Deactivate += (s, e) =>
                    {
                        if (colorPickerForm != null && !colorPickerForm.IsDisposed)
                        {
                            colorPickerForm.Close();
                            colorPickerForm = null;
                        }
                    };

                    colorPickerForm.Show();
                }

                color1Button.Click += (s, e) =>
                {
                    ShowColorPicker(
                        BackgroundSettings.Color1,
                        color =>
                        {
                            BackgroundSettings.Color1 = color;
                            color1Preview.BackColor = color;
                            canvas.Invalidate();
                        },
                        color1Button.Location
                    );
                };

                color2Button.Click += (s, e) =>
                {
                    ShowColorPicker(
                        BackgroundSettings.Color2,
                        color =>
                        {
                            BackgroundSettings.Color2 = color;
                            color2Preview.BackColor = color;
                            canvas.Invalidate();
                        },
                        color2Button.Location
                    );
                };

                patternForm.Controls.AddRange(new Control[] {
                    sizeLabel, sizeInput,
                    color1Label, color1Preview, color1Button,
                    color2Label, color2Preview, color2Button,
                    okButton
                });

                patternForm.FormClosing += (s, e) =>
                {
                    if (colorPickerForm != null && !colorPickerForm.IsDisposed)
                    {
                        colorPickerForm.Close();
                    }
                };

                patternForm.ShowDialog(this);
            }
        }
    }

    public enum Tool
    {
        Pen,
        Eraser,
        Bucket,
        ColorPicker,
        Marquee
    }
}
