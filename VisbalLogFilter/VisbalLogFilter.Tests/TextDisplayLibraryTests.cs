using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Windows.Forms;
using VisbalLogFilter.Libraries;

namespace VisbalLogFilter.Tests
{
    [TestClass]
    public class TextDisplayLibraryTests
    {
        private Mock<Control> mockControl;
        private Mock<RichTextBox> mockTextBox;
        private TextDisplayLibrary textDisplayLibrary;
        private List<string> testLines;
        private List<(string Line, Color Color)> testColoredLines;
        private ManualResetEvent waitHandle;
        private string resultMessage;

        [TestInitialize]
        public void Setup()
        {
            // Create a mock control for UI thread operations
            mockControl = new Mock<Control>();
            mockControl.Setup(c => c.IsHandleCreated).Returns(true);
            mockControl.Setup(c => c.IsDisposed).Returns(false);
            mockControl.Setup(c => c.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());

            // Create a mock RichTextBox
            mockTextBox = new Mock<RichTextBox>();
            mockTextBox.Setup(t => t.IsHandleCreated).Returns(true);
            mockTextBox.Setup(t => t.IsDisposed).Returns(false);
            mockTextBox.Setup(t => t.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());

            // Create the text display library with the mock controls
            textDisplayLibrary = new TextDisplayLibrary(mockControl.Object, mockTextBox.Object);

            // Set up test data
            testLines = new List<string>
            {
                "This is a test line with ERROR",
                "This is a normal line",
                "Another ERROR line for testing",
                "Final line with no special content"
            };

            // Set up test colored lines
            testColoredLines = new List<(string Line, Color Color)>
            {
                ("This is a test line with ERROR", Color.Red),
                ("Another ERROR line for testing", Color.Red)
            };

            // Set up event handling
            waitHandle = new ManualResetEvent(false);
            resultMessage = null;

            textDisplayLibrary.DisplayCompleted += (sender, e) =>
            {
                resultMessage = e.Message;
                waitHandle.Set();
            };
        }

        [TestMethod]
        public void DisplayAllLines_ShouldCallTextBoxMethods()
        {
            // Arrange - already done in Setup

            // Set up property setter verification
            string capturedText = null;
            mockTextBox.SetupSet(t => t.Text = It.IsAny<string>())
                .Callback<string>(value => capturedText = value);

            // Act
            textDisplayLibrary.DisplayAllLines(testLines);
            
            // Wait for the async operation to complete (with timeout)
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Display operation timed out");
            mockTextBox.Verify(t => t.SuspendLayout(), Times.AtLeastOnce);
            mockTextBox.Verify(t => t.Clear(), Times.AtLeastOnce);
            
            // For small test data, it should set text directly
            mockTextBox.VerifySet(t => t.Text = It.IsAny<string>(), Times.Once);
            mockTextBox.Verify(t => t.Select(0, 0), Times.Once);
            mockTextBox.Verify(t => t.ResumeLayout(), Times.Once);
            
            // Verify text was set (optional)
            Assert.IsNotNull(capturedText, "Text should have been set");
        }

        [TestMethod]
        public void DisplayAllLines_WithEmptyLines_ShouldHandleGracefully()
        {
            // Arrange
            var emptyLines = new List<string>();

            // Act
            textDisplayLibrary.DisplayAllLines(emptyLines);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Display operation timed out");
            Assert.IsNotNull(resultMessage, "Result message should not be null");
            Assert.IsTrue(resultMessage.Contains("No lines"), "Message should indicate no lines");
        }

        [TestMethod]
        public void DisplayFilteredLines_ShouldCallTextBoxMethods()
        {
            // Arrange - already done in Setup

            // Set up property setter verification
            Color capturedColor = Color.Empty;
            mockTextBox.SetupSet(t => t.SelectionBackColor = It.IsAny<Color>())
                .Callback<Color>(value => capturedColor = value);

            // Act
            textDisplayLibrary.DisplayFilteredLines(testColoredLines, testLines.Count);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Display operation timed out");
            mockTextBox.Verify(t => t.SuspendLayout(), Times.AtLeastOnce);
            mockTextBox.Verify(t => t.Clear(), Times.AtLeastOnce);
            
            // For small test data, it should append text directly
            mockTextBox.Verify(t => t.AppendText(It.IsAny<string>()), Times.AtLeast(2));
            mockTextBox.Verify(t => t.Select(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeast(3)); // 2 for colors + 1 for reset
            mockTextBox.VerifySet(t => t.SelectionBackColor = It.IsAny<Color>(), Times.Exactly(2));
            mockTextBox.Verify(t => t.ResumeLayout(), Times.Once);
            
            // Verify color was set (optional)
            Assert.AreNotEqual(Color.Empty, capturedColor, "Color should have been set");
        }

        [TestMethod]
        public void DisplayFilteredLines_WithEmptyLines_ShouldHandleGracefully()
        {
            // Arrange
            var emptyLines = new List<(string Line, Color Color)>();

            // Act
            textDisplayLibrary.DisplayFilteredLines(emptyLines, testLines.Count);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Display operation timed out");
            Assert.IsNotNull(resultMessage, "Result message should not be null");
            Assert.IsTrue(resultMessage.Contains("No matches"), "Message should indicate no matches");
            mockTextBox.Verify(t => t.AppendText("No matches found."), Times.Once);
        }

        [TestMethod]
        public void DisplayModifiedLines_ShouldCallTextBoxMethods()
        {
            // Arrange
            var modifiedLines = new List<string>
            {
                "This is a test line with WARNING",
                "This is a normal line",
                "Another WARNING line for testing",
                "Final line with no special content"
            };

            // Set up property setter verification
            string capturedText = null;
            mockTextBox.SetupSet(t => t.Text = It.IsAny<string>())
                .Callback<string>(value => capturedText = value);

            // Act
            textDisplayLibrary.DisplayModifiedLines(modifiedLines, 2);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Display operation timed out");
            mockTextBox.Verify(t => t.SuspendLayout(), Times.AtLeastOnce);
            mockTextBox.Verify(t => t.Clear(), Times.AtLeastOnce);
            
            // For small test data, it should set text directly
            mockTextBox.VerifySet(t => t.Text = It.IsAny<string>(), Times.Once);
            mockTextBox.Verify(t => t.Select(0, 0), Times.Once);
            mockTextBox.Verify(t => t.ResumeLayout(), Times.Once);
            
            // Check message contains replacement count
            Assert.IsTrue(resultMessage.Contains("2"), "Message should include replacement count");
            
            // Verify text was set (optional)
            Assert.IsNotNull(capturedText, "Text should have been set");
        }

        [TestMethod]
        public void DisplayModifiedLines_WithEmptyLines_ShouldHandleGracefully()
        {
            // Arrange
            var emptyLines = new List<string>();

            // Act
            textDisplayLibrary.DisplayModifiedLines(emptyLines, 0);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Display operation timed out");
            Assert.IsNotNull(resultMessage, "Result message should not be null");
            Assert.IsTrue(resultMessage.Contains("No lines"), "Message should indicate no lines");
        }

        [TestMethod]
        public void CancelDisplay_ShouldCancelOperation()
        {
            // This is a simple test to ensure the method doesn't throw exceptions
            
            // Act & Assert - should not throw
            textDisplayLibrary.CancelDisplay();
        }

        [TestCleanup]
        public void Cleanup()
        {
            waitHandle.Dispose();
        }
    }
} 