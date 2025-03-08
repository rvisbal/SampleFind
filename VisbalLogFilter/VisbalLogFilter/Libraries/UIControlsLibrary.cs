using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VisbalLogFilter.Libraries
{
    /// <summary>
    /// Library class that contains custom UI controls
    /// </summary>
    public static class UIControlsLibrary
    {
        /// <summary>
        /// Create a styled rounded button
        /// </summary>
        /// <param name="text">Button text</param>
        /// <param name="width">Button width</param>
        /// <param name="height">Button height</param>
        /// <param name="location">Button location</param>
        /// <param name="backColor">Button background color</param>
        /// <returns>A styled rounded button</returns>
        public static RoundedButton CreateStyledButton(string text, int width, int height, Point location, Color backColor)
        {
            var button = new RoundedButton
            {
                Text = text,
                Size = new Size(width, height),
                Location = location,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                BorderRadius = 10,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };

            // Set hover colors
            button.HoverBackColor = ControlPaint.Light(backColor);
            button.HoverForeColor = Color.White;
            button.PressedBackColor = ControlPaint.Dark(backColor);

            return button;
        }
    }

    /// <summary>
    /// Custom rounded button control
    /// </summary>
    public class RoundedButton : Button
    {
        private int borderRadius = 10;
        private Color borderColor = Color.Silver;
        private Color hoverBackColor;
        private Color hoverForeColor;
        private Color pressedBackColor;
        private bool isHovering = false;
        private bool isPressed = false;
        private Color defaultBackColor;

        /// <summary>
        /// Gets or sets the border radius
        /// </summary>
        [Category("Appearance")]
        public int BorderRadius
        {
            get { return borderRadius; }
            set 
            { 
                borderRadius = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the border color
        /// </summary>
        [Category("Appearance")]
        public Color BorderColor
        {
            get { return borderColor; }
            set 
            { 
                borderColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the hover background color
        /// </summary>
        [Category("Appearance")]
        public Color HoverBackColor
        {
            get { return hoverBackColor; }
            set { hoverBackColor = value; }
        }

        /// <summary>
        /// Gets or sets the hover foreground color
        /// </summary>
        [Category("Appearance")]
        public Color HoverForeColor
        {
            get { return hoverForeColor; }
            set { hoverForeColor = value; }
        }

        /// <summary>
        /// Gets or sets the pressed background color
        /// </summary>
        [Category("Appearance")]
        public Color PressedBackColor
        {
            get { return pressedBackColor; }
            set { pressedBackColor = value; }
        }

        /// <summary>
        /// Constructor for RoundedButton
        /// </summary>
        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(150, 40);
            BackColor = Color.MediumSlateBlue;
            ForeColor = Color.White;
            defaultBackColor = BackColor;
            
            // Set default hover colors
            hoverBackColor = ControlPaint.Light(BackColor);
            hoverForeColor = ForeColor;
            pressedBackColor = ControlPaint.Dark(BackColor);
            
            // Redraw when resized
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        /// <summary>
        /// Paint the button
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Rectangle rectSurface = this.ClientRectangle;
            Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);
            int smoothSize = 2;
            
            if (borderRadius > 2) // Rounded button
            {
                using (GraphicsPath pathSurface = GetFigurePath(rectSurface, borderRadius))
                using (GraphicsPath pathBorder = GetFigurePath(rectBorder, borderRadius - 1))
                using (Pen penSurface = new Pen(this.Parent.BackColor, smoothSize))
                using (Pen penBorder = new Pen(borderColor, 1))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    
                    // Button surface
                    this.Region = new Region(pathSurface);
                    
                    // Draw surface border for HD result
                    e.Graphics.DrawPath(penSurface, pathSurface);
                    
                    // Button border
                    if (borderColor != Color.Transparent)
                        e.Graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else // Normal button
            {
                e.Graphics.SmoothingMode = SmoothingMode.None;
                
                // Button surface
                this.Region = new Region(rectSurface);
                
                // Button border
                if (borderColor != Color.Transparent)
                    using (Pen penBorder = new Pen(borderColor, 1))
                        e.Graphics.DrawRectangle(penBorder, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        /// <summary>
        /// Get the figure path for the button
        /// </summary>
        private GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Handle mouse enter event
        /// </summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovering = true;
            if (hoverBackColor != Color.Empty)
            {
                defaultBackColor = BackColor;
                BackColor = hoverBackColor;
            }
            if (hoverForeColor != Color.Empty)
            {
                ForeColor = hoverForeColor;
            }
        }

        /// <summary>
        /// Handle mouse leave event
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovering = false;
            if (!isPressed)
            {
                BackColor = defaultBackColor;
                ForeColor = ForeColor;
            }
        }

        /// <summary>
        /// Handle mouse down event
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            isPressed = true;
            if (pressedBackColor != Color.Empty)
            {
                BackColor = pressedBackColor;
            }
        }

        /// <summary>
        /// Handle mouse up event
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPressed = false;
            if (isHovering)
            {
                if (hoverBackColor != Color.Empty)
                {
                    BackColor = hoverBackColor;
                }
            }
            else
            {
                BackColor = defaultBackColor;
            }
        }
    }
} 