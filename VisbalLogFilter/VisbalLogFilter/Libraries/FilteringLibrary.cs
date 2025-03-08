using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VisbalLogFilter.Libraries
{
    /// <summary>
    /// Library class that handles all filtering functionality
    /// </summary>
    public class FilteringLibrary
    {
        // Events
        public event EventHandler<FilteringResultEventArgs> FilteringCompleted;

        // Private fields
        private readonly Control parentControl;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Constructor for the FilteringLibrary
        /// </summary>
        /// <param name="parentControl">The parent control for UI thread operations</param>
        public FilteringLibrary(Control parentControl)
        {
            this.parentControl = parentControl;
        }

        /// <summary>
        /// Apply filters to a list of strings
        /// </summary>
        /// <param name="originalLines">The original lines to filter</param>
        /// <param name="filterConditions">The filter conditions to apply</param>
        public void ApplyFilters(List<string> originalLines, List<FilterConditionModel> filterConditions)
        {
            if (originalLines == null || originalLines.Count == 0)
            {
                OnFilteringCompleted(new List<(string Line, Color Color)>(), "No lines to filter");
                return;
            }

            // Cancel any previous operation
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Get enabled filter conditions
            var enabledFilterConditions = filterConditions.Where(c => c.Enabled).ToList();

            // If no filters are active or enabled, show all lines
            if (enabledFilterConditions.Count == 0 || enabledFilterConditions.All(c => string.IsNullOrEmpty(c.FilterText)))
            {
                OnFilteringCompleted(originalLines.Select(line => (line, Color.White)).ToList(), "No active filters");
                return;
            }

            // Prepare filter conditions for faster matching
            var activeFilters = enabledFilterConditions
                .Where(c => !string.IsNullOrEmpty(c.FilterText))
                .Select(c => new {
                    Text = c.FilterText,
                    IsContains = c.FilterType == "CONTAINS",
                    Color = c.HighlightColor
                })
                .ToList();

            // Pre-allocate collection with estimated capacity
            var matchingLines = new List<(string Line, Color Color)>(originalLines.Count / 4);

            // Run the filtering operation in a background task
            Task.Run(() =>
            {
                try
                {
                    int totalLines = originalLines.Count;
                    int processedLines = 0;
                    
                    // Process in chunks for better responsiveness
                    const int processChunkSize = 10000;
                    for (int chunkStart = 0; chunkStart < totalLines; chunkStart += processChunkSize)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        
                        int chunkEnd = Math.Min(chunkStart + processChunkSize, totalLines);
                        
                        // Process this chunk
                        for (int i = chunkStart; i < chunkEnd; i++)
                        {
                            string line = originalLines[i];
                            
                            // Find first matching filter
                            foreach (var filter in activeFilters)
                            {
                                bool isMatch = filter.IsContains
                                    ? line.IndexOf(filter.Text, StringComparison.OrdinalIgnoreCase) >= 0
                                    : line.Equals(filter.Text, StringComparison.OrdinalIgnoreCase);
                                
                                if (isMatch)
                                {
                                    lock (matchingLines)
                                    {
                                        matchingLines.Add((line, filter.Color));
                                    }
                                    break; // Stop checking other filters once we find a match
                                }
                            }
                            
                            processedLines++;
                            
                            // Report progress every 1000 lines
                            if (processedLines % 1000 == 0)
                            {
                                int progressPercentage = (int)((double)processedLines / totalLines * 100);
                                ReportProgress(progressPercentage, $"Processing: {processedLines:N0} of {totalLines:N0} lines ({progressPercentage}%)");
                            }
                        }
                    }
                    
                    // Complete the operation
                    if (!token.IsCancellationRequested)
                    {
                        string message = matchingLines.Count > 0 
                            ? $"Found {matchingLines.Count:N0} matching lines out of {totalLines:N0} ({(double)matchingLines.Count / totalLines * 100:F1}%)"
                            : "No matches found";
                        
                        OnFilteringCompleted(matchingLines, message);
                    }
                }
                catch (Exception ex)
                {
                    OnFilteringCompleted(new List<(string Line, Color Color)>(), $"Error: {ex.Message}");
                }
            }, token);
        }

        /// <summary>
        /// Save filter conditions to a file
        /// </summary>
        /// <param name="filterConditions">The filter conditions to save</param>
        /// <param name="filePath">The file path to save to, or null to prompt for a location</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SaveFilters(List<FilterConditionModel> filterConditions, string filePath = null)
        {
            try
            {
                // Convert to data models for serialization
                var filterDataList = filterConditions.Select(c => new FilterDataModel
                {
                    FilterType = c.FilterType,
                    FilterText = c.FilterText,
                    HighlightColor = ColorTranslator.ToHtml(c.HighlightColor),
                    Enabled = c.Enabled
                }).ToList();

                // Serialize to JSON
                string jsonString = JsonSerializer.Serialize(filterDataList, new JsonSerializerOptions { WriteIndented = true });

                // If no path provided, prompt for one
                if (string.IsNullOrEmpty(filePath))
                {
                    using SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                        Title = "Save Filters",
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
        /// Load filter conditions from a file
        /// </summary>
        /// <param name="filePath">The file path to load from, or null to prompt for a location</param>
        /// <returns>The loaded filter conditions, or null if loading failed</returns>
        public List<FilterDataModel> LoadFilters(string filePath = null)
        {
            try
            {
                // If no path provided, prompt for one
                if (string.IsNullOrEmpty(filePath))
                {
                    using OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                        Title = "Load Filters"
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
                var filterDataList = JsonSerializer.Deserialize<List<FilterDataModel>>(jsonString);
                return filterDataList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Cancel any ongoing filtering operation
        /// </summary>
        public void CancelFiltering()
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
                FilteringProgressReported?.Invoke(this, new ProgressEventArgs(percentage, message));
            }));
        }

        /// <summary>
        /// Trigger the FilteringCompleted event on the UI thread
        /// </summary>
        private void OnFilteringCompleted(List<(string Line, Color Color)> results, string message)
        {
            if (parentControl.IsDisposed || !parentControl.IsHandleCreated)
                return;

            parentControl.Invoke((Action)(() =>
            {
                FilteringCompleted?.Invoke(this, new FilteringResultEventArgs(results, message));
            }));
        }

        /// <summary>
        /// Event for progress reporting
        /// </summary>
        public event EventHandler<ProgressEventArgs> FilteringProgressReported;

        /// <summary>
        /// Reset all filter colors to white
        /// </summary>
        /// <param name="filterConditions">The filter conditions to reset colors for</param>
        /// <returns>The updated filter conditions with white colors</returns>
        public List<FilterConditionModel> ResetAllColors(List<FilterConditionModel> filterConditions)
        {
            if (filterConditions == null || filterConditions.Count == 0)
            {
                return filterConditions;
            }

            // Create a new list to avoid modifying the original
            var updatedConditions = new List<FilterConditionModel>();
            
            foreach (var condition in filterConditions)
            {
                // Create a new condition with the same properties but white color
                updatedConditions.Add(new FilterConditionModel
                {
                    FilterType = condition.FilterType,
                    FilterText = condition.FilterText,
                    HighlightColor = Color.White,
                    Enabled = condition.Enabled
                });
            }
            
            return updatedConditions;
        }
    }

    /// <summary>
    /// Model class for filter conditions
    /// </summary>
    public class FilterConditionModel
    {
        public string FilterType { get; set; } = "CONTAINS";
        public string FilterText { get; set; } = string.Empty;
        public Color HighlightColor { get; set; } = Color.White;
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Model class for filter data (serialization)
    /// </summary>
    public class FilterDataModel
    {
        public string FilterType { get; set; } = "CONTAINS";
        public string FilterText { get; set; } = string.Empty;
        public string HighlightColor { get; set; } = "#FFFFFF";
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Event args for filtering results
    /// </summary>
    public class FilteringResultEventArgs : EventArgs
    {
        public List<(string Line, Color Color)> Results { get; }
        public string Message { get; }

        public FilteringResultEventArgs(List<(string Line, Color Color)> results, string message)
        {
            Results = results;
            Message = message;
        }
    }
} 