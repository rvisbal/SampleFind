using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisbalLogFilter.Libraries
{
    /// <summary>
    /// Library class that handles appearance settings for the text display
    /// </summary>
    public class AppearanceLibrary
    {
        // Events
        public event EventHandler<AppearanceChangedEventArgs> AppearanceChanged;

        // Private fields
        private readonly Control parentControl;
        private readonly RichTextBox targetTextBox;

        /// <summary>
        /// Constructor for the AppearanceLibrary
        /// </summary>
        /// <param name="parentControl">The parent control for UI thread operations</param>
        /// <param name="targetTextBox">The text box to apply appearance settings to</param>
        public AppearanceLibrary(Control parentControl, RichTextBox targetTextBox)
        {
            this.parentControl = parentControl;
            this.targetTextBox = targetTextBox;
        }

        /// <summary>
        /// Apply appearance settings to the target text box
        /// </summary>
        /// <param name="fontName">The font name</param>
        /// <param name="fontSize">The font size</param>
        /// <param name="textColor">The text color</param>
        /// <param name="backgroundColor">The background color</param>
        public void ApplyAppearanceSettings(string fontName, float fontSize, Color textColor, Color backgroundColor)
        {
            if (targetTextBox == null)
                return;

            try
            {
                // Apply settings to the target text box
                targetTextBox.Font = new Font(fontName, fontSize);
                targetTextBox.ForeColor = textColor;
                targetTextBox.BackColor = backgroundColor;

                // Raise the AppearanceChanged event
                OnAppearanceChanged(new AppearanceSettings
                {
                    FontName = fontName,
                    FontSize = fontSize,
                    TextColor = textColor,
                    BackgroundColor = backgroundColor
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error applying appearance settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the current appearance settings from the target text box
        /// </summary>
        /// <returns>The current appearance settings</returns>
        public AppearanceSettings GetCurrentAppearanceSettings()
        {
            if (targetTextBox == null)
                return GetDefaultAppearanceSettings();

            return new AppearanceSettings
            {
                FontName = targetTextBox.Font.FontFamily.Name,
                FontSize = targetTextBox.Font.Size,
                TextColor = targetTextBox.ForeColor,
                BackgroundColor = targetTextBox.BackColor
            };
        }

        /// <summary>
        /// Get default appearance settings
        /// </summary>
        /// <returns>The default appearance settings</returns>
        public AppearanceSettings GetDefaultAppearanceSettings()
        {
            return new AppearanceSettings
            {
                FontName = "Consolas",
                FontSize = 10f,
                TextColor = Color.Black,
                BackgroundColor = Color.White
            };
        }

        /// <summary>
        /// Get a list of recommended monospace fonts
        /// </summary>
        /// <returns>An array of recommended monospace font names</returns>
        public string[] GetRecommendedFonts()
        {
            return new string[]
            {
                "Consolas",
                "Courier New",
                "Lucida Console",
                "Monaco",
                "DejaVu Sans Mono"
            };
        }

        /// <summary>
        /// Get a list of recommended font sizes
        /// </summary>
        /// <returns>An array of recommended font sizes</returns>
        public string[] GetRecommendedFontSizes()
        {
            return new string[]
            {
                "8", "9", "10", "11", "12", "14", "16", "18", "20"
            };
        }

        /// <summary>
        /// Determine a contrasting text color for a background color
        /// </summary>
        /// <param name="color">The background color</param>
        /// <returns>A contrasting text color (black or white)</returns>
        public Color GetContrastColor(Color color)
        {
            // Calculate the perceptive luminance (perceived brightness)
            // This formula is based on human perception of color
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            
            // If the color is bright, return black; otherwise, return white
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        /// <summary>
        /// Create the appearance settings UI
        /// </summary>
        /// <param name="container">The container to add the UI to</param>
        /// <returns>The panel containing the appearance settings UI</returns>
        public Panel CreateAppearanceUI(Control container)
        {
            // Create a panel for the appearance settings
            var appearancePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Get current settings
            var currentSettings = GetCurrentAppearanceSettings();

            // Create the preview text box first so it can be referenced in event handlers
            var previewTextBox = new RichTextBox
            {
                Location = new Point(10, 210),
                Size = new Size(650, 150),
                Font = new Font(currentSettings.FontName, currentSettings.FontSize),
                ForeColor = currentSettings.TextColor,
                BackColor = currentSettings.BackgroundColor,
                Text = "This is a preview of how your text will look.\r\nABCDEFGHIJKLMNOPQRSTUVWXYZ\r\nabcdefghijklmnopqrstuvwxyz\r\n0123456789\r\n!@#$%^&*()_+-=[]{}|;':\",./<>?"
            };

            // Font Type section
            var fontTypeLabel = new Label
            {
                Text = "Font Type:",
                Location = new Point(10, 20),
                AutoSize = true
            };

            var fontTypeComboBox = new ComboBox
            {
                Location = new Point(150, 20),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Add recommended fonts
            fontTypeComboBox.Items.AddRange(GetRecommendedFonts());

            // Set current font
            fontTypeComboBox.SelectedItem = currentSettings.FontName;
            if (fontTypeComboBox.SelectedIndex == -1 && fontTypeComboBox.Items.Count > 0)
            {
                fontTypeComboBox.SelectedIndex = 0; // Default to first font if current not in list
            }

            // Font Size section
            var fontSizeLabel = new Label
            {
                Text = "Font Size:",
                Location = new Point(10, 60),
                AutoSize = true
            };

            var fontSizeComboBox = new ComboBox
            {
                Location = new Point(150, 60),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Add recommended font sizes
            fontSizeComboBox.Items.AddRange(GetRecommendedFontSizes());

            // Set current font size
            fontSizeComboBox.SelectedItem = currentSettings.FontSize.ToString("0");
            if (fontSizeComboBox.SelectedIndex == -1 && fontSizeComboBox.Items.Count > 0)
            {
                fontSizeComboBox.SelectedIndex = 2; // Default to 10pt if current not in list
            }

            // Text Color section
            var textColorLabel = new Label
            {
                Text = "Text Color:",
                Location = new Point(10, 100),
                AutoSize = true
            };

            var textColorButton = new Button
            {
                Text = "Choose Color",
                Location = new Point(150, 100),
                Width = 120,
                BackColor = currentSettings.TextColor,
                ForeColor = GetContrastColor(currentSettings.TextColor)
            };

            // Background Color section
            var bgColorLabel = new Label
            {
                Text = "Background Color:",
                Location = new Point(10, 140),
                AutoSize = true
            };

            var bgColorButton = new Button
            {
                Text = "Choose Color",
                Location = new Point(150, 140),
                Width = 120,
                BackColor = currentSettings.BackgroundColor,
                ForeColor = GetContrastColor(currentSettings.BackgroundColor)
            };

            // Set up event handlers after all controls are created
            textColorButton.Click += (s, e) =>
            {
                using (var colorDialog = new ColorDialog())
                {
                    colorDialog.Color = textColorButton.BackColor;
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        textColorButton.BackColor = colorDialog.Color;
                        textColorButton.ForeColor = GetContrastColor(colorDialog.Color);
                        UpdatePreview(previewTextBox, fontTypeComboBox, fontSizeComboBox, textColorButton, bgColorButton);
                    }
                }
            };

            bgColorButton.Click += (s, e) =>
            {
                using (var colorDialog = new ColorDialog())
                {
                    colorDialog.Color = bgColorButton.BackColor;
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        bgColorButton.BackColor = colorDialog.Color;
                        bgColorButton.ForeColor = GetContrastColor(colorDialog.Color);
                        UpdatePreview(previewTextBox, fontTypeComboBox, fontSizeComboBox, textColorButton, bgColorButton);
                    }
                }
            };

            // Preview section
            var previewLabel = new Label
            {
                Text = "Preview:",
                Location = new Point(10, 180),
                AutoSize = true
            };

            // Update preview when font type or size changes
            fontTypeComboBox.SelectedIndexChanged += (s, e) => 
                UpdatePreview(previewTextBox, fontTypeComboBox, fontSizeComboBox, textColorButton, bgColorButton);
            
            fontSizeComboBox.SelectedIndexChanged += (s, e) => 
                UpdatePreview(previewTextBox, fontTypeComboBox, fontSizeComboBox, textColorButton, bgColorButton);

            // Apply button
            var applyButton = new Button
            {
                Text = "Apply Changes",
                Location = new Point(510, 380),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            applyButton.Click += (s, e) =>
            {
                try
                {
                    string fontName = fontTypeComboBox.SelectedItem?.ToString() ?? "Consolas";
                    float fontSize = float.Parse(fontSizeComboBox.SelectedItem?.ToString() ?? "10");
                    
                    // Apply changes to the target text box
                    ApplyAppearanceSettings(fontName, fontSize, textColorButton.BackColor, bgColorButton.BackColor);
                    
                    MessageBox.Show("Appearance settings applied successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error applying settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Reset button
            var resetButton = new Button
            {
                Text = "Reset to Default",
                Location = new Point(350, 380),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            resetButton.Click += (s, e) =>
            {
                // Get default settings
                var defaultSettings = GetDefaultAppearanceSettings();
                
                // Reset to default values
                fontTypeComboBox.SelectedItem = defaultSettings.FontName;
                fontSizeComboBox.SelectedItem = defaultSettings.FontSize.ToString("0");
                textColorButton.BackColor = defaultSettings.TextColor;
                textColorButton.ForeColor = GetContrastColor(defaultSettings.TextColor);
                bgColorButton.BackColor = defaultSettings.BackgroundColor;
                bgColorButton.ForeColor = GetContrastColor(defaultSettings.BackgroundColor);
                
                // Update preview
                UpdatePreview(previewTextBox, fontTypeComboBox, fontSizeComboBox, textColorButton, bgColorButton);
            };

            // Add controls to panel
            appearancePanel.Controls.AddRange(new Control[] {
                fontTypeLabel, fontTypeComboBox,
                fontSizeLabel, fontSizeComboBox,
                textColorLabel, textColorButton,
                bgColorLabel, bgColorButton,
                previewLabel, previewTextBox,
                applyButton, resetButton
            });

            // Add panel to container
            container.Controls.Add(appearancePanel);

            return appearancePanel;
        }

        /// <summary>
        /// Update the preview text box with the current settings
        /// </summary>
        private void UpdatePreview(RichTextBox previewTextBox, ComboBox fontTypeComboBox, ComboBox fontSizeComboBox, Button textColorButton, Button bgColorButton)
        {
            try
            {
                string fontName = fontTypeComboBox.SelectedItem?.ToString() ?? "Consolas";
                float fontSize = float.Parse(fontSizeComboBox.SelectedItem?.ToString() ?? "10");
                previewTextBox.Font = new Font(fontName, fontSize);
                previewTextBox.ForeColor = textColorButton.BackColor;
                previewTextBox.BackColor = bgColorButton.BackColor;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating preview: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Trigger the AppearanceChanged event
        /// </summary>
        /// <param name="settings">The new appearance settings</param>
        private void OnAppearanceChanged(AppearanceSettings settings)
        {
            if (parentControl.IsDisposed || !parentControl.IsHandleCreated)
                return;

            parentControl.Invoke((Action)(() =>
            {
                AppearanceChanged?.Invoke(this, new AppearanceChangedEventArgs(settings));
            }));
        }
    }

    /// <summary>
    /// Class to store appearance settings
    /// </summary>
    public class AppearanceSettings
    {
        public string FontName { get; set; } = "Consolas";
        public float FontSize { get; set; } = 10f;
        public Color TextColor { get; set; } = Color.Black;
        public Color BackgroundColor { get; set; } = Color.White;
    }

    /// <summary>
    /// Event args for appearance changes
    /// </summary>
    public class AppearanceChangedEventArgs : EventArgs
    {
        public AppearanceSettings Settings { get; }

        public AppearanceChangedEventArgs(AppearanceSettings settings)
        {
            Settings = settings;
        }
    }
} 