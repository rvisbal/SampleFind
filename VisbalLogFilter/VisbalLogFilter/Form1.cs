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
using VisbalLogFilter.Libraries;

namespace VisbalLogFilter
{
    public partial class Form1 : Form
    {
        private List<FilterCondition> filterConditions = new List<FilterCondition>();
        private List<string> originalLines = new List<string>();
        private Form filterForm;
        private Libraries.LineNumberRichTextBox mainTextBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private string lastOpenedFilePath = string.Empty;
        
        // Add new fields for replacement functionality
        private List<ReplaceCondition> replaceConditions = new List<ReplaceCondition>();
        private TabControl tabControl;
        private TabPage filtersTabPage;
        private TabPage replaceTabPage;

        // Add a field for the AppearanceLibrary
        private AppearanceLibrary appearanceLibrary;

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
            
            // Set a larger size for the main form
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Set the form title and icon
            this.Text = "VisbalLogFilter";
            this.Icon = GetFolderIcon();
            
            InitializeCustomComponents();
            
            // Initialize filter conditions list
            filterConditions = new List<FilterCondition>();
            
            // Initialize replace conditions list
            replaceConditions = new List<ReplaceCondition>();
            
            // Create the filter form
            CreateFilterForm();
            
            // Try to load default filters if available
            LoadDefaultFilters();
        }

        private void InitializeCustomComponents()
        {
            // Set up the main text box with line numbers
            mainTextBox = new Libraries.LineNumberRichTextBox
            {
                Dock = DockStyle.Fill,
                ShowLineNumbers = false,
                Font = new Font("Consolas", 9.75F),
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(mainTextBox);

            // Create status bar
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);

            // Create menu
            MenuStrip menuStrip = new MenuStrip();
            
            // File menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem openFileMenuItem = new ToolStripMenuItem("Open File", null, OpenFile_Click);
            ToolStripMenuItem saveContentMenuItem = new ToolStripMenuItem("Save Content", null, SaveContent_Click);
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openFileMenuItem, saveContentMenuItem, new ToolStripSeparator(), exitMenuItem });

            // View menu
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
            ToolStripMenuItem lineNumbersMenuItem = new ToolStripMenuItem("Line Numbers", null, ShowSidebar_Click);
            viewMenu.DropDownItems.Add(lineNumbersMenuItem);

            // Tools menu
            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Tools");
            ToolStripMenuItem filtersMenuItem = new ToolStripMenuItem("Filters", null, ShowFilters_Click);
            ToolStripMenuItem cleanDateMenuItem = new ToolStripMenuItem("Clean Date Part", null, CleanDatePart_Click);
            ToolStripMenuItem combineMenuItem = new ToolStripMenuItem("Combine Identical Lines", null, CombineIdenticalLines_Click);
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { filtersMenuItem, cleanDateMenuItem, combineMenuItem });
            
            // Add all menus to the menu strip
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, toolsMenu });
            
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Ensure proper z-order
            mainTextBox.BringToFront();
            menuStrip.BringToFront();
            statusStrip.BringToFront();
        }

        private void CreateFilterForm()
        {
            // Create the filter form
            filterForm = new Form
            {
                Text = "Settings",
                Size = new Size(700, 700),
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ShowInTaskbar = false
            };

            // Create tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 10)
            };

            // Create tabs
            filtersTabPage = new TabPage("Filters");
            replaceTabPage = new TabPage("Replace");
            var appearanceTabPage = new TabPage("Appearance");

            // Add tabs to tab control
            tabControl.TabPages.Add(filtersTabPage);
            tabControl.TabPages.Add(replaceTabPage);
            tabControl.TabPages.Add(appearanceTabPage);

            // Add tab control to form
            filterForm.Controls.Add(tabControl);

            // Set up the tabs
            SetupFiltersTab();
            SetupReplaceTab();
            SetupAppearanceTab();
        }

        private void SetupFiltersTab()
        {
            // Top panel for buttons
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Create styled buttons
            var addFilterButton = UIControlsLibrary.CreateStyledButton(
                "Add Filter", 
                120, 
                30, 
                new Point(10, 10), 
                Color.FromArgb(76, 175, 80)
            );
            addFilterButton.Click += AddFilter_Click;

            var applyButton = UIControlsLibrary.CreateStyledButton(
                "Apply Filters", 
                120, 
                30, 
                new Point(140, 10), 
                Color.FromArgb(33, 150, 243)
            );
            applyButton.Click += ApplyFilters_Click;

            var loadButton = UIControlsLibrary.CreateStyledButton(
                "Load Filters", 
                120, 
                30, 
                new Point(270, 10), 
                Color.FromArgb(255, 152, 0)
            );
            loadButton.Click += LoadFilters_Click;

            var saveButton = UIControlsLibrary.CreateStyledButton(
                "Save Filters", 
                120, 
                30, 
                new Point(400, 10), 
                Color.FromArgb(244, 67, 54)
            );
            saveButton.Click += SaveFilters_Click;

            // Add a Reset Colors button
            var resetColorsButton = UIControlsLibrary.CreateStyledButton("Reset Colors", 120, 30, new Point(filtersTabPage.Width - 140, 10), Color.DarkGray);
            resetColorsButton.Click += ResetColors_Click;
            filtersTabPage.Controls.Add(resetColorsButton);

            topPanel.Controls.AddRange(new Control[] { addFilterButton, applyButton, loadButton, saveButton });

            // Create filter container panel
            Panel filterContainerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            // Create a scrollable panel for filter conditions
            Panel filterScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true, // Enable scrolling
                Padding = new Padding(0, 0, 5, 0) // Add padding for scrollbar
            };

            // Create filter conditions panel inside the scroll panel
            Panel filterConditionsPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(5),
                Name = "filterConditionsPanel",
                Width = filterContainerPanel.Width - 30 // Make slightly narrower to accommodate scrollbar
            };

            // Add explanation label
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

            // Add panels to form in the correct hierarchy
            filterScrollPanel.Controls.Add(filterConditionsPanel);
            filterContainerPanel.Controls.Add(filterScrollPanel);
            
            // Add panels to the Filters tab
            filtersTabPage.Controls.Add(filterContainerPanel);
            filtersTabPage.Controls.Add(topPanel);
        }

        private void SetupReplaceTab()
        {
            // Top panel for buttons
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Create styled buttons for the Replace tab
            var addReplaceButton = UIControlsLibrary.CreateStyledButton(
                "Add Replace", 
                120, 
                30, 
                new Point(10, 10), 
                Color.FromArgb(76, 175, 80)
            );
            addReplaceButton.Click += AddReplace_Click;

            var applyReplaceButton = UIControlsLibrary.CreateStyledButton(
                "Apply Replace", 
                120, 
                30, 
                new Point(140, 10), 
                Color.FromArgb(33, 150, 243)
            );
            applyReplaceButton.Click += ApplyReplace_Click;

            var loadReplaceButton = UIControlsLibrary.CreateStyledButton(
                "Load Replace", 
                120, 
                30, 
                new Point(270, 10), 
                Color.FromArgb(255, 152, 0)
            );
            loadReplaceButton.Click += LoadReplace_Click;

            var saveReplaceButton = UIControlsLibrary.CreateStyledButton(
                "Save Replace", 
                120, 
                30, 
                new Point(400, 10), 
                Color.FromArgb(244, 67, 54)
            );
            saveReplaceButton.Click += SaveReplace_Click;

            topPanel.Controls.AddRange(new Control[] { addReplaceButton, applyReplaceButton, loadReplaceButton, saveReplaceButton });

            // Create replace container panel
            Panel replaceContainerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            // Create a scrollable panel for replace conditions
            Panel replaceScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true, // Enable scrolling
                Padding = new Padding(0, 0, 5, 0) // Add padding for scrollbar
            };

            // Create replace conditions panel inside the scroll panel
            Panel replaceConditionsPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(5),
                Name = "replaceConditionsPanel",
                Width = replaceContainerPanel.Width - 30 // Make slightly narrower to accommodate scrollbar
            };

            // Add explanation label
            Label explanationLabel = new Label
            {
                Text = "Add text to find and replace. Each replacement is applied in order from top to bottom.",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5)
            };
            replaceConditionsPanel.Controls.Add(explanationLabel);

            // Add panels to form in the correct hierarchy
            replaceScrollPanel.Controls.Add(replaceConditionsPanel);
            replaceContainerPanel.Controls.Add(replaceScrollPanel);
            
            // Add panels to the Replace tab
            replaceTabPage.Controls.Add(replaceContainerPanel);
            replaceTabPage.Controls.Add(topPanel);
        }

        private void SetupAppearanceTab()
        {
            // Get the Appearance tab page
            TabPage appearanceTabPage = tabControl.TabPages[2]; // Use index instead of name

            // Initialize the AppearanceLibrary
            appearanceLibrary = new AppearanceLibrary(this, mainTextBox);
            
            // Create the appearance UI
            appearanceLibrary.CreateAppearanceUI(appearanceTabPage);
            
            // Subscribe to appearance changed events
            appearanceLibrary.AppearanceChanged += (sender, e) =>
            {
                // Update status
                statusLabel.Text = $"Appearance settings updated: Font={e.Settings.FontName}, Size={e.Settings.FontSize}";
            };
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
                    // Always position the filter form directly to the right of the main form
                    int filterFormX = this.Right;
                    int filterFormY = this.Top;
                    
                    // Make sure the filter form is visible on screen
                    Rectangle screenBounds = Screen.FromControl(this).Bounds;
                    if (filterFormX + filterForm.Width > screenBounds.Right)
                    {
                        // If it would go off screen to the right, position it to the left of the main form
                        filterFormX = this.Left - filterForm.Width;
                        
                        // If it would still be off screen to the left, position it at the same X as the main form
                        // but offset it vertically to avoid complete overlap
                        if (filterFormX < screenBounds.Left)
                        {
                            filterFormX = this.Left;
                            filterFormY = this.Top + 50; // Offset vertically
                        }
                    }
                    
                    // Set the position and ensure it's a child of the main form
                    filterForm.StartPosition = FormStartPosition.Manual;
                    filterForm.Location = new Point(filterFormX, filterFormY);
                    filterForm.Owner = this; // Make it a child window of the main form
                    filterForm.Show();
                    
                    // Bring the filter form to the front
                    filterForm.BringToFront();
                    
                    // Ensure the filter form stays on top of the main form
                    filterForm.TopMost = true;
                    Application.DoEvents();
                    filterForm.TopMost = false;
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
            enabledCheckBox.CheckedChanged += (s, ev) =>
            {
                // Don't automatically apply filters when checkbox changes
                statusLabel.Text = "Filter enabled/disabled. Click 'Apply Filters' to update.";
            };

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

            // Create a simple button for color selection
            Button colorButton = new Button
            {
                Width = 40,
                Height = 30,
                Location = new Point(380, 10),
                BackColor = Color.FromArgb(0, 123, 255), // Default blue color
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Text = "",
                Tag = "FilterColorButton"
            };

            // Set up event handlers after all controls are created
            colorButton.Click += async (s, ev) =>
            {
                // Ensure we're working with the correct button
                if (s is Button clickedButton && clickedButton.Tag as string == "FilterColorButton")
                {
                    using (ColorDialog colorDialog = new ColorDialog())
                    {
                        colorDialog.Color = clickedButton.BackColor;
                        colorDialog.FullOpen = true;
                        
                        if (colorDialog.ShowDialog() == DialogResult.OK)
                        {
                            // Set the color only on this specific button
                            clickedButton.BackColor = colorDialog.Color;
                            
                            // Find and update only the matching filter condition
                            FilterCondition matchingCondition = null;
                            foreach (var condition in filterConditions)
                            {
                                if (ReferenceEquals(condition.ColorButton, clickedButton))
                                {
                                    matchingCondition = condition;
                                    break;
                                }
                            }
                            
                            if (matchingCondition != null)
                            {
                                // Use the setter which will only update this specific button
                                matchingCondition.HighlightColor = colorDialog.Color;
                                
                                // Show confirmation
                                statusLabel.Text = $"Filter color updated: R={colorDialog.Color.R}, G={colorDialog.Color.G}, B={colorDialog.Color.B}. Click Apply Filters.";
                            }
                        }
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
                // Don't automatically apply filters when removing one
                statusLabel.Text = "Filter removed. Click 'Apply Filters' to update.";
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
                        
                        // Show message to remind user to apply filters
                        statusLabel.Text = $"Filters loaded from {openFileDialog.FileName}. Click 'Apply Filters' to use them.";
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
            enabledCheckBox.CheckedChanged += (s, ev) =>
            {
                // Don't automatically apply filters when checkbox changes
                statusLabel.Text = "Filter enabled/disabled. Click 'Apply Filters' to update.";
            };

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

            // Create a simple button for color selection
            Button colorButton = new Button
            {
                Width = 40,
                Height = 30,
                Location = new Point(380, 10),
                BackColor = ColorTranslator.FromHtml(filterData.HighlightColor),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Text = "",
                Tag = "FilterColorButton"
            };

            // Set up event handlers after all controls are created
            colorButton.Click += async (s, ev) =>
            {
                // Ensure we're working with the correct button
                if (s is Button clickedButton && clickedButton.Tag as string == "FilterColorButton")
                {
                    using (ColorDialog colorDialog = new ColorDialog())
                    {
                        colorDialog.Color = clickedButton.BackColor;
                        colorDialog.FullOpen = true;
                        
                        if (colorDialog.ShowDialog() == DialogResult.OK)
                        {
                            // Set the color only on this specific button
                            clickedButton.BackColor = colorDialog.Color;
                            
                            // Find and update only the matching filter condition
                            FilterCondition matchingCondition = null;
                            foreach (var condition in filterConditions)
                            {
                                if (ReferenceEquals(condition.ColorButton, clickedButton))
                                {
                                    matchingCondition = condition;
                                    break;
                                }
                            }
                            
                            if (matchingCondition != null)
                            {
                                // Use the setter which will only update this specific button
                                matchingCondition.HighlightColor = colorDialog.Color;
                                
                                // Show confirmation
                                statusLabel.Text = $"Filter color updated: R={colorDialog.Color.R}, G={colorDialog.Color.G}, B={colorDialog.Color.B}. Click Apply Filters.";
                            }
                        }
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
                // Don't automatically apply filters when removing one
                statusLabel.Text = "Filter removed. Click 'Apply Filters' to update.";
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
            // Toggle line numbers
            bool currentState = mainTextBox.ShowLineNumbers;
            mainTextBox.ShowLineNumbers = !currentState;
            
            // Update menu item checked state if sender is a ToolStripMenuItem
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = !currentState;
            }
            
            // Update status
            statusLabel.Text = mainTextBox.ShowLineNumbers ? "Line numbers shown" : "Line numbers hidden";
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

        private void CleanDatePart_Click(object sender, EventArgs e)
        {
            // Check if there's content to process
            if (originalLines.Count == 0)
            {
                MessageBox.Show("Please open a file first.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Show processing indicator
                statusLabel.Text = "Processing...";
                Application.DoEvents();

                // Create a new list to store the cleaned lines
                List<string> cleanedLines = new List<string>(originalLines.Count);
                
                // Process each line
                foreach (string line in originalLines)
                {
                    string cleanedLine = line;
                    int pipeIndex = line.IndexOf('|');
                    
                    // If the line contains a pipe character, remove everything before it
                    if (pipeIndex >= 0)
                    {
                        cleanedLine = line.Substring(pipeIndex + 1);
                    }
                    
                    cleanedLines.Add(cleanedLine);
                }
                
                // Replace the original lines with the cleaned lines
                originalLines = cleanedLines;
                
                // Update the display
                mainTextBox.SuspendLayout();
                mainTextBox.Clear();
                
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
                
                // Inform user they need to reapply filters if needed
                if (filterConditions.Count > 0 && filterConditions.Any(c => c.Enabled && !string.IsNullOrEmpty(c.TextBox.Text)))
                {
                    statusLabel.Text = $"Date part removed from {originalLines.Count:N0} lines. Click 'Apply Filters' to update the view.";
                }
                else
                {
                    statusLabel.Text = $"Date part removed from {originalLines.Count:N0} lines.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing content: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error processing content";
            }
        }

        private void CombineIdenticalLines_Click(object sender, EventArgs e)
        {
            // Check if there's content to process
            if (originalLines.Count == 0)
            {
                MessageBox.Show("Please open a file first.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Show processing indicator
                statusLabel.Text = "Combining identical lines...";
                Application.DoEvents();

                // Create a new list to store the combined lines
                List<string> combinedLines = new List<string>();
                
                if (originalLines.Count > 0)
                {
                    string currentLine = originalLines[0];
                    int count = 1;
                    
                    // Process from the second line onwards
                    for (int i = 1; i < originalLines.Count; i++)
                    {
                        if (originalLines[i] == currentLine)
                        {
                            // Same line, increment counter
                            count++;
                        }
                        else
                        {
                            // Different line, add the previous line with count if needed
                            if (count > 1)
                            {
                                combinedLines.Add($"{currentLine} - {count}");
                            }
                            else
                            {
                                combinedLines.Add(currentLine);
                            }
                            
                            // Reset for the new line
                            currentLine = originalLines[i];
                            count = 1;
                        }
                    }
                    
                    // Add the last line
                    if (count > 1)
                    {
                        combinedLines.Add($"{currentLine} - {count}");
                    }
                    else
                    {
                        combinedLines.Add(currentLine);
                    }
                }
                
                // Replace the original lines with the combined lines
                originalLines = combinedLines;
                
                // Update the display
                mainTextBox.SuspendLayout();
                mainTextBox.Clear();
                
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
                
                // Inform user they need to reapply filters if needed
                if (filterConditions.Count > 0 && filterConditions.Any(c => c.Enabled && !string.IsNullOrEmpty(c.TextBox.Text)))
                {
                    statusLabel.Text = $"Combined identical lines: {combinedLines.Count:N0} lines (reduced by {Math.Abs(combinedLines.Count - originalLines.Count):N0} lines). Click 'Apply Filters' to update the view.";
                }
                else
                {
                    statusLabel.Text = $"Combined identical lines: {combinedLines.Count:N0} lines (reduced by {Math.Abs(combinedLines.Count - originalLines.Count):N0} lines).";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing content: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error combining lines";
            }
        }

        // Add methods for the Replace functionality
        private void AddReplace_Click(object sender, EventArgs e)
        {
            Panel replaceConditionsPanel = (Panel)filterForm.Controls.Find("replaceConditionsPanel", true)[0];
            
            // Create replace container with simplified styling
            Panel replaceContainer = new Panel
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

            // Create Find TextBox
            TextBox findTextBox = new TextBox
            {
                Width = 220,
                Height = 30,
                Location = new Point(40, 10),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Text to find..."
            };

            // Create Replace TextBox
            TextBox replaceTextBox = new TextBox
            {
                Width = 220,
                Height = 30,
                Location = new Point(270, 10),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Replace with..."
            };

            // Create a rounded remove button
            RoundedButton removeButton = new RoundedButton
            {
                Text = "X",
                Width = 40,
                Height = 30,
                Location = new Point(500, 10),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                BorderRadius = 10,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            removeButton.Click += (s, ev) =>
            {
                replaceConditionsPanel.Controls.Remove(replaceContainer);
                replaceConditions.RemoveAll(r => r.Container == replaceContainer);
                statusLabel.Text = "Replace rule removed. Click 'Apply Replace' to update.";
            };

            replaceContainer.Controls.AddRange(new Control[] { enabledCheckBox, findTextBox, replaceTextBox, removeButton });
            
            // Add new replace condition
            ReplaceCondition condition = new ReplaceCondition
            {
                Container = replaceContainer,
                FindTextBox = findTextBox,
                ReplaceTextBox = replaceTextBox,
                EnabledCheckBox = enabledCheckBox
            };
            replaceConditions.Add(condition);

            // Add to panel below the buttons
            replaceConditionsPanel.Controls.Add(replaceContainer);
            replaceContainer.BringToFront();
        }

        private void ApplyReplace_Click(object sender, EventArgs e)
        {
            if (originalLines.Count == 0)
            {
                MessageBox.Show("Please open a file first.", "No File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show processing indicator in status bar
            statusLabel.Text = "Processing replacements...";
            Application.DoEvents(); // Allow UI to update

            // Get enabled replace conditions
            var enabledReplaceConditions = replaceConditions.Where(c => c.Enabled).ToList();

            if (enabledReplaceConditions.Count == 0)
            {
                statusLabel.Text = "No replacements to apply.";
                return;
            }

            try
            {
                // Create a copy of the original lines to work with
                List<string> modifiedLines = new List<string>(originalLines);
                int replacementsCount = 0;

                // Apply each replacement
                foreach (var condition in enabledReplaceConditions)
                {
                    string findText = condition.FindTextBox.Text;
                    string replaceText = condition.ReplaceTextBox.Text;

                    if (!string.IsNullOrEmpty(findText))
                    {
                        for (int i = 0; i < modifiedLines.Count; i++)
                        {
                            string originalLine = modifiedLines[i];
                            string newLine = originalLine.Replace(findText, replaceText);
                            
                            if (originalLine != newLine)
                            {
                                modifiedLines[i] = newLine;
                                replacementsCount++;
                            }
                        }
                    }
                }

                // Update the display with the modified text
                mainTextBox.SuspendLayout();
                mainTextBox.Clear();
                
                // For large files, use a StringBuilder and append in chunks
                if (modifiedLines.Count > 10000)
                {
                    const int chunkSize = 5000;
                    for (int i = 0; i < modifiedLines.Count; i += chunkSize)
                    {
                        int count = Math.Min(chunkSize, modifiedLines.Count - i);
                        string chunk = string.Join(Environment.NewLine, modifiedLines.GetRange(i, count));
                        mainTextBox.AppendText(chunk);
                        if (i + count < modifiedLines.Count)
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
                    mainTextBox.Text = string.Join(Environment.NewLine, modifiedLines);
                }
                
                mainTextBox.Select(0, 0);
                mainTextBox.ResumeLayout();
                
                // Update status bar
                statusLabel.Text = $"Replacements applied: {replacementsCount} occurrences replaced.";
                
                // Update the original lines with the modified ones
                originalLines = modifiedLines;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying replacements: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error applying replacements.";
            }
        }

        private void SaveReplace_Click(object sender, EventArgs e)
        {
            if (replaceConditions.Count == 0)
            {
                MessageBox.Show("No replacements to save.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        var replaceData = replaceConditions.Select(c => new ReplaceData
                        {
                            FindText = c.FindTextBox.Text,
                            ReplaceText = c.ReplaceTextBox.Text,
                            Enabled = c.EnabledCheckBox.Checked
                        }).ToList();

                        string jsonString = JsonSerializer.Serialize(replaceData, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                        File.WriteAllText(saveFileDialog.FileName, jsonString);
                        MessageBox.Show($"Replacements saved to {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving replacements: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadReplace_Click(object sender, EventArgs e)
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
                        var replaceData = JsonSerializer.Deserialize<List<ReplaceData>>(jsonString);

                        // Clear existing replacements
                        Panel replaceConditionsPanel = (Panel)filterForm.Controls.Find("replaceConditionsPanel", true)[0];
                        replaceConditionsPanel.Controls.Clear();
                        replaceConditions.Clear();

                        // Add label back
                        Label explanationLabel = new Label
                        {
                            Text = "Add text to find and replace. Each replacement is applied in order from top to bottom.",
                            Dock = DockStyle.Top,
                            Height = 40,
                            Font = new Font("Segoe UI", 9),
                            ForeColor = Color.DimGray,
                            TextAlign = ContentAlignment.MiddleLeft,
                            Padding = new Padding(5)
                        };
                        replaceConditionsPanel.Controls.Add(explanationLabel);

                        // Add loaded replacements
                        foreach (var data in replaceData)
                        {
                            AddReplaceFromData(data);
                        }
                        
                        // Show message to remind user to apply replacements
                        statusLabel.Text = $"Replacements loaded from {openFileDialog.FileName}. Click 'Apply Replace' to use them.";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading replacements: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddReplaceFromData(ReplaceData replaceData)
        {
            Panel replaceConditionsPanel = (Panel)filterForm.Controls.Find("replaceConditionsPanel", true)[0];
            
            // Create replace container with simplified styling
            Panel replaceContainer = new Panel
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
                Checked = replaceData.Enabled,
                Location = new Point(10, 15),
                Width = 20,
                Height = 20
            };

            // Create Find TextBox
            TextBox findTextBox = new TextBox
            {
                Width = 220,
                Height = 30,
                Location = new Point(40, 10),
                Font = new Font("Segoe UI", 10),
                Text = replaceData.FindText,
                PlaceholderText = "Text to find..."
            };

            // Create Replace TextBox
            TextBox replaceTextBox = new TextBox
            {
                Width = 220,
                Height = 30,
                Location = new Point(270, 10),
                Font = new Font("Segoe UI", 10),
                Text = replaceData.ReplaceText,
                PlaceholderText = "Replace with..."
            };

            // Create a rounded remove button
            RoundedButton removeButton = new RoundedButton
            {
                Text = "X",
                Width = 40,
                Height = 30,
                Location = new Point(500, 10),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                BorderRadius = 10,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            removeButton.Click += (s, ev) =>
            {
                replaceConditionsPanel.Controls.Remove(replaceContainer);
                replaceConditions.RemoveAll(r => r.Container == replaceContainer);
                statusLabel.Text = "Replace rule removed. Click 'Apply Replace' to update.";
            };

            replaceContainer.Controls.AddRange(new Control[] { enabledCheckBox, findTextBox, replaceTextBox, removeButton });
            
            // Add new replace condition
            ReplaceCondition condition = new ReplaceCondition
            {
                Container = replaceContainer,
                FindTextBox = findTextBox,
                ReplaceTextBox = replaceTextBox,
                EnabledCheckBox = enabledCheckBox
            };
            replaceConditions.Add(condition);

            // Add to panel below the buttons
            replaceConditionsPanel.Controls.Add(replaceContainer);
            replaceContainer.BringToFront();
        }

        private void ResetColors_Click(object sender, EventArgs e)
        {
            if (filterConditions.Count == 0)
            {
                MessageBox.Show("No filters to reset colors for.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create a FilteringLibrary instance
            var filteringLibrary = new Libraries.FilteringLibrary(this);
            
            // Convert UI filter conditions to models
            var filterConditionModels = filterConditions.Select(c => new Libraries.FilterConditionModel
            {
                FilterType = c.TypeComboBox.Text,
                FilterText = c.TextBox.Text,
                HighlightColor = c.ColorButton.BackColor,
                Enabled = c.EnabledCheckBox.Checked
            }).ToList();
            
            // Reset all colors to white
            var updatedConditions = filteringLibrary.ResetAllColors(filterConditionModels);
            
            // Update the UI filter conditions with the reset colors
            for (int i = 0; i < filterConditions.Count; i++)
            {
                filterConditions[i].ColorButton.BackColor = Color.White;
            }
            
            MessageBox.Show("All filter colors have been reset to white.", "Colors Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class FilterCondition
    {
        public Panel Container { get; set; }
        public ComboBox TypeComboBox { get; set; }
        public TextBox TextBox { get; set; }
        public Button ColorButton { get; set; }
        private Color highlightColor = Color.White;
        public Color HighlightColor 
        { 
            get { return highlightColor; }
            set 
            { 
                highlightColor = value;
                if (ColorButton != null)
                {
                    ColorButton.BackColor = value;
                }
            }
        }
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

    public class ReplaceCondition
    {
        public Panel Container { get; set; }
        public TextBox FindTextBox { get; set; }
        public TextBox ReplaceTextBox { get; set; }
        public CheckBox EnabledCheckBox { get; set; }
        public bool Enabled { get => EnabledCheckBox?.Checked ?? true; }
    }

    public class ReplaceData
    {
        public string FindText { get; set; }
        public string ReplaceText { get; set; }
        public bool Enabled { get; set; } = true;
    }
}