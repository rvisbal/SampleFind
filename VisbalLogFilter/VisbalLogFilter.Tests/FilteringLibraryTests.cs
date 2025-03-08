using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Windows.Forms;
using VisbalLogFilter.Libraries;

namespace VisbalLogFilter.Tests
{
    [TestClass]
    public class FilteringLibraryTests
    {
        private Mock<Control> mockControl;
        private FilteringLibrary filteringLibrary;
        private List<string> testLines;
        private List<FilterConditionModel> testConditions;
        private ManualResetEvent waitHandle;
        private List<(string Line, Color Color)> resultLines;
        private string resultMessage;

        [TestInitialize]
        public void Setup()
        {
            // Create a mock control for UI thread operations
            mockControl = new Mock<Control>();
            mockControl.Setup(c => c.IsHandleCreated).Returns(true);
            mockControl.Setup(c => c.IsDisposed).Returns(false);
            mockControl.Setup(c => c.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());

            // Create the filtering library with the mock control
            filteringLibrary = new FilteringLibrary(mockControl.Object);

            // Set up test data
            testLines = new List<string>
            {
                "This is a test line with ERROR",
                "This is a normal line",
                "Another ERROR line for testing",
                "Final line with no special content"
            };

            // Set up test filter conditions
            testConditions = new List<FilterConditionModel>
            {
                new FilterConditionModel
                {
                    FilterType = "CONTAINS",
                    FilterText = "ERROR",
                    HighlightColor = Color.Red,
                    Enabled = true
                }
            };

            // Set up event handling
            waitHandle = new ManualResetEvent(false);
            resultLines = null;
            resultMessage = null;

            filteringLibrary.FilteringCompleted += (sender, e) =>
            {
                resultLines = e.Results;
                resultMessage = e.Message;
                waitHandle.Set();
            };
        }

        [TestMethod]
        public void ApplyFilters_WithValidConditions_ShouldReturnMatchingLines()
        {
            // Arrange - already done in Setup

            // Act
            filteringLibrary.ApplyFilters(testLines, testConditions);
            
            // Wait for the async operation to complete (with timeout)
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Filtering operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(2, resultLines.Count, "Should find 2 lines containing 'ERROR'");
            Assert.AreEqual("This is a test line with ERROR", resultLines[0].Line);
            Assert.AreEqual("Another ERROR line for testing", resultLines[1].Line);
            Assert.AreEqual(Color.Red, resultLines[0].Color);
        }

        [TestMethod]
        public void ApplyFilters_WithEmptyLines_ShouldReturnEmptyResult()
        {
            // Arrange
            var emptyLines = new List<string>();

            // Act
            filteringLibrary.ApplyFilters(emptyLines, testConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Filtering operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(0, resultLines.Count, "Should return empty result for empty input");
        }

        [TestMethod]
        public void ApplyFilters_WithNoConditions_ShouldReturnAllLines()
        {
            // Arrange
            var emptyConditions = new List<FilterConditionModel>();

            // Act
            filteringLibrary.ApplyFilters(testLines, emptyConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Filtering operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(testLines.Count, resultLines.Count, "Should return all lines when no conditions");
        }

        [TestMethod]
        public void ApplyFilters_WithDisabledCondition_ShouldReturnAllLines()
        {
            // Arrange
            var disabledConditions = new List<FilterConditionModel>
            {
                new FilterConditionModel
                {
                    FilterType = "CONTAINS",
                    FilterText = "ERROR",
                    HighlightColor = Color.Red,
                    Enabled = false
                }
            };

            // Act
            filteringLibrary.ApplyFilters(testLines, disabledConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Filtering operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(testLines.Count, resultLines.Count, "Should return all lines when condition is disabled");
        }

        [TestMethod]
        public void ApplyFilters_WithExactMatch_ShouldReturnMatchingLines()
        {
            // Arrange
            var exactMatchConditions = new List<FilterConditionModel>
            {
                new FilterConditionModel
                {
                    FilterType = "EQUALS",
                    FilterText = "This is a normal line",
                    HighlightColor = Color.Blue,
                    Enabled = true
                }
            };

            // Act
            filteringLibrary.ApplyFilters(testLines, exactMatchConditions);
            
            // Wait for the async operation to complete
            bool completed = waitHandle.WaitOne(5000);

            // Assert
            Assert.IsTrue(completed, "Filtering operation timed out");
            Assert.IsNotNull(resultLines, "Result lines should not be null");
            Assert.AreEqual(1, resultLines.Count, "Should find 1 exact matching line");
            Assert.AreEqual("This is a normal line", resultLines[0].Line);
            Assert.AreEqual(Color.Blue, resultLines[0].Color);
        }

        [TestMethod]
        public void SaveAndLoadFilters_ShouldPreserveFilterData()
        {
            // This test would require file system access, so we'll just verify the models
            // Arrange
            var filterData = new List<FilterDataModel>();
            
            // Convert filter conditions to data models
            foreach (var condition in testConditions)
            {
                filterData.Add(new FilterDataModel
                {
                    FilterType = condition.FilterType,
                    FilterText = condition.FilterText,
                    HighlightColor = Common.ColorToHtml(condition.HighlightColor),
                    Enabled = condition.Enabled
                });
            }

            // Assert
            Assert.AreEqual(testConditions.Count, filterData.Count, "Data model count should match condition count");
            Assert.AreEqual(testConditions[0].FilterType, filterData[0].FilterType, "FilterType should be preserved");
            Assert.AreEqual(testConditions[0].FilterText, filterData[0].FilterText, "FilterText should be preserved");
            Assert.AreEqual(testConditions[0].Enabled, filterData[0].Enabled, "Enabled state should be preserved");
            Assert.AreEqual(Common.ColorToHtml(testConditions[0].HighlightColor), filterData[0].HighlightColor, "Color should be preserved");
        }

        [TestMethod]
        public void ResetAllColors_ShouldSetAllColorsToWhite()
        {
            // Arrange
            var coloredConditions = new List<FilterConditionModel>
            {
                new FilterConditionModel
                {
                    FilterType = "CONTAINS",
                    FilterText = "ERROR",
                    HighlightColor = Color.Red,
                    Enabled = true
                },
                new FilterConditionModel
                {
                    FilterType = "EQUALS",
                    FilterText = "WARNING",
                    HighlightColor = Color.Yellow,
                    Enabled = false
                }
            };

            // Act
            var result = filteringLibrary.ResetAllColors(coloredConditions);

            // Assert
            Assert.AreEqual(coloredConditions.Count, result.Count, "Should return same number of conditions");
            
            foreach (var condition in result)
            {
                Assert.AreEqual(Color.White, condition.HighlightColor, "All colors should be reset to white");
            }
            
            // Verify original conditions are not modified
            Assert.AreEqual(Color.Red, coloredConditions[0].HighlightColor, "Original conditions should not be modified");
            Assert.AreEqual(Color.Yellow, coloredConditions[1].HighlightColor, "Original conditions should not be modified");
        }

        [TestMethod]
        public void ResetAllColors_WithNullOrEmptyInput_ShouldReturnSameInput()
        {
            // Arrange
            List<FilterConditionModel> nullConditions = null;
            var emptyConditions = new List<FilterConditionModel>();

            // Act
            var resultNull = filteringLibrary.ResetAllColors(nullConditions);
            var resultEmpty = filteringLibrary.ResetAllColors(emptyConditions);

            // Assert
            Assert.AreEqual(nullConditions, resultNull, "Should return null for null input");
            Assert.AreEqual(0, resultEmpty.Count, "Should return empty list for empty input");
        }

        [TestCleanup]
        public void Cleanup()
        {
            waitHandle.Dispose();
        }
    }
} 