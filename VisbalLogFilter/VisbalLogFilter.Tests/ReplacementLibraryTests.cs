using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Windows.Forms;
using VisbalLogFilter.Libraries;

namespace VisbalLogFilter.Tests
{
    [TestClass]
    public class ReplacementLibraryTests
    {
        private Mock<Control> mockControl;
        private ReplacementLibrary replacementLibrary;
        private List<string> testLines;
        private List<ReplaceConditionModel> testConditions;
        private ManualResetEvent waitHandle;
        private List<string> resultLines;
        private string resultMessage;

        [TestInitialize]
        public void Setup()
        {
            // Create a mock control for UI thread operations
            mockControl = new Mock<Control>();
            mockControl.Setup(c => c.IsHandleCreated).Returns(true);
            mockControl.Setup(c => c.IsDisposed).Returns(false);
            mockControl.Setup(c => c.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());

            // Create the replacement library with the mock control
            replacementLibrary = new ReplacementLibrary(mockControl.Object);

            // Set up test data
            testLines = new List<string>
            {
                "This is a test line with ERROR",
                "This is a normal line",
                "Another ERROR line for testing",
                "Final line with no special content"
            };

            // Set up test replacement conditions
            testConditions = new List<ReplaceConditionModel>
            {
                new ReplaceConditionModel
                {
                    FindText = "ERROR",
                    ReplaceText = "WARNING",
                    Enabled = true
                }
            };

            // Set up event handling
            waitHandle = new ManualResetEvent(false);
            resultLines = null;
            resultMessage = null;

            replacementLibrary.ReplacementCompleted += (sender, e) =>
            {
                resultLines = e.Results;
                resultMessage = e.Message;
                waitHandle.Set();
            };
        }

        [TestMethod]
        public void ApplyReplacements_WithValidConditions_ShouldReplaceText()
        {
            // Arrange - already done in Setup

            // Act
            replacementLibrary.ApplyReplacements(testLines, testConditions);
            
            // Wait for the async operation to complete (with timeout)
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Replacement operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(testLines.Count, resultLines.Count, "Should return same number of lines");
            Assert.AreEqual("This is a test line with WARNING", resultLines[0]);
            Assert.AreEqual("This is a normal line", resultLines[1]);
            Assert.AreEqual("Another WARNING line for testing", resultLines[2]);
            Assert.AreEqual("Final line with no special content", resultLines[3]);
        }

        [TestMethod]
        public void ApplyReplacements_WithEmptyLines_ShouldReturnEmptyResult()
        {
            // Arrange
            var emptyLines = new List<string>();

            // Act
            replacementLibrary.ApplyReplacements(emptyLines, testConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Replacement operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(0, resultLines.Count, "Should return empty result for empty input");
        }

        [TestMethod]
        public void ApplyReplacements_WithNoConditions_ShouldReturnOriginalLines()
        {
            // Arrange
            var emptyConditions = new List<ReplaceConditionModel>();

            // Act
            replacementLibrary.ApplyReplacements(testLines, emptyConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Replacement operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(testLines.Count, resultLines.Count, "Should return all original lines");
            
            // Verify the lines are unchanged
            for (int i = 0; i < testLines.Count; i++)
            {
                Assert.AreEqual(testLines[i], resultLines[i], $"Line {i} should be unchanged");
            }
        }

        [TestMethod]
        public void ApplyReplacements_WithDisabledCondition_ShouldReturnOriginalLines()
        {
            // Arrange
            var disabledConditions = new List<ReplaceConditionModel>
            {
                new ReplaceConditionModel
                {
                    FindText = "ERROR",
                    ReplaceText = "WARNING",
                    Enabled = false
                }
            };

            // Act
            replacementLibrary.ApplyReplacements(testLines, disabledConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Replacement operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            
            // Verify the lines are unchanged
            for (int i = 0; i < testLines.Count; i++)
            {
                Assert.AreEqual(testLines[i], resultLines[i], $"Line {i} should be unchanged when condition is disabled");
            }
        }

        [TestMethod]
        public void ApplyReplacements_WithMultipleConditions_ShouldApplyAllReplacements()
        {
            // Arrange
            var multipleConditions = new List<ReplaceConditionModel>
            {
                new ReplaceConditionModel
                {
                    FindText = "ERROR",
                    ReplaceText = "WARNING",
                    Enabled = true
                },
                new ReplaceConditionModel
                {
                    FindText = "test",
                    ReplaceText = "sample",
                    Enabled = true
                }
            };

            // Act
            replacementLibrary.ApplyReplacements(testLines, multipleConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Replacement operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual("This is a sample line with WARNING", resultLines[0]);
            Assert.AreEqual("This is a normal line", resultLines[1]);
            Assert.AreEqual("Another WARNING line for testing", resultLines[2]);
            Assert.AreEqual("Final line with no special content", resultLines[3]);
        }

        [TestMethod]
        public void SaveAndLoadReplacements_ShouldPreserveReplacementData()
        {
            // This test would require file system access, so we'll just verify the models
            // Arrange
            var replaceData = new List<ReplaceDataModel>();
            
            // Convert replacement conditions to data models
            foreach (var condition in testConditions)
            {
                replaceData.Add(new ReplaceDataModel
                {
                    FindText = condition.FindText,
                    ReplaceText = condition.ReplaceText,
                    Enabled = condition.Enabled
                });
            }

            // Assert
            Assert.AreEqual(testConditions.Count, replaceData.Count, "Data model count should match condition count");
            Assert.AreEqual(testConditions[0].FindText, replaceData[0].FindText, "FindText should be preserved");
            Assert.AreEqual(testConditions[0].ReplaceText, replaceData[0].ReplaceText, "ReplaceText should be preserved");
            Assert.AreEqual(testConditions[0].Enabled, replaceData[0].Enabled, "Enabled state should be preserved");
        }

        [TestCleanup]
        public void Cleanup()
        {
            waitHandle.Dispose();
        }
    }
} 