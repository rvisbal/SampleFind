using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Windows.Forms;
using VisbalLogFilter.Libraries;

namespace VisbalLogFilter.Tests
{
    [TestClass]
    public class CommonTests
    {
        [TestMethod]
        public void ColorToHtml_ShouldConvertColorCorrectly()
        {
            // Arrange
            Color red = Color.Red;
            Color blue = Color.Blue;
            Color custom = Color.FromArgb(123, 45, 67);

            // Act
            string redHtml = Common.ColorToHtml(red);
            string blueHtml = Common.ColorToHtml(blue);
            string customHtml = Common.ColorToHtml(custom);

            // Assert
            Assert.AreEqual("#FF0000", redHtml);
            Assert.AreEqual("#0000FF", blueHtml);
            Assert.AreEqual("#7B2D43", customHtml);
        }

        [TestMethod]
        public void HtmlToColor_ShouldConvertHtmlCorrectly()
        {
            // Arrange
            string redHtml = "#FF0000";
            string blueHtml = "#0000FF";
            string customHtml = "#7B2D43";

            // Act
            Color red = Common.HtmlToColor(redHtml);
            Color blue = Common.HtmlToColor(blueHtml);
            Color custom = Common.HtmlToColor(customHtml);

            // Assert
            Assert.AreEqual(Color.Red.ToArgb(), red.ToArgb());
            Assert.AreEqual(Color.Blue.ToArgb(), blue.ToArgb());
            Assert.AreEqual(Color.FromArgb(123, 45, 67).ToArgb(), custom.ToArgb());
        }

        [TestMethod]
        public void HtmlToColor_WithInvalidInput_ShouldReturnWhite()
        {
            // Arrange
            string invalidHtml = "not-a-color";

            // Act
            Color result = Common.HtmlToColor(invalidHtml);

            // Assert
            Assert.AreEqual(Color.White.ToArgb(), result.ToArgb());
        }

        [TestMethod]
        public void CreateProgressDialog_ShouldCreateDialogWithCorrectProperties()
        {
            // Arrange
            string title = "Test Progress";
            string message = "Processing...";
            bool cancelCalled = false;
            Action cancelAction = () => { cancelCalled = true; };

            // Act
            using (Form progressDialog = Common.CreateProgressDialog(title, message, cancelAction))
            {
                // Assert
                Assert.AreEqual(title, progressDialog.Text);
                Assert.AreEqual(FormBorderStyle.FixedDialog, progressDialog.FormBorderStyle);
                Assert.AreEqual(FormStartPosition.CenterParent, progressDialog.StartPosition);
                Assert.IsFalse(progressDialog.MaximizeBox);
                Assert.IsFalse(progressDialog.MinimizeBox);
                Assert.IsFalse(progressDialog.ControlBox);

                // Check controls
                var progressBar = progressDialog.Controls.OfType<ProgressBar>().FirstOrDefault();
                var label = progressDialog.Controls.OfType<Label>().FirstOrDefault();
                var button = progressDialog.Controls.OfType<Button>().FirstOrDefault();

                Assert.IsNotNull(progressBar, "Progress bar should be created");
                Assert.IsNotNull(label, "Label should be created");
                Assert.IsNotNull(button, "Button should be created");
                Assert.AreEqual(message, label.Text);
                Assert.AreEqual("Cancel", button.Text);

                // Test cancel action
                button.PerformClick();
                Assert.IsTrue(cancelCalled, "Cancel action should be called");
            }
        }

        [TestMethod]
        public void UpdateProgressDialog_ShouldUpdateProgressBarAndLabel()
        {
            // Arrange
            string title = "Test Progress";
            string message = "Processing...";
            string updatedMessage = "Almost done...";
            int percentage = 75;

            using (Form progressDialog = Common.CreateProgressDialog(title, message, null))
            {
                var progressBar = progressDialog.Controls.OfType<ProgressBar>().FirstOrDefault();
                var label = progressDialog.Controls.OfType<Label>().FirstOrDefault();

                // Act
                Common.UpdateProgressDialog(progressDialog, percentage, updatedMessage);

                // Assert
                Assert.AreEqual(percentage, progressBar.Value);
                Assert.AreEqual(updatedMessage, label.Text);
            }
        }

        [TestMethod]
        public void ProgressEventArgs_ShouldStoreValues()
        {
            // Arrange
            int percentage = 50;
            string message = "Halfway there";

            // Act
            var args = new ProgressEventArgs(percentage, message);

            // Assert
            Assert.AreEqual(percentage, args.Percentage);
            Assert.AreEqual(message, args.Message);
        }
    }
} 