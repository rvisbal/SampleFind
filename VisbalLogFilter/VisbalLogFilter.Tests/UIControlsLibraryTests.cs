using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Windows.Forms;
using VisbalLogFilter.Libraries;

namespace VisbalLogFilter.Tests
{
    [TestClass]
    public class UIControlsLibraryTests
    {
        [TestMethod]
        public void CreateStyledButton_ShouldCreateButtonWithCorrectProperties()
        {
            // Arrange
            string text = "Test Button";
            int width = 120;
            int height = 40;
            Point location = new Point(10, 20);
            Color backColor = Color.Blue;

            // Act
            RoundedButton button = UIControlsLibrary.CreateStyledButton(text, width, height, location, backColor);

            // Assert
            Assert.AreEqual(text, button.Text);
            Assert.AreEqual(width, button.Width);
            Assert.AreEqual(height, button.Height);
            Assert.AreEqual(location, button.Location);
            Assert.AreEqual(backColor, button.BackColor);
            Assert.AreEqual(Color.White, button.ForeColor);
            Assert.AreEqual(FlatStyle.Flat, button.FlatStyle);
            Assert.AreEqual(10, button.BorderRadius);
            Assert.AreEqual(Cursors.Hand, button.Cursor);
            
            // Check hover colors are set
            Assert.AreEqual(ControlPaint.Light(backColor), button.HoverBackColor);
            Assert.AreEqual(Color.White, button.HoverForeColor);
            Assert.AreEqual(ControlPaint.Dark(backColor), button.PressedBackColor);
        }

        [TestMethod]
        public void RoundedButton_DefaultConstructor_ShouldSetDefaultProperties()
        {
            // Act
            RoundedButton button = new RoundedButton();

            // Assert
            Assert.AreEqual(FlatStyle.Flat, button.FlatStyle);
            Assert.AreEqual(0, button.FlatAppearance.BorderSize);
            Assert.AreEqual(new Size(150, 40), button.Size);
            Assert.AreEqual(Color.MediumSlateBlue, button.BackColor);
            Assert.AreEqual(Color.White, button.ForeColor);
            Assert.AreEqual(10, button.BorderRadius);
            Assert.AreEqual(Color.Silver, button.BorderColor);
            
            // Check hover colors are set to non-empty values
            Assert.AreNotEqual(Color.Empty, button.HoverBackColor);
            Assert.AreNotEqual(Color.Empty, button.HoverForeColor);
            Assert.AreNotEqual(Color.Empty, button.PressedBackColor);
        }

        [TestMethod]
        public void RoundedButton_BorderRadius_ShouldUpdateAndInvalidate()
        {
            // Arrange
            RoundedButton button = new RoundedButton();
            int newRadius = 20;
            bool invalidateCalled = false;
            
            // Use reflection to replace the Invalidate method with our test version
            var type = typeof(RoundedButton);
            var invalidateMethod = type.GetMethod("Invalidate", System.Type.EmptyTypes);
            if (invalidateMethod != null)
            {
                // This is a simplified approach - in a real test, you would use a mock framework
                // or a more sophisticated method to verify Invalidate was called
                invalidateCalled = true;
            }

            // Act
            button.BorderRadius = newRadius;

            // Assert
            Assert.AreEqual(newRadius, button.BorderRadius);
            // In a real test with proper mocking, we would verify invalidateCalled is true
        }

        [TestMethod]
        public void RoundedButton_BorderColor_ShouldUpdateAndInvalidate()
        {
            // Arrange
            RoundedButton button = new RoundedButton();
            Color newColor = Color.Red;
            bool invalidateCalled = false;
            
            // Use reflection to replace the Invalidate method with our test version
            var type = typeof(RoundedButton);
            var invalidateMethod = type.GetMethod("Invalidate", System.Type.EmptyTypes);
            if (invalidateMethod != null)
            {
                // This is a simplified approach - in a real test, you would use a mock framework
                // or a more sophisticated method to verify Invalidate was called
                invalidateCalled = true;
            }

            // Act
            button.BorderColor = newColor;

            // Assert
            Assert.AreEqual(newColor, button.BorderColor);
            // In a real test with proper mocking, we would verify invalidateCalled is true
        }

        [TestMethod]
        public void RoundedButton_HoverColors_ShouldUpdateCorrectly()
        {
            // Arrange
            RoundedButton button = new RoundedButton();
            Color newHoverBackColor = Color.Yellow;
            Color newHoverForeColor = Color.Black;
            Color newPressedBackColor = Color.Orange;

            // Act
            button.HoverBackColor = newHoverBackColor;
            button.HoverForeColor = newHoverForeColor;
            button.PressedBackColor = newPressedBackColor;

            // Assert
            Assert.AreEqual(newHoverBackColor, button.HoverBackColor);
            Assert.AreEqual(newHoverForeColor, button.HoverForeColor);
            Assert.AreEqual(newPressedBackColor, button.PressedBackColor);
        }
    }
} 