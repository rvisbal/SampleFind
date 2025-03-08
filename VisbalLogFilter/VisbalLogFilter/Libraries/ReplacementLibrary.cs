using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VisbalLogFilter.Libraries
{
    /// <summary>
    /// Library class that handles all text replacement functionality
    /// </summary>
    public class ReplacementLibrary
    {
        // Events
        public event EventHandler<ReplacementResultEventArgs> ReplacementCompleted;

        // Private fields
        private readonly Control parentControl;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Constructor for the ReplacementLibrary
        /// </summary>
        /// <param name="parentControl">The parent control for UI thread operations</param>
        public ReplacementLibrary(Control parentControl)
        {
            this.parentControl = parentControl;
        }

        /// <summary>
        /// Apply replacements to a list of strings
        /// </summary>
        /// <param name="originalLines">The original lines to process</param>
        /// <param name="replaceConditions">The replacement conditions to apply</param>
        public void ApplyReplacements(List<string> originalLines, List<ReplaceConditionModel> replaceConditions)
        {
            if (originalLines == null || originalLines.Count == 0)
            {
                OnReplacementCompleted(new List<string>(), "No lines to process");
                return;
            }

            // Cancel any previous operation
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Get enabled replacement conditions
            var enabledReplaceConditions = replaceConditions.Where(c => c.Enabled).ToList();

            if (enabledReplaceConditions.Count == 0)
            {
                OnReplacementCompleted(originalLines, "No active replacements");
                return;
            }

            // Run the replacement operation in a background task
            Task.Run(() =>
            {
                try
                {
                    // Create a copy of the original lines to work with
                    List<string> modifiedLines = new List<string>(originalLines);
                    int replacementsCount = 0;
                    int totalLines = originalLines.Count;
                    
                    // Apply each replacement
                    foreach (var condition in enabledReplaceConditions)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        
                        string findText = condition.FindText;
                        string replaceText = condition.ReplaceText;

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
                                
                                // Report progress every 1000 lines
                                if (i % 1000 == 0)
                                {
                                    int progressPercentage = (int)((double)i / totalLines * 100);
                                    ReportProgress(progressPercentage, $"Processing: {i:N0} of {totalLines:N0} lines ({progressPercentage}%)");
                                }
                            }
                        }
                    }
                    
                    // Complete the operation
                    if (!token.IsCancellationRequested)
                    {
                        string message = replacementsCount > 0 
                            ? $"Applied {replacementsCount} replacements across {totalLines:N0} lines"
                            : "No replacements were made";
                        
                        OnReplacementCompleted(modifiedLines, message);
                    }
                }
                catch (Exception ex)
                {
                    OnReplacementCompleted(originalLines, $"Error: {ex.Message}");
                }
            }, token);
        }

        /// <summary>
        /// Save replacement conditions to a file
        /// </summary>
        /// <param name="replaceConditions">The replacement conditions to save</param>
        /// <param name="filePath">The file path to save to, or null to prompt for a location</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SaveReplacements(List<ReplaceConditionModel> replaceConditions, string filePath = null)
        {
            try
            {
                // Convert to data models for serialization
                var replaceDataList = replaceConditions.Select(c => new ReplaceDataModel
                {
                    FindText = c.FindText,
                    ReplaceText = c.ReplaceText,
                    Enabled = c.Enabled
                }).ToList();

                // Serialize to JSON
                string jsonString = JsonSerializer.Serialize(replaceDataList, new JsonSerializerOptions { WriteIndented = true });

                // If no path provided, prompt for one
                if (string.IsNullOrEmpty(filePath))
                {
                    using SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                        Title = "Save Replacements",
                        DefaultExt = "json"
                    };

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return false;
                    }

                    filePath = saveFileDialog.FileName;
                }

                // Write to file
                File.WriteAllText(filePath, jsonString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Load replacement conditions from a file
        /// </summary>
        /// <param name="filePath">The file path to load from, or null to prompt for a location</param>
        /// <returns>The loaded replacement conditions, or null if loading failed</returns>
        public List<ReplaceDataModel> LoadReplacements(string filePath = null)
        {
            try
            {
                // If no path provided, prompt for one
                if (string.IsNullOrEmpty(filePath))
                {
                    using OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                        Title = "Load Replacements"
                    };

                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return null;
                    }

                    filePath = openFileDialog.FileName;
                }

                // Read from file
                string jsonString = File.ReadAllText(filePath);

                // Deserialize from JSON
                var replaceDataList = JsonSerializer.Deserialize<List<ReplaceDataModel>>(jsonString);
                return replaceDataList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Cancel any ongoing replacement operation
        /// </summary>
        public void CancelReplacement()
        {
            cancellationTokenSource?.Cancel();
        }

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
                ReplacementProgressReported?.Invoke(this, new ProgressEventArgs(percentage, message));
            }));
        }

        /// <summary>
        /// Trigger the ReplacementCompleted event on the UI thread
        /// </summary>
        private void OnReplacementCompleted(List<string> results, string message)
        {
            if (parentControl.IsDisposed || !parentControl.IsHandleCreated)
                return;

            parentControl.Invoke((Action)(() =>
            {
                ReplacementCompleted?.Invoke(this, new ReplacementResultEventArgs(results, message));
            }));
        }

        /// <summary>
        /// Event for progress reporting
        /// </summary>
        public event EventHandler<ProgressEventArgs> ReplacementProgressReported;
    }

    /// <summary>
    /// Model class for replacement conditions
    /// </summary>
    public class ReplaceConditionModel
    {
        public string FindText { get; set; } = string.Empty;
        public string ReplaceText { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Model class for replacement data (serialization)
    /// </summary>
    public class ReplaceDataModel
    {
        public string FindText { get; set; } = string.Empty;
        public string ReplaceText { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Event args for replacement results
    /// </summary>
    public class ReplacementResultEventArgs : EventArgs
    {
        public List<string> Results { get; }
        public string Message { get; }

        public ReplacementResultEventArgs(List<string> results, string message)
        {
            Results = results;
            Message = message;
        }
    }
} 