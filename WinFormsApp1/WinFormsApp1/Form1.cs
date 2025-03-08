using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Text;
using System.ComponentModel;

namespace WinFormsApp1
{
    // Custom rounded button class
    public class RoundedButton : Button
    {
        private int borderRadius = 10;
        private Color borderColor = Color.Silver;
        private Color hoverBackColor;
        private Color hoverForeColor;
        private Color pressedBackColor;
        private bool isHovering = false;
        private bool isPressed = false;
        private Color defaultBackColor;

        [Category("Appearance")]
        public int BorderRadius
        {
            get { return borderRadius; }
            set 
            { 
                borderRadius = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        public Color BorderColor
        {
            get { return borderColor; }
            set 
            { 
                borderColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        public Color HoverBackColor
        {
            get { return hoverBackColor; }
            set { hoverBackColor = value; }
        }

        [Category("Appearance")]
        public Color HoverForeColor
        {
            get { return hoverForeColor; }
            set { hoverForeColor = value; }
        }

        [Category("Appearance")]
        public Color PressedBackColor
        {
            get { return pressedBackColor; }
            set { pressedBackColor = value; }
        }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(150, 40);
            BackColor = Color.FromArgb(60, 141, 188);
            defaultBackColor = BackColor; // Store the default back color
            ForeColor = Color.White;
            hoverBackColor = Color.FromArgb(45, 125, 154);
            hoverForeColor = Color.White;
            pressedBackColor = Color.FromArgb(25, 105, 134);
            Font = new Font("Segoe UI", 10F);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Rectangle rectSurface = this.ClientRectangle;
            Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);
            int smoothSize = 2;
            
            if (borderRadius > 2) // Rounded button
            {
                using (GraphicsPath pathSurface = GetFigurePath(rectSurface, borderRadius))
                using (GraphicsPath pathBorder = GetFigurePath(rectBorder, borderRadius - 1))
                using (Pen penSurface = new Pen(this.Parent.BackColor, smoothSize))
                using (Pen penBorder = new Pen(borderColor, 1))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    
                    // Button surface
                    this.Region = new Region(pathSurface);
                    
                    // Draw surface border for HD result
                    e.Graphics.DrawPath(penSurface, pathSurface);
                    
                    // Button border
                    if (borderColor != Color.Transparent)
                        e.Graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else // Normal button
            {
                e.Graphics.SmoothingMode = SmoothingMode.None;
                
                // Button surface
                this.Region = new Region(rectSurface);
                
                // Button border
                if (borderColor != Color.Transparent)
                    using (Pen penBorder = new Pen(borderColor, 1))
                        e.Graphics.DrawRectangle(penBorder, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;
            
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovering = true;
            if (HoverBackColor != Color.Empty)
                BackColor = HoverBackColor;
            if (HoverForeColor != Color.Empty)
                ForeColor = HoverForeColor;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovering = false;
            isPressed = false;
            
            // Reset to original colors
            BackColor = defaultBackColor;
            ForeColor = DefaultForeColor;
            
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            isPressed = true;
            if (PressedBackColor != Color.Empty)
                BackColor = PressedBackColor;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPressed = false;
            if (isHovering)
            {
                if (HoverBackColor != Color.Empty)
                    BackColor = HoverBackColor;
            }
            else
            {
                BackColor = FlatAppearance.MouseOverBackColor;
            }
            Invalidate();
        }
    }

    public partial class Form1 : Form
    {
        private List<FilterCondition> filterConditions = new List<FilterCondition>();
        private List<string> originalLines = new List<string>();
        private Form filterForm;
        private LineNumberRichTextBox mainTextBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private string lastOpenedFilePath = string.Empty;

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        private Icon GetFolderIcon()
        {
            IntPtr hIcon = ExtractIcon(IntPtr.Zero, "shell32.dll", 4); // Index 4 is typically a folder icon
            if (hIcon != IntPtr.Zero)
            {
                return Icon.FromHandle(hIcon);
            }
            return SystemIcons.Application; // Fallback icon
        }

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            
            // Try to load default filters if available
            LoadDefaultFilters();
        }

        private void InitializeCustomComponents()
        {
            // Create main menu
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem openFileMenuItem = new ToolStripMenuItem("Open File", null, OpenFile_Click);
            fileMenu.DropDownItems.Add(openFileMenuItem);

            // Add Save Content menu item
            ToolStripMenuItem saveContentMenuItem = new ToolStripMenuItem("Save Content", null, SaveContent_Click);
            fileMenu.DropDownItems.Add(saveContentMenuItem);

            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
            ToolStripMenuItem showFiltersMenuItem = new ToolStripMenuItem("Show Filters", null, ShowFilters_Click);
            ToolStripMenuItem showSidebarMenuItem = new ToolStripMenuItem("Show Line Numbers");
            showSidebarMenuItem.Checked = false;
            showSidebarMenuItem.Click += ShowSidebar_Click;
            
            viewMenu.DropDownItems.Add(showFiltersMenuItem);
            viewMenu.DropDownItems.Add(showSidebarMenuItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Create status strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);

            // Container panel for layout - make sure it's positioned correctly
            Panel containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, menuStrip.Height, 0, statusStrip.Height) // Add padding to avoid overlap
            };
            this.Controls.Add(containerPanel);

            // Create main text box
            mainTextBox = new LineNumberRichTextBox
            {
                Dock = DockStyle.Fill,
                ShowLineNumbers = false,
                BorderStyle = BorderStyle.None
            };

            containerPanel.Controls.Add(mainTextBox);

            // Set form properties
            this.Text = "File Viewer";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = GetFolderIcon();

            // Create filter form
            CreateFilterForm();
            
            // Make sure controls are in the correct z-order
            menuStrip.BringToFront();
            statusStrip.BringToFront();
        }

        // Helper method to create a styled button
        private RoundedButton CreateStyledButton(string text, int width, int height, Point location, Color backColor)
        {
            return new RoundedButton
            {
                Text = text,
                Width = width,
                Height = height,
                Location = location,
                BackColor = backColor,
                ForeColor = Color.White,
                BorderRadius = 15,
                Font = new Font("Segoe UI", 10)
            };
        }

        private void CreateFilterForm()
        {
            filterForm = new Form
            {
                Text = "Filter Options",
                Width = 560,
                Height = 600,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // Top panel for buttons
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Create styled buttons
            RoundedButton addFilterButton = CreateStyledButton(
                "Add Filter", 
                120, 
                35, 
                new Point(10, 12), 
                Color.FromArgb(40, 167, 69)
            );
            addFilterButton.Click += AddFilter_Click;

            RoundedButton applyButton = CreateStyledButton(
                "Apply Filters", 
                120, 
                35, 
                new Point(140, 12), 
                Color.FromArgb(0, 123, 255)
            );
            applyButton.Click += ApplyFilters_Click;

            RoundedButton loadButton = CreateStyledButton(
                "Load Filters", 
                120, 
                35, 
                new Point(270, 12), 
                Color.FromArgb(108, 117, 125)
            );
            loadButton.Click += LoadFilters_Click;

            RoundedButton saveButton = CreateStyledButton(
                "Save Filters", 
                120, 
                35, 
                new Point(400, 12), 
                Color.FromArgb(255, 193, 7)
            );
            saveButton.Click += SaveFilters_Click;

            topPanel.Controls.AddRange(new Control[] { addFilterButton, applyButton, loadButton, saveButton });

            // Add a container panel for the filters with a border
            Panel filterContainerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            
            // Panel for filter conditions
            Panel filterConditionsPanel = new Panel
            {
                Name = "filterConditionsPanel",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            
            // Add an explanatory label at the top of the conditions panel
            Label explanationLabel = new Label
            {
                Text = "Add filters to show only lines that match the criteria. Each filter is applied with OR logic.",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5)
            };
            
            filterConditionsPanel.Controls.Add(explanationLabel);
            filterContainerPanel.Controls.Add(filterConditionsPanel);

            // Add panels to form
            filterForm.Controls.Add(filterContainerPanel);
            filterForm.Controls.Add(topPanel);
        }

        private void ShowFilters_Click(object sender, EventArgs e)
        {
            try
            {
                if (filterForm == null || filterForm.IsDisposed)
                {
                    CreateFilterForm();
                }

                if (!filterForm.Visible)
                {
                    // Position the filter form to the right of the main form with proper spacing
                    // and ensure it doesn't overlap with the main form
                    int filterFormX = this.Location.X + this.Width + 10; // Add 10px spacing
                    int filterFormY = this.Location.Y;
                    
                    // Make sure the filter form is visible on screen
                    Rectangle screenBounds = Screen.FromControl(this).Bounds;
                    if (filterFormX + filterForm.Width > screenBounds.Right)
                    {
                        // If it would go off screen, position it to the left of the main form instead
                        filterFormX = this.Location.X - filterForm.Width - 10;
                        
                        // If it would still be off screen, just position it at the right edge of the screen
                        if (filterFormX < screenBounds.Left)
                        {
                            filterFormX = screenBounds.Right - filterForm.Width;
                            filterFormY = this.Location.Y + 50; // Offset it vertically a bit
                        }
                    }
                    
                    filterForm.Location = new Point(filterFormX, filterFormY);
                    filterForm.Show(this);
                }
                else
                {
                    filterForm.Hide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing filters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Attempt to recreate the form if there was an error
                CreateFilterForm();
            }
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Log files (*.log)|*.log";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Store the file path
                        lastOpenedFilePath = openFileDialog.FileName;
                        
                        // Read all lines and store them
                        originalLines = File.ReadAllLines(openFileDialog.FileName).ToList();
                        
                        if (originalLines.Count == 0)
                        {
                            MessageBox.Show("The file is empty.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // For very large files, show a warning
                        if (originalLines.Count > 100000)
                        {
                            if (MessageBox.Show(
                                "This is a very large file. Loading it might take some time and memory. Continue?",
                                "Large File Warning",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning) != DialogResult.Yes)
                            {
                                return;
                            }
                        }

                        mainTextBox.SuspendLayout();
                        mainTextBox.Clear();
                        
                        // Load text in chunks for better performance
                        const int chunkSize = 5000;
                        for (int i = 0; i < originalLines.Count; i += chunkSize)
                        {
                            int count = Math.Min(chunkSize, originalLines.Count - i);
                            string chunk = string.Join(Environment.NewLine, originalLines.GetRange(i, count));
                            mainTextBox.AppendText(chunk);
                            if (i + count < originalLines.Count)
                            {
                                mainTextBox.AppendText(Environment.NewLine);
                            }
                            Application.DoEvents();
                        }

                        mainTextBox.Select(0, 0);
                        mainTextBox.ResumeLayout();

                        // Update status bar instead of showing message box
                        statusLabel.Text = $"Total lines: {originalLines.Count:N0} | Displayed: {originalLines.Count:N0} (100%)";

                        // Optional: Free up memory if it's a very large file
                        if (originalLines.Count > 100000)
                        {
                            GC.Collect();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddFilter_Click(object sender, EventArgs e)
        {
            Panel filterConditionsPanel = (Panel)filterForm.Controls.Find("filterConditionsPanel", true)[0];
            
            // Create filter container with simplified styling
            Panel filterContainer = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(5),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add enabled checkbox
            CheckBox enabledCheckBox = new CheckBox
            {
                Text = "",
                Checked = true,
                Location = new Point(10, 15),
                Width = 20,
                Height = 20
            };
            enabledCheckBox.CheckedChanged += (s, ev) => ApplyFilters_Click(null, null); // Reapply filters when checkbox changes

            ComboBox filterTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Height = 30,
                Location = new Point(40, 10), // Moved to make room for checkbox
                Font = new Font("Segoe UI", 10)
            };
            filterTypeCombo.Items.AddRange(new string[] { "CONTAINS", "EQUALS" });
            filterTypeCombo.SelectedIndex = 0;

            TextBox filterTextBox = new TextBox
            {
                Width = 220,
                Height = 30,
                Location = new Point(150, 10), // Adjusted position
                Font = new Font("Segoe UI", 10)
            };

            // Create a rounded color button
            RoundedButton colorButton = new RoundedButton
            {
                Width = 40,
                Height = 30,
                Location = new Point(380, 10), // Adjusted position
                BackColor = Color.White,
                BorderRadius = 10,
                Text = ""
            };

            colorButton.Click += (s, ev) =>
            {
                using (ColorDialog colorDialog = new ColorDialog())
                {
                    colorDialog.Color = colorButton.BackColor;
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        colorButton.BackColor = colorDialog.Color;
                        ApplyFilters_Click(null, null); // Reapply filters to update colors
                    }
                }
            };

            // Create a rounded remove button
            RoundedButton removeButton = new RoundedButton
            {
                Text = "X",
                Width = 40,
                Height = 30,
                Location = new Point(430, 10), // Adjusted position
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                BorderRadius = 10,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            removeButton.Click += (s, ev) =>
            {
                filterConditionsPanel.Controls.Remove(filterContainer);
                filterConditions.RemoveAll(f => f.Container == filterContainer);
                ApplyFilters_Click(null, null); // Reapply filters after removing one
            };

            filterContainer.Controls.AddRange(new Control[] { enabledCheckBox, filterTypeCombo, filterTextBox, colorButton, removeButton });
            
            // Add new filter condition
            FilterCondition condition = new FilterCondition
            {
                Container = filterContainer,
                TypeComboBox = filterTypeCombo,
                TextBox = filterTextBox,
                ColorButton = colorButton,
                EnabledCheckBox = enabledCheckBox,
                HighlightColor = Color.White
            };
            filterConditions.Add(condition);

            // Add to panel below the buttons
            filterConditionsPanel.Controls.Add(filterContainer);
            filterContainer.BringToFront();
        }

        private void ApplyFilters_Click(object sender, EventArgs e)
        {
            if (originalLines.Count == 0)
            {
                MessageBox.Show("Please open a file first.", "No File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show processing indicator in status bar
            statusLabel.Text = "Processing filters...";
            Application.DoEvents(); // Allow UI to update

            mainTextBox.SuspendLayout();
            mainTextBox.Clear();

            // Get enabled filters
            var enabledFilterConditions = filterConditions.Where(c => c.Enabled).ToList();

            // If no filters are active or enabled, show all lines
            if (enabledFilterConditions.Count == 0 || enabledFilterConditions.All(c => string.IsNullOrEmpty(c.TextBox.Text)))
            {
                // For large files, use a StringBuilder and append in chunks
                if (originalLines.Count > 10000)
                {
                    const int chunkSize = 5000;
                    for (int i = 0; i < originalLines.Count; i += chunkSize)
                    {
                        int count = Math.Min(chunkSize, originalLines.Count - i);
                        string chunk = string.Join(Environment.NewLine, originalLines.GetRange(i, count));
                        mainTextBox.AppendText(chunk);
                        if (i + count < originalLines.Count)
                        {
                            mainTextBox.AppendText(Environment.NewLine);
                        }
                        // Allow UI to update periodically for responsiveness
                        if (i % 20000 == 0)
                        {
                            Application.DoEvents();
                        }
                    }
                }
                else
                {
                    mainTextBox.Text = string.Join(Environment.NewLine, originalLines);
                }
                
                mainTextBox.Select(0, 0);
                mainTextBox.ResumeLayout();
                
                // Update status bar to show all lines are displayed
                statusLabel.Text = $"Total lines: {originalLines.Count:N0} | Displayed: {originalLines.Count:N0} (100%)";
                return;
            }

            // Prepare filter conditions for faster matching - only use enabled filters
            var activeFilters = enabledFilterConditions
                .Where(c => !string.IsNullOrEmpty(c.TextBox.Text))
                .Select(c => new {
                    Text = c.TextBox.Text,
                    IsContains = c.TypeComboBox.Text == "CONTAINS",
                    Color = c.ColorButton.BackColor
                })
                .ToList();

            // Pre-allocate collection with estimated capacity
            var matchingLines = new List<(string Line, Color Color)>(originalLines.Count / 4);
            
            // Process in chunks for better responsiveness
            const int processChunkSize = 10000;
            for (int chunkStart = 0; chunkStart < originalLines.Count; chunkStart += processChunkSize)
            {
                int chunkEnd = Math.Min(chunkStart + processChunkSize, originalLines.Count);
                
                // Process this chunk
                for (int i = chunkStart; i < chunkEnd; i++)
                {
                    string line = originalLines[i];
                    
                    // Find first matching filter
                    foreach (var filter in activeFilters)
                    {
                        bool isMatch = filter.IsContains
                            ? line.IndexOf(filter.Text, StringComparison.OrdinalIgnoreCase) >= 0 // Faster than Contains with StringComparison
                            : line.Equals(filter.Text, StringComparison.OrdinalIgnoreCase);
                        
                        if (isMatch)
                        {
                            matchingLines.Add((line, filter.Color));
                            break; // Stop checking other filters once we find a match
                        }
                    }
                }
                
                // Allow UI to update periodically
                if (chunkStart % (processChunkSize * 2) == 0)
                {
                    statusLabel.Text = $"Processing... {Math.Min(chunkEnd, originalLines.Count):N0}/{originalLines.Count:N0} lines";
                    Application.DoEvents();
                }
            }

            // Now display only the matching lines with their colors
            if (matchingLines.Count > 0)
            {
                // For large result sets, use batched processing
                if (matchingLines.Count > 5000)
                {
                    // First, build the text content
                    StringBuilder sb = new StringBuilder(matchingLines.Count * 100); // Estimate average line length
                    for (int i = 0; i < matchingLines.Count; i++)
                    {
                        sb.AppendLine(matchingLines[i].Line);
                    }
                    
                    // Set the text all at once (much faster)
                    mainTextBox.Text = sb.ToString();
                    
                    // Now apply colors in batches
                    mainTextBox.SuspendLayout();
                    int currentPos = 0;
                    for (int i = 0; i < matchingLines.Count; i++)
                    {
                        var (line, color) = matchingLines[i];
                        int lineLength = line.Length;
                        
                        mainTextBox.Select(currentPos, lineLength);
                        mainTextBox.SelectionBackColor = color;
                        
                        // Skip past this line and the newline character(s)
                        currentPos += lineLength + Environment.NewLine.Length;
                        
                        // Update UI periodically
                        if (i % 1000 == 0 && i > 0)
                        {
                            statusLabel.Text = $"Applying colors... {i:N0}/{matchingLines.Count:N0}";
                            Application.DoEvents();
                        }
                    }
                }
                else
                {
                    // For smaller result sets, use the original approach
                    for (int i = 0; i < matchingLines.Count; i++)
                    {
                        var (line, color) = matchingLines[i];
                        int startIndex = mainTextBox.TextLength;
                        
                        mainTextBox.AppendText(line);
                        if (i < matchingLines.Count - 1)
                            mainTextBox.AppendText(Environment.NewLine);
                            
                        mainTextBox.Select(startIndex, line.Length);
                        mainTextBox.SelectionBackColor = color;
                    }
                }
                
                // Calculate percentage of lines displayed
                double percentage = (double)matchingLines.Count / originalLines.Count * 100;
                statusLabel.Text = $"Total lines: {originalLines.Count:N0} | Displayed: {matchingLines.Count:N0} ({percentage:F1}%)";
            }
            else
            {
                mainTextBox.AppendText("No matches found.");
                statusLabel.Text = $"Total lines: {originalLines.Count:N0} | Displayed: 0 (0%)";
            }

            mainTextBox.Select(0, 0);
            mainTextBox.ResumeLayout();
        }

        private void SaveFilters_Click(object sender, EventArgs e)
        {
            if (filterConditions.Count == 0)
            {
                MessageBox.Show("No filters to save.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.DefaultExt = "json";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var filterData = filterConditions.Select(c => new FilterData
                        {
                            FilterType = c.TypeComboBox.Text,
                            FilterText = c.TextBox.Text,
                            HighlightColor = ColorTranslator.ToHtml(c.ColorButton.BackColor),
                            Enabled = c.EnabledCheckBox.Checked
                        }).ToList();

                        string jsonString = JsonSerializer.Serialize(filterData, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                        File.WriteAllText(saveFileDialog.FileName, jsonString);
                        MessageBox.Show($"Filters saved to {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving filters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadFilters_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string jsonString = File.ReadAllText(openFileDialog.FileName);
                        var filterData = JsonSerializer.Deserialize<List<FilterData>>(jsonString);

                        // Clear existing filters
                        Panel filterConditionsPanel = (Panel)filterForm.Controls.Find("filterConditionsPanel", true)[0];
                        filterConditionsPanel.Controls.Clear();
                        filterConditions.Clear();

                        // Add label back
                        Label explanationLabel = new Label
                        {
                            Text = "Add filters to show only lines that match the criteria. Each filter is applied with OR logic.",
                            Dock = DockStyle.Top,
                            Height = 40,
                            Font = new Font("Segoe UI", 9),
                            ForeColor = Color.DimGray,
                            TextAlign = ContentAlignment.MiddleLeft,
                            Padding = new Padding(5)
                        };
                        filterConditionsPanel.Controls.Add(explanationLabel);

                        // Add loaded filters
                        foreach (var filter in filterData)
                        {
                            AddFilterFromData(filter);
                        }

                        // Apply loaded filters
                        ApplyFilters_Click(null, null);
                        
                        // No confirmation message
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading filters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddFilterFromData(FilterData filterData)
        {
            Panel filterConditionsPanel = (Panel)filterForm.Controls.Find("filterConditionsPanel", true)[0];
            
            // Create filter container with simplified styling
            Panel filterContainer = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(5),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add enabled checkbox
            CheckBox enabledCheckBox = new CheckBox
            {
                Text = "",
                Checked = filterData.Enabled, // Set from loaded data
                Location = new Point(10, 15),
                Width = 20,
                Height = 20
            };
            enabledCheckBox.CheckedChanged += (s, ev) => ApplyFilters_Click(null, null); // Reapply filters when checkbox changes

            ComboBox filterTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Height = 30,
                Location = new Point(40, 10), // Moved to make room for checkbox
                Font = new Font("Segoe UI", 10)
            };
            filterTypeCombo.Items.AddRange(new string[] { "CONTAINS", "EQUALS" });
            filterTypeCombo.Text = filterData.FilterType;

            TextBox filterTextBox = new TextBox
            {
                Width = 220,
                Height = 30,
                Location = new Point(150, 10), // Adjusted position
                Text = filterData.FilterText,
                Font = new Font("Segoe UI", 10)
            };

            // Create a rounded color button
            RoundedButton colorButton = new RoundedButton
            {
                Width = 40,
                Height = 30,
                Location = new Point(380, 10), // Adjusted position
                BackColor = ColorTranslator.FromHtml(filterData.HighlightColor),
                BorderRadius = 10,
                Text = ""
            };

            colorButton.Click += (s, ev) =>
            {
                using (ColorDialog colorDialog = new ColorDialog())
                {
                    colorDialog.Color = colorButton.BackColor;
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        colorButton.BackColor = colorDialog.Color;
                        ApplyFilters_Click(null, null);
                    }
                }
            };

            // Create a rounded remove button
            RoundedButton removeButton = new RoundedButton
            {
                Text = "X",
                Width = 40,
                Height = 30,
                Location = new Point(430, 10), // Adjusted position
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                BorderRadius = 10,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            removeButton.Click += (s, ev) =>
            {
                filterConditionsPanel.Controls.Remove(filterContainer);
                filterConditions.RemoveAll(f => f.Container == filterContainer);
                ApplyFilters_Click(null, null);
            };

            filterContainer.Controls.AddRange(new Control[] { enabledCheckBox, filterTypeCombo, filterTextBox, colorButton, removeButton });
            
            // Add new filter condition
            FilterCondition condition = new FilterCondition
            {
                Container = filterContainer,
                TypeComboBox = filterTypeCombo,
                TextBox = filterTextBox,
                ColorButton = colorButton,
                EnabledCheckBox = enabledCheckBox,
                HighlightColor = ColorTranslator.FromHtml(filterData.HighlightColor)
            };
            filterConditions.Add(condition);

            // Add to panel below the buttons
            filterConditionsPanel.Controls.Add(filterContainer);
            filterContainer.BringToFront();
        }

        private void ShowSidebar_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem != null)
            {
                menuItem.Checked = !menuItem.Checked;
                mainTextBox.ShowLineNumbers = menuItem.Checked;
                mainTextBox.Invalidate();
            }
        }

        private void LoadDefaultFilters()
        {
            string defaultFiltersPath = Path.Combine(Application.StartupPath, "default.json");
            
            if (File.Exists(defaultFiltersPath))
            {
                try
                {
                    string jsonString = File.ReadAllText(defaultFiltersPath);
                    var filterData = JsonSerializer.Deserialize<List<FilterData>>(jsonString);

                    // Clear existing filters (should be empty on startup, but just in case)
                    Panel filterConditionsPanel = (Panel)filterForm.Controls.Find("filterConditionsPanel", true)[0];
                    filterConditionsPanel.Controls.Clear();
                    filterConditions.Clear();

                    // Add label back
                    Label explanationLabel = new Label
                    {
                        Text = "Add filters to show only lines that match the criteria. Each filter is applied with OR logic.",
                        Dock = DockStyle.Top,
                        Height = 40,
                        Font = new Font("Segoe UI", 9),
                        ForeColor = Color.DimGray,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(5)
                    };
                    filterConditionsPanel.Controls.Add(explanationLabel);

                    // Add loaded filters
                    foreach (var filter in filterData)
                    {
                        AddFilterFromData(filter);
                    }
                    
                    // Update status bar
                    statusLabel.Text = $"Default filters loaded from {defaultFiltersPath}";
                }
                catch (Exception ex)
                {
                    // Log error but don't show message box on startup
                    Console.WriteLine($"Error loading default filters: {ex.Message}");
                    statusLabel.Text = "Error loading default filters";
                }
            }
        }

        private void SaveContent_Click(object sender, EventArgs e)
        {
            // Check if there's content to save
            if (string.IsNullOrEmpty(mainTextBox.Text))
            {
                MessageBox.Show("There is no content to save.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.Title = "Save Current Content";

                // If a file was previously opened, suggest its name for saving
                if (originalLines.Count > 0)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(lastOpenedFilePath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            saveFileDialog.FileName = $"{fileName}_filtered.txt";
                        }
                    }
                    catch
                    {
                        // If there's any error getting the filename, just use a default
                        saveFileDialog.FileName = "filtered_content.txt";
                    }
                }
                else
                {
                    saveFileDialog.FileName = "content.txt";
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Save the current content of the text box
                        File.WriteAllText(saveFileDialog.FileName, mainTextBox.Text);
                        
                        // Show success message
                        statusLabel.Text = $"Content saved to {saveFileDialog.FileName}";
                        
                        // Optional: Show a message box for confirmation
                        MessageBox.Show($"Content successfully saved to {saveFileDialog.FileName}", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving content: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    public class FilterCondition
    {
        public Panel Container { get; set; }
        public ComboBox TypeComboBox { get; set; }
        public TextBox TextBox { get; set; }
        public Button ColorButton { get; set; }
        public Color HighlightColor { get; set; } = Color.White;
        public CheckBox EnabledCheckBox { get; set; }
        public bool Enabled { get => EnabledCheckBox?.Checked ?? true; }
    }

    public class FilterData
    {
        public string FilterType { get; set; }
        public string FilterText { get; set; }
        public string HighlightColor { get; set; } = "#FFFFFF";
        public bool Enabled { get; set; } = true;
    }

    public class LineNumberRichTextBox : RichTextBox
    {
        private const int WM_PAINT = 0x000F;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_HSCROLL = 0x0114;
        private const int LineNumberPadding = 5;
        private const int LineNumberWidth = 45;
        private readonly Color LineNumberColor;
        private readonly Color GutterBackColor;
        private readonly Color SeparatorColor;
        private StringFormat lineNumberFormat;
        private Font lineNumberFont;
        private SolidBrush lineNumberBrush;
        private SolidBrush gutterBrush;
        private Pen separatorPen;
        private bool isLargeFile = false;
        private bool showLineNumbers = false;

        public bool ShowLineNumbers
        {
            get => showLineNumbers;
            set
            {
                if (showLineNumbers != value)
                {
                    showLineNumbers = value;
                    UpdateTextPadding();
                    if (!showLineNumbers)
                    {
                        // Force a clean redraw when hiding line numbers
                        Invalidate();
                    }
                }
            }
        }

        public LineNumberRichTextBox()
        {
            // Initialize readonly color fields
            LineNumberColor = Color.FromArgb(140, 140, 140);
            GutterBackColor = Color.FromArgb(240, 240, 240);
            SeparatorColor = Color.FromArgb(255, 128, 0);

            // Basic settings
            this.Margin = new Padding(0);
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;
            this.Font = new Font("Consolas", 8.25F);
            this.WordWrap = false;
            this.ScrollBars = RichTextBoxScrollBars.Both;
            this.BorderStyle = BorderStyle.None;
            this.DetectUrls = false;
            this.HideSelection = false;

            // Set initial padding
            this.SelectionIndent = 5;  // Start with minimum padding

            // Enable double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw, true);
        }

        private void InitializeLineNumberResources()
        {
            if (lineNumberFormat == null)
            {
                lineNumberFormat = new StringFormat { 
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };
                lineNumberFont = new Font("Consolas", 8.25F);
                lineNumberBrush = new SolidBrush(LineNumberColor);
                gutterBrush = new SolidBrush(GutterBackColor);
                separatorPen = new Pen(SeparatorColor);
            }
        }

        private void UpdateTextPadding()
        {
            this.SelectionIndent = showLineNumbers ? LineNumberWidth + 5 : 5;
            if (showLineNumbers && lineNumberFormat == null)
            {
                InitializeLineNumberResources();
            }
            this.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (showLineNumbers && m.Msg == WM_PAINT)
            {
                DrawLineNumbers();
            }
        }

        private void DrawLineNumbers()
        {
            if (!showLineNumbers || lineNumberFormat == null) return;

            using (Graphics g = CreateGraphics())
            {
                // Fill gutter background
                g.FillRectangle(gutterBrush, 0, 0, LineNumberWidth, ClientSize.Height);

                // Only calculate visible lines if we're actually showing line numbers
                Point pt = new Point(0, 0);
                int firstLine = GetLineFromCharIndex(GetCharIndexFromPosition(pt));
                int lastLine = GetLineFromCharIndex(GetCharIndexFromPosition(new Point(0, ClientSize.Height)));

                // Draw the line numbers
                for (int i = firstLine; i <= lastLine + 1 && i < Lines.Length; i++)
                {
                    pt.Y = GetPositionFromCharIndex(GetFirstCharIndexFromLine(i)).Y;
                    if (pt.Y >= 0 && pt.Y <= ClientSize.Height)
                    {
                        string lineNumber = (i + 1).ToString();
                        g.DrawString(
                            lineNumber,
                            lineNumberFont,
                            lineNumberBrush,
                            new RectangleF(0, pt.Y, LineNumberWidth - LineNumberPadding, Font.Height),
                            lineNumberFormat);
                    }
                }

                // Draw separator line
                g.DrawLine(separatorPen, LineNumberWidth - 1, 0, LineNumberWidth - 1, ClientSize.Height);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            isLargeFile = Lines.Length > 10000;
            if (showLineNumbers)
            {
                Invalidate();
            }
        }

        protected override void OnVScroll(EventArgs e)
        {
            base.OnVScroll(e);
            if (showLineNumbers)
            {
                Invalidate();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (showLineNumbers)
            {
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lineNumberFormat?.Dispose();
                lineNumberFont?.Dispose();
                lineNumberBrush?.Dispose();
                gutterBrush?.Dispose();
                separatorPen?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void SetShowLineNumbers(bool show)
        {
            ShowLineNumbers = show;
        }
    }
}