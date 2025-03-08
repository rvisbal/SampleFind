using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VisbalLogFilter.Libraries
{
    /// <summary>
    /// Common utilities and shared classes
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Convert a color to HTML format
        /// </summary>
        /// <param name="color">The color to convert</param>
        /// <returns>HTML color string</returns>
        public static string ColorToHtml(Color color)
        {
            return ColorTranslator.ToHtml(color);
        }

        /// <summary>
        /// Convert an HTML color string to a Color
        /// </summary>
        /// <param name="htmlColor">The HTML color string</param>
        /// <returns>Color object</returns>
        public static Color HtmlToColor(string htmlColor)
        {
            try
            {
                return ColorTranslator.FromHtml(htmlColor);
            }
            catch
            {
                return Color.White;
            }
        }

        /// <summary>
        /// Show a file open dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <returns>Selected file path or null if canceled</returns>
        public static string ShowOpenFileDialog(string title, string filter)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        /// <summary>
        /// Show a file save dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <param name="defaultExt">Default extension</param>
        /// <returns>Selected file path or null if canceled</returns>
        public static string ShowSaveFileDialog(string title, string filter, string defaultExt)
        {
            using SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }

        /// <summary>
        /// Show a progress dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Initial message</param>
        /// <param name="cancelAction">Action to perform when cancel is clicked</param>
        /// <returns>The progress dialog</returns>
        public static Form CreateProgressDialog(string title, string message, Action cancelAction)
        {
            var progressForm = new Form
            {
                Text = title,
                Size = new Size(300, 100),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false
            };
            
            var progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Margin = new Padding(10),
                Height = 20,
                Style = ProgressBarStyle.Continuous
            };
            
            var progressLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = message
            };
            
            var cancelButton = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            
            if (cancelAction != null)
            {
                cancelButton.Click += (s, e) => cancelAction();
            }
            
            progressForm.Controls.Add(progressLabel);
            progressForm.Controls.Add(progressBar);
            progressForm.Controls.Add(cancelButton);
            
            return progressForm;
        }

        /// <summary>
        /// Update a progress dialog
        /// </summary>
        /// <param name="progressForm">The progress form</param>
        /// <param name="percentage">Progress percentage</param>
        /// <param name="message">Progress message</param>
        public static void UpdateProgressDialog(Form progressForm, int percentage, string message)
        {
            if (progressForm.InvokeRequired)
            {
                progressForm.Invoke((Action)(() => UpdateProgressDialog(progressForm, percentage, message)));
                return;
            }

            var progressBar = progressForm.Controls.OfType<ProgressBar>().FirstOrDefault();
            var progressLabel = progressForm.Controls.OfType<Label>().FirstOrDefault();

            if (progressBar != null)
            {
                progressBar.Value = Math.Min(100, Math.Max(0, percentage));
            }

            if (progressLabel != null)
            {
                progressLabel.Text = message;
            }
        }
    }

    /// <summary>
    /// Event args for progress reporting
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public int Percentage { get; }
        public string Message { get; }

        public ProgressEventArgs(int percentage, string message)
        {
            Percentage = percentage;
            Message = message;
        }
    }
} 