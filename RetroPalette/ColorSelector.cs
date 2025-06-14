using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace RetroPalette
{
    /// <summary>
    /// A custom color selector control that provides HSV color picking functionality with alpha channel support.
    /// This control includes a saturation/value box, hue slider, alpha slider, and a customizable color palette.
    /// </summary>
    public class ColorSelector : Panel
    {
        // Current selected color and palette storage
        private Color currentColor = Color.Black;
        private List<Color> palette = new List<Color>();

        // Mouse interaction state tracking
        private bool isDraggingSV = false;
        private bool isDraggingHue = false;
        private bool isDraggingAlpha = false;

        // Control bounds for different components
        private Rectangle svBoxBounds;
        private Rectangle hueSliderBounds;
        private Rectangle alphaSliderBounds;
        private Rectangle paletteAreaBounds;
        private Rectangle loadPaletteButtonBounds;

        // Constants for UI layout
        private const int PALETTE_CELL_SIZE = 20;
        private const int PALETTE_COLS = 8;
        private const int SLIDER_HEIGHT = 20;
        private const int MARKER_SIZE = 10;
        private const int BUTTON_HEIGHT = 30;

        // HSV color components
        private float hue = 0f;        // 0-360 degrees
        private float saturation = 1f; // 0-1 range
        private float value = 1f;      // 0-1 range
        private int alpha = 255;       // 0-255 range

        // UI marker positions
        private Point svMarker;
        private int hueMarkerX;
        private int alphaMarkerX;

        /// <summary>
        /// Event triggered when the selected color changes
        /// </summary>
        [Browsable(false)]
        public event EventHandler<Color> ColorChanged;

        /// <summary>
        /// Gets or sets the currently selected color
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Gets or sets the current color")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CurrentColor
        {
            get => currentColor;
            set
            {
                if (currentColor != value)
                {
                    currentColor = value;
                    UpdateHSVFromColor(value);
                    ColorChanged?.Invoke(this, currentColor);
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the ColorSelector control
        /// </summary>
        public ColorSelector()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(200, 600);
            this.MinimumSize = new Size(200, 400);
            this.AutoScroll = true;

            // Initialize with pure red color (hue = 0, saturation = 1, value = 1, alpha = 255)
            hue = 0f;
            saturation = 1f;
            value = 1f;
            alpha = 255;
            currentColor = Color.Red;

            this.MouseDown += ColorSelector_MouseDown;
            this.MouseMove += ColorSelector_MouseMove;
            this.MouseUp += ColorSelector_MouseUp;

            // Force layout and marker positions to be calculated after control is created
            this.HandleCreated += (s, e) => 
            {
                this.OnPaint(new PaintEventArgs(this.CreateGraphics(), this.ClientRectangle));
                UpdateMarkerPositions();
                Invalidate();
            };
        }

        /// <summary>
        /// Handles the control's size change event and ensures minimum height requirements are met
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            int requiredHeight = CalculateRequiredHeight();
            if (Height < requiredHeight)
            {
                Height = requiredHeight;
            }
            Invalidate();
        }

        /// <summary>
        /// Calculates the minimum required height for the control based on its components
        /// </summary>
        /// <returns>The minimum height in pixels needed to display all components</returns>
        private int CalculateRequiredHeight()
        {
            int padding = 10;
            int fixedControlsHeight = 200 +
                                    SLIDER_HEIGHT + padding +
                                    SLIDER_HEIGHT + padding +
                                    40 + padding +
                                    BUTTON_HEIGHT + padding +
                                    BUTTON_HEIGHT + padding;

            int paletteRows = (palette.Count + PALETTE_COLS - 1) / PALETTE_COLS;
            int paletteHeight = paletteRows * PALETTE_CELL_SIZE + padding;

            return fixedControlsHeight + paletteHeight + padding;
        }

        /// <summary>
        /// Updates HSV and alpha values from a given Color object
        /// </summary>
        /// <param name="color">The color to extract HSV values from</param>
        private void UpdateHSVFromColor(Color color)
        {
            alpha = color.A;
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            // Calculate Value
            value = max;

            // Calculate Saturation
            saturation = max == 0 ? 0 : delta / max;

            // Calculate Hue
            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue = 60 * ((g - b) / delta % 6);
            }
            else if (max == g)
            {
                hue = 60 * ((b - r) / delta + 2);
            }
            else
            {
                hue = 60 * ((r - g) / delta + 4);
            }

            if (hue < 0) hue += 360;

            // Update markers
            UpdateMarkerPositions();
        }

        /// <summary>
        /// Updates the positions of all markers based on current HSV values
        /// </summary>
        private void UpdateMarkerPositions()
        {
            if (svBoxBounds.Width == 0 || svBoxBounds.Height == 0 || 
                hueSliderBounds.Width == 0 || alphaSliderBounds.Width == 0)
                return;

            // Update SV marker - ensure it's within the box bounds
            svMarker = new Point(
                (int)(svBoxBounds.Left + (saturation * (svBoxBounds.Width - 1))),
                (int)(svBoxBounds.Top + ((1 - value) * (svBoxBounds.Height - 1)))
            );

            // Update Hue marker - ensure it's within the slider bounds
            hueMarkerX = (int)(hueSliderBounds.Left + (hue / 360f * (hueSliderBounds.Width - 1)));

            // Update Alpha marker - ensure it's within the slider bounds
            alphaMarkerX = (int)(alphaSliderBounds.Left + (alpha / 255f * (alphaSliderBounds.Width - 1)));

            // Clamp all markers within their bounds
            svMarker = new Point(
                Math.Max(svBoxBounds.Left, Math.Min(svMarker.X, svBoxBounds.Right - 1)),
                Math.Max(svBoxBounds.Top, Math.Min(svMarker.Y, svBoxBounds.Bottom - 1))
            );
            
            hueMarkerX = Math.Max(hueSliderBounds.Left, Math.Min(hueMarkerX, hueSliderBounds.Right - 1));
            alphaMarkerX = Math.Max(alphaSliderBounds.Left, Math.Min(alphaMarkerX, alphaSliderBounds.Right - 1));
        }

        /// <summary>
        /// Handles the painting of the control and all its components
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padding = 10;
            int currentY = padding;

            // Draw Saturation/Value box
            svBoxBounds = new Rectangle(padding, currentY, Width - 2 * padding, 200);
            DrawSVBox(g, svBoxBounds);
            currentY = svBoxBounds.Bottom + padding;

            // Draw Hue slider
            hueSliderBounds = new Rectangle(padding, currentY, Width - 2 * padding, SLIDER_HEIGHT);
            DrawHueSlider(g, hueSliderBounds);
            currentY = hueSliderBounds.Bottom + padding;

            // Draw Alpha slider
            alphaSliderBounds = new Rectangle(padding, currentY, Width - 2 * padding, SLIDER_HEIGHT);
            DrawAlphaSlider(g, alphaSliderBounds);
            currentY = alphaSliderBounds.Bottom + padding;

            // Draw current color preview
            Rectangle previewBounds = new Rectangle(padding, currentY, Width - 2 * padding, 40);
            using (var brush = new SolidBrush(currentColor))
            {
                g.FillRectangle(brush, previewBounds);
            }
            g.DrawRectangle(Pens.Black, previewBounds);
            currentY = previewBounds.Bottom + padding;

            // Draw "Add to Palette" button
            Rectangle addButtonBounds = new Rectangle(padding, currentY, Width - 2 * padding, BUTTON_HEIGHT);
            using (var buttonBrush = new SolidBrush(SystemColors.Control))
            {
                g.FillRectangle(buttonBrush, addButtonBounds);
                g.DrawRectangle(Pens.Black, addButtonBounds);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("Add to Palette", Font, Brushes.Black, addButtonBounds, sf);
                }
            }
            currentY = addButtonBounds.Bottom + padding;

            // Draw "Load Palette" button
            loadPaletteButtonBounds = new Rectangle(padding, currentY, Width - 2 * padding, BUTTON_HEIGHT);
            using (var buttonBrush = new SolidBrush(SystemColors.Control))
            {
                g.FillRectangle(buttonBrush, loadPaletteButtonBounds);
                g.DrawRectangle(Pens.Black, loadPaletteButtonBounds);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("Load Palette", Font, Brushes.Black, loadPaletteButtonBounds, sf);
                }
            }
            currentY = loadPaletteButtonBounds.Bottom + padding;

            // Draw color palette
            paletteAreaBounds = new Rectangle(padding, currentY, Width - 2 * padding, Height - currentY - padding);
            int x = paletteAreaBounds.Left;
            int y = currentY;

            // Draw palette grid
            for (int i = 0; i < palette.Count; i++)
            {
                if (i > 0 && i % PALETTE_COLS == 0)
                {
                    y += PALETTE_CELL_SIZE;
                    x = paletteAreaBounds.Left;
                }

                Rectangle cellRect = new Rectangle(x, y, PALETTE_CELL_SIZE, PALETTE_CELL_SIZE);
                using (var brush = new SolidBrush(palette[i]))
                {
                    g.FillRectangle(brush, cellRect);
                }
                g.DrawRectangle(Pens.Black, cellRect);

                // Draw marker if this color matches the current color
                if (ColorsMatch(palette[i], currentColor))
                {
                    // Draw a white circle with black outline
                    int markerSize = 8;
                    int markerX = cellRect.Right - markerSize - 2;
                    int markerY = cellRect.Top + 2;
                    g.FillEllipse(Brushes.White, markerX, markerY, markerSize, markerSize);
                    g.DrawEllipse(Pens.Black, markerX, markerY, markerSize, markerSize);
                }

                x += PALETTE_CELL_SIZE;
            }

            // Draw markers
            DrawMarker(g, svMarker);
            DrawHorizontalMarker(g, hueMarkerX, hueSliderBounds.Top);
            DrawHorizontalMarker(g, alphaMarkerX, alphaSliderBounds.Top);
        }

        /// <summary>
        /// Compares two colors for equality, ignoring alpha channel
        /// </summary>
        private bool ColorsMatch(Color c1, Color c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B && c1.A == c2.A;
        }

        /// <summary>
        /// Draws the saturation/value color box
        /// </summary>
        private void DrawSVBox(Graphics g, Rectangle bounds)
        {
            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics gBmp = Graphics.FromImage(bmp))
                {
                    for (int y = 0; y < bounds.Height; y++)
                    {
                        for (int x = 0; x < bounds.Width; x++)
                        {
                            float s = x / (float)bounds.Width;
                            float v = 1 - (y / (float)bounds.Height);
                            Color color = HSVToRGB(hue, s, v, alpha);
                            using (var brush = new SolidBrush(color))
                            {
                                gBmp.FillRectangle(brush, x, y, 1, 1);
                            }
                        }
                    }
                }
                g.DrawImage(bmp, bounds);
            }
            g.DrawRectangle(Pens.Black, bounds);
        }

        /// <summary>
        /// Draws the hue slider with color gradient
        /// </summary>
        private void DrawHueSlider(Graphics g, Rectangle bounds)
        {
            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics gBmp = Graphics.FromImage(bmp))
                {
                    for (int x = 0; x < bounds.Width; x++)
                    {
                        float h = (x / (float)bounds.Width) * 360f;
                        Color color = HSVToRGB(h, 1, 1, 255);
                        using (var brush = new SolidBrush(color))
                        {
                            gBmp.FillRectangle(brush, x, 0, 1, bounds.Height);
                        }
                    }
                }
                g.DrawImage(bmp, bounds);
            }
            g.DrawRectangle(Pens.Black, bounds);
        }

        /// <summary>
        /// Draws the alpha slider with transparency gradient
        /// </summary>
        private void DrawAlphaSlider(Graphics g, Rectangle bounds)
        {
            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics gBmp = Graphics.FromImage(bmp))
                {
                    for (int x = 0; x < bounds.Width; x++)
                    {
                        int a = (int)((x / (float)bounds.Width) * 255);
                        Color color = HSVToRGB(hue, saturation, value, a);
                        using (var brush = new SolidBrush(color))
                        {
                            gBmp.FillRectangle(brush, x, 0, 1, bounds.Height);
                        }
                    }
                }
                g.DrawImage(bmp, bounds);
            }
            g.DrawRectangle(Pens.Black, bounds);
        }

        /// <summary>
        /// Draws a circular marker at the specified center point
        /// </summary>
        private void DrawMarker(Graphics g, Point center)
        {
            g.DrawEllipse(Pens.White, center.X - MARKER_SIZE/2, center.Y - MARKER_SIZE/2, MARKER_SIZE, MARKER_SIZE);
            g.DrawEllipse(Pens.Black, center.X - MARKER_SIZE/2 - 1, center.Y - MARKER_SIZE/2 - 1, MARKER_SIZE + 2, MARKER_SIZE + 2);
        }

        /// <summary>
        /// Draws a horizontal marker for sliders
        /// </summary>
        private void DrawHorizontalMarker(Graphics g, int x, int y)
        {
            Point[] points = new Point[]
            {
                new Point(x, y),
                new Point(x - 5, y - 5),
                new Point(x + 5, y - 5)
            };
            g.FillPolygon(Brushes.White, points);
            g.DrawPolygon(Pens.Black, points);
        }

        /// <summary>
        /// Handles mouse down events for color selection and interaction
        /// </summary>
        private void ColorSelector_MouseDown(object sender, MouseEventArgs e)
        {
            if (svBoxBounds.Contains(e.Location))
            {
                isDraggingSV = true;
                UpdateSVFromMouse(e.Location);
            }
            else if (hueSliderBounds.Contains(e.Location))
            {
                isDraggingHue = true;
                UpdateHueFromMouse(e.Location);
            }
            else if (alphaSliderBounds.Contains(e.Location))
            {
                isDraggingAlpha = true;
                UpdateAlphaFromMouse(e.Location);
            }
            else if (loadPaletteButtonBounds.Contains(e.Location))
            {
                LoadPaletteFromFile();
            }
            else if (e.Y >= paletteAreaBounds.Top - 70 && e.Y <= paletteAreaBounds.Top - 40)
            {
                // Add to palette button clicked
                if (!palette.Contains(currentColor))
                {
                    palette.Add(currentColor);
                    Invalidate();
                }
            }
            else if (e.Y >= paletteAreaBounds.Top)
            {
                // Check if clicked on palette color
                int row = (e.Y - paletteAreaBounds.Top) / PALETTE_CELL_SIZE;
                int col = (e.X - paletteAreaBounds.Left) / PALETTE_CELL_SIZE;
                int index = row * PALETTE_COLS + col;

                if (index >= 0 && index < palette.Count)
                {
                    CurrentColor = palette[index];
                }
            }
        }

        /// <summary>
        /// Handles mouse movement for dragging operations
        /// </summary>
        private void ColorSelector_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingSV && svBoxBounds.Contains(e.Location))
            {
                UpdateSVFromMouse(e.Location);
            }
            else if (isDraggingHue && e.X >= hueSliderBounds.Left && e.X <= hueSliderBounds.Right)
            {
                UpdateHueFromMouse(e.Location);
            }
            else if (isDraggingAlpha && e.X >= alphaSliderBounds.Left && e.X <= alphaSliderBounds.Right)
            {
                UpdateAlphaFromMouse(e.Location);
            }
        }

        /// <summary>
        /// Handles mouse up events to end dragging operations
        /// </summary>
        private void ColorSelector_MouseUp(object sender, MouseEventArgs e)
        {
            isDraggingSV = false;
            isDraggingHue = false;
            isDraggingAlpha = false;
        }

        /// <summary>
        /// Updates saturation and value based on mouse position in the SV box
        /// </summary>
        private void UpdateSVFromMouse(Point location)
        {
            saturation = Math.Clamp((location.X - svBoxBounds.Left) / (float)svBoxBounds.Width, 0, 1);
            value = Math.Clamp(1 - (location.Y - svBoxBounds.Top) / (float)svBoxBounds.Height, 0, 1);
            svMarker = location;
            UpdateCurrentColor();
        }

        /// <summary>
        /// Updates hue value based on mouse position in the hue slider
        /// </summary>
        private void UpdateHueFromMouse(Point location)
        {
            hue = Math.Clamp((location.X - hueSliderBounds.Left) / (float)hueSliderBounds.Width * 360f, 0, 360);
            hueMarkerX = location.X;
            UpdateCurrentColor();
        }

        /// <summary>
        /// Updates alpha value based on mouse position in the alpha slider
        /// </summary>
        private void UpdateAlphaFromMouse(Point location)
        {
            alpha = (int)Math.Clamp((location.X - alphaSliderBounds.Left) / (float)alphaSliderBounds.Width * 255f, 0, 255);
            alphaMarkerX = location.X;
            UpdateCurrentColor();
        }

        /// <summary>
        /// Updates the current color based on HSV and alpha values
        /// </summary>
        private void UpdateCurrentColor()
        {
            currentColor = HSVToRGB(hue, saturation, value, alpha);
            ColorChanged?.Invoke(this, currentColor);
            Invalidate();
        }

        /// <summary>
        /// Converts HSV color values to RGB Color object
        /// </summary>
        private static Color HSVToRGB(float hue, float saturation, float value, int alpha)
        {
            int hi = (int)Math.Floor(hue / 60) % 6;
            float f = hue / 60 - (float)Math.Floor(hue / 60);

            value = value * 255;
            int v = (int)value;
            int p = (int)(value * (1 - saturation));
            int q = (int)(value * (1 - f * saturation));
            int t = (int)(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(alpha, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(alpha, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(alpha, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(alpha, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(alpha, t, p, v);
            else
                return Color.FromArgb(alpha, v, p, q);
        }

        /// <summary>
        /// Loads a color palette from a file
        /// </summary>
        private void LoadPaletteFromFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Path.Combine(Application.StartupPath, "palettes");
                openFileDialog.Filter = "Aseprite Files (*.aseprite)|*.aseprite|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<Color> newPalette = ReadPaletteFromAseprite(openFileDialog.FileName);
                        if (newPalette.Count > 0)
                        {
                            palette = newPalette;
                            Height = CalculateRequiredHeight();
                            Invalidate();
                            MessageBox.Show($"Successfully loaded {newPalette.Count} colors from palette.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("No palette found in the selected file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading palette: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Reads color palette from an Aseprite file
        /// </summary>
        private List<Color> ReadPaletteFromAseprite(string filePath)
        {
            List<Color> colors = new List<Color>();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Read header
                uint fileSize = reader.ReadUInt32();
                ushort magic = reader.ReadUInt16();
                ushort frames = reader.ReadUInt16();

                if (magic != 0xA5E0)
                    throw new Exception("Invalid Aseprite file format");

                // Skip remaining header bytes (total header is 128 bytes)
                reader.BaseStream.Position = 128;

                // Process each frame
                for (int frame = 0; frame < frames; frame++)
                {
                    uint bytesInFrame = reader.ReadUInt32();
                    ushort frameMagic = reader.ReadUInt16();
                    ushort oldChunks = reader.ReadUInt16();
                    ushort frameDuration = reader.ReadUInt16();
                    reader.BaseStream.Position += 2; // Skip reserved bytes
                    uint newChunks = reader.ReadUInt32();

                    if (frameMagic != 0xF1FA)
                        throw new Exception("Invalid frame format");

                    uint numChunks = newChunks != 0 ? newChunks : oldChunks;

                    // Process chunks in frame
                    for (uint chunk = 0; chunk < numChunks; chunk++)
                    {
                        uint chunkSize = reader.ReadUInt32();
                        ushort chunkType = reader.ReadUInt16();

                        if (chunkType == 0x2019) // Palette chunk
                        {
                            uint newPaletteSize = reader.ReadUInt32();
                            uint firstColorIndex = reader.ReadUInt32();
                            uint lastColorIndex = reader.ReadUInt32();
                            reader.BaseStream.Position += 8; // Skip reserved bytes

                            uint numEntries = lastColorIndex - firstColorIndex + 1;
                            for (uint i = 0; i < numEntries; i++)
                            {
                                ushort flags = reader.ReadUInt16();
                                byte r = reader.ReadByte();
                                byte g = reader.ReadByte();
                                byte b = reader.ReadByte();
                                byte a = reader.ReadByte();

                                colors.Add(Color.FromArgb(a, r, g, b));

                                if ((flags & 1) != 0)
                                {
                                    // Skip name if present
                                    ushort nameLength = reader.ReadUInt16();
                                    reader.BaseStream.Position += nameLength;
                                }
                            }
                        }
                        else
                        {
                            // Skip other chunk types
                            reader.BaseStream.Position += chunkSize - 6;
                        }
                    }
                }
            }
            return colors;
        }
    }
} 