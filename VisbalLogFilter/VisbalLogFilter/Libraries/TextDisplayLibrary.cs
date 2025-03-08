using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace VisbalLogFilter.Libraries
{
    /// <summary>
    /// Library class that handles text display and line number rendering
    /// </summary>
    public class TextDisplayLibrary
    {
        // Events
        public event EventHandler<TextDisplayEventArgs> DisplayCompleted;

        // Private fields
        private readonly Control parentControl;
        private readonly RichTextBox textBox;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Constructor for the TextDisplayLibrary
        /// </summary>
        /// <param name="parentControl">The parent control for UI thread operations</param>
        /// <param name="textBox">The RichTextBox to display text in</param>
        public TextDisplayLibrary(Control parentControl, RichTextBox textBox)
        {
            this.parentControl = parentControl;
            this.textBox = textBox;
        }

        /// <summary>
        /// Display all lines without filtering
        /// </summary>
        /// <param name="lines">The lines to display</param>
        public void DisplayAllLines(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                OnDisplayCompleted("No lines to display");
                return;
            }

            // Cancel any previous operation
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Suspend layout for better performance
            SuspendTextBoxLayout();

            // Clear the text box
            ClearTextBox();

            // For large files, use a background thread
            if (lines.Count > 10000)
            {
                Task.Run(() =>
                {
                    try
                    {
                        const int chunkSize = 5000;
                        for (int i = 0; i < lines.Count; i += chunkSize)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            int count = Math.Min(chunkSize, lines.Count - i);
                            string chunk = string.Join(Environment.NewLine, lines.GetRange(i, count));
                            
                            AppendTextSafe(chunk, i + count < lines.Count);
                            
                            // Report progress
                            int progressPercentage = (int)((double)(i + count) / lines.Count * 100);
                            ReportProgress(progressPercentage, $"Displaying: {Math.Min(i + count, lines.Count):N0} of {lines.Count:N0} lines");
                            
                            // Small delay to keep UI responsive
                            Thread.Sleep(10);
                        }
                        
                        // Complete the operation
                        if (!token.IsCancellationRequested)
                        {
                            ResetCursorPosition();
                            ResumeTextBoxLayout();
                            OnDisplayCompleted($"Displayed all {lines.Count:N0} lines");
                        }
                    }
                    catch (Exception ex)
                    {
                        ResumeTextBoxLayout();
                        OnDisplayCompleted($"Error: {ex.Message}");
                    }
                }, token);
            }
            else
            {
                // For smaller files, set text directly
                SetTextSafe(string.Join(Environment.NewLine, lines));
                ResetCursorPosition();
                ResumeTextBoxLayout();
                OnDisplayCompleted($"Displayed all {lines.Count:N0} lines");
            }
        }

        /// <summary>
        /// Display filtered lines with highlighting
        /// </summary>
        /// <param name="matchingLines">The matching lines with their highlight colors</param>
        /// <param name="totalLineCount">The total number of lines in the original text</param>
        public void DisplayFilteredLines(List<(string Line, Color Color)> matchingLines, int totalLineCount)
        {
            if (matchingLines == null)
            {
                OnDisplayCompleted("No lines to display");
                return;
            }

            // Cancel any previous operation
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Suspend layout for better performance
            SuspendTextBoxLayout();

            // Clear the text box
            ClearTextBox();

            if (matchingLines.Count > 0)
            {
                // For large result sets, use batched processing in a background thread
                if (matchingLines.Count > 5000)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            // First, build the text content
                            StringBuilder sb = new StringBuilder(matchingLines.Count * 100); // Estimate average line length
                            foreach (var item in matchingLines)
                            {
                                sb.AppendLine(item.Line);
                            }
                            
                            string fullText = sb.ToString();
                            
                            // Set the text all at once (much faster)
                            SetTextSafe(fullText);
                            
                            // Now apply colors in batches
                            int currentPos = 0;
                            int totalItems = matchingLines.Count;
                            
                            for (int i = 0; i < totalItems; i += 100) // Process in batches of 100
                            {
                                if (token.IsCancellationRequested)
                                    break;

                                int batchSize = Math.Min(100, totalItems - i);
                                List<(int Start, int Length, Color Color)> colorBatch = new List<(int, int, Color)>(batchSize);
                                
                                for (int j = 0; j < batchSize; j++)
                                {
                                    if (i + j >= totalItems) break;
                                    
                                    var (line, color) = matchingLines[i + j];
                                    int lineLength = line.Length;
                                    
                                    colorBatch.Add((currentPos, lineLength, color));
                                    currentPos += lineLength + Environment.NewLine.Length;
                                }
                                
                                // Apply colors for this batch
                                ApplyColorsSafe(colorBatch);
                                
                                // Report progress
                                int progressPercentage = (int)((double)(i + batchSize) / totalItems * 100);
                                ReportProgress(progressPercentage, $"Applying colors: {Math.Min(i + batchSize, totalItems):N0} of {totalItems:N0} lines");
                                
                                // Small delay to keep UI responsive
                                Thread.Sleep(10);
                            }
                            
                            // Complete the operation
                            if (!token.IsCancellationRequested)
                            {
                                ResetCursorPosition();
                                ResumeTextBoxLayout();
                                
                                // Calculate percentage of lines displayed
                                double percentage = (double)matchingLines.Count / totalLineCount * 100;
                                OnDisplayCompleted($"Displayed {matchingLines.Count:N0} of {totalLineCount:N0} lines ({percentage:F1}%)");
                            }
                        }
                        catch (Exception ex)
                        {
                            ResumeTextBoxLayout();
                            OnDisplayCompleted($"Error: {ex.Message}");
                        }
                    }, token);
                }
                else
                {
                    // For smaller result sets, use the direct approach
                    for (int i = 0; i < matchingLines.Count; i++)
                    {
                        var (line, color) = matchingLines[i];
                        int startIndex = textBox.TextLength;
                        
                        AppendTextDirectly(line);
                        if (i < matchingLines.Count - 1)
                            AppendTextDirectly(Environment.NewLine);
                            
                        ApplyColorDirectly(startIndex, line.Length, color);
                    }
                    
                    ResetCursorPosition();
                    ResumeTextBoxLayout();
                    
                    // Calculate percentage of lines displayed
                    double percentage = (double)matchingLines.Count / totalLineCount * 100;
                    OnDisplayCompleted($"Displayed {matchingLines.Count:N0} of {totalLineCount:N0} lines ({percentage:F1}%)");
                }
            }
            else
            {
                AppendTextDirectly("No matches found.");
                ResetCursorPosition();
                ResumeTextBoxLayout();
                OnDisplayCompleted($"No matches found in {totalLineCount:N0} lines");
            }
        }

        /// <summary>
        /// Display modified lines after replacement
        /// </summary>
        /// <param name="modifiedLines">The modified lines</param>
        /// <param name="replacementsCount">The number of replacements made</param>
        public void DisplayModifiedLines(List<string> modifiedLines, int replacementsCount)
        {
            if (modifiedLines == null || modifiedLines.Count == 0)
            {
                OnDisplayCompleted("No lines to display");
                return;
            }

            // Cancel any previous operation
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Suspend layout for better performance
            SuspendTextBoxLayout();

            // Clear the text box
            ClearTextBox();
            
            // For large files, use a background thread
            if (modifiedLines.Count > 10000)
            {
                Task.Run(() =>
                {
                    try
                    {
                        const int chunkSize = 5000;
                        for (int i = 0; i < modifiedLines.Count; i += chunkSize)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            int count = Math.Min(chunkSize, modifiedLines.Count - i);
                            string chunk = string.Join(Environment.NewLine, modifiedLines.GetRange(i, count));
                            
                            AppendTextSafe(chunk, i + count < modifiedLines.Count);
                            
                            // Report progress
                            int progressPercentage = (int)((double)(i + count) / modifiedLines.Count * 100);
                            ReportProgress(progressPercentage, $"Displaying: {Math.Min(i + count, modifiedLines.Count):N0} of {modifiedLines.Count:N0} lines");
                            
                            // Small delay to keep UI responsive
                            Thread.Sleep(10);
                        }
                        
                        // Complete the operation
                        if (!token.IsCancellationRequested)
                        {
                            ResetCursorPosition();
                            ResumeTextBoxLayout();
                            OnDisplayCompleted($"Replacements applied: {replacementsCount} occurrences replaced");
                        }
                    }
                    catch (Exception ex)
                    {
                        ResumeTextBoxLayout();
                        OnDisplayCompleted($"Error: {ex.Message}");
                    }
                }, token);
            }
            else
            {
                // For smaller files, set text directly
                SetTextSafe(string.Join(Environment.NewLine, modifiedLines));
                ResetCursorPosition();
                ResumeTextBoxLayout();
                OnDisplayCompleted($"Replacements applied: {replacementsCount} occurrences replaced");
            }
        }

        /// <summary>
        /// Cancel any ongoing display operation
        /// </summary>
        public void CancelDisplay()
        {
            cancellationTokenSource?.Cancel();
        }

        #region UI Thread Safe Methods

        private void SuspendTextBoxLayout()
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                textBox.SuspendLayout();
            }));
        }

        private void ResumeTextBoxLayout()
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                textBox.ResumeLayout();
            }));
        }

        private void ClearTextBox()
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                textBox.Clear();
            }));
        }

        private void SetTextSafe(string text)
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                textBox.Text = text;
            }));
        }

        private void AppendTextSafe(string text, bool addNewLine)
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                textBox.AppendText(text);
                if (addNewLine)
                {
                    textBox.AppendText(Environment.NewLine);
                }
            }));
        }

        private void AppendTextDirectly(string text)
        {
            textBox.AppendText(text);
        }

        private void ApplyColorsSafe(List<(int Start, int Length, Color Color)> colorBatch)
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                foreach (var (start, length, color) in colorBatch)
                {
                    textBox.Select(start, length);
                    textBox.SelectionBackColor = color;
                }
            }));
        }

        private void ApplyColorDirectly(int start, int length, Color color)
        {
            textBox.Select(start, length);
            textBox.SelectionBackColor = color;
        }

        private void ResetCursorPosition()
        {
            if (textBox.IsDisposed || !textBox.IsHandleCreated)
                return;

            textBox.Invoke((Action)(() =>
            {
                textBox.Select(0, 0);
            }));
        }

        #endregion

        /// <summary>
        /// Report progress on the UI thread
        /// </summary>
        private void ReportProgress(int percentage, string message)
        {
            if (parentControl.IsDisposed || !parentControl.IsHandleCreated)
                return;

            parentControl.Invoke((Action)(() =>
            {
                // This will be handled by the form
                DisplayProgressReported?.Invoke(this, new ProgressEventArgs(percentage, message));
            }));
        }

        /// <summary>
        /// Trigger the DisplayCompleted event on the UI thread
        /// </summary>
        private void OnDisplayCompleted(string message)
        {
            if (parentControl.IsDisposed || !parentControl.IsHandleCreated)
                return;

            parentControl.Invoke((Action)(() =>
            {
                DisplayCompleted?.Invoke(this, new TextDisplayEventArgs(message));
            }));
        }

        /// <summary>
        /// Event for progress reporting
        /// </summary>
        public event EventHandler<ProgressEventArgs> DisplayProgressReported;
    }

    /// <summary>
    /// Custom RichTextBox with line numbers
    /// </summary>
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

        /// <summary>
        /// Gets or sets whether line numbers are shown
        /// </summary>
        public bool ShowLineNumbers
        {
            get => showLineNumbers;
            set
            {
                if (showLineNumbers != value)
                {
                    showLineNumbers = value;
                    UpdateTextPadding();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Constructor for LineNumberRichTextBox
        /// </summary>
        public LineNumberRichTextBox()
        {
            // Initialize readonly color fields
            LineNumberColor = Color.FromArgb(100, 100, 100);
            GutterBackColor = Color.FromArgb(240, 240, 240);
            SeparatorColor = Color.FromArgb(210, 210, 210);
            
            // Basic settings
            this.Margin = new Padding(0);
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;
            this.Font = new Font("Consolas", 9.75F);
            this.WordWrap = false;
            this.ScrollBars = RichTextBoxScrollBars.Both;
            this.BorderStyle = BorderStyle.None;
            this.DetectUrls = false;
            this.HideSelection = false;
            
            // Set initial padding
            this.SelectionIndent = 5;
            
            // Enable double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw, true);
                    
            // Initialize graphics resources
            InitializeLineNumberResources();
        }

        /// <summary>
        /// Initialize line number resources
        /// </summary>
        private void InitializeLineNumberResources()
        {
            if (lineNumberFormat == null)
            {
                lineNumberFormat = new StringFormat { 
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };
                lineNumberFont = new Font("Consolas", 8.5F);
                lineNumberBrush = new SolidBrush(LineNumberColor);
                gutterBrush = new SolidBrush(GutterBackColor);
                separatorPen = new Pen(SeparatorColor);
            }
        }

        /// <summary>
        /// Update text padding based on line number visibility
        /// </summary>
        private void UpdateTextPadding()
        {
            this.SelectionIndent = showLineNumbers ? LineNumberWidth + 5 : 5;
            if (showLineNumbers && lineNumberFormat == null)
            {
                InitializeLineNumberResources();
            }
            this.Invalidate();
        }

        /// <summary>
        /// Handle window messages
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            
            if (showLineNumbers && m.Msg == WM_PAINT)
            {
                DrawLineNumbers();
            }
        }

        /// <summary>
        /// Draw line numbers
        /// </summary>
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
                
                // For large files, optimize by only drawing every few lines
                int step = isLargeFile ? 5 : 1;
                
                // Draw the line numbers
                for (int i = firstLine; i <= lastLine + 1 && i < Lines.Length; i += step)
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

        /// <summary>
        /// Handle text changed event
        /// </summary>
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            isLargeFile = Lines.Length > 10000;
            if (showLineNumbers)
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Handle vertical scroll event
        /// </summary>
        protected override void OnVScroll(EventArgs e)
        {
            base.OnVScroll(e);
            if (showLineNumbers)
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Handle size changed event
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (showLineNumbers)
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
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

        /// <summary>
        /// Set whether to show line numbers
        /// </summary>
        public void SetShowLineNumbers(bool show)
        {
            ShowLineNumbers = show;
        }
    }

    /// <summary>
    /// Event args for text display
    /// </summary>
    public class TextDisplayEventArgs : EventArgs
    {
        public string Message { get; }

        public TextDisplayEventArgs(string message)
        {
            Message = message;
        }
    }
} 