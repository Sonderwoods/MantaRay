using Eto.Drawing;
using Eto.Forms;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Components.Controls
{
    /// <summary>
    /// A custom button. Based on https://gist.github.com/cwensley/95000998e37acd93e830
    /// </summary>
    public class RoundedButton : Drawable
    {

        public string Text { get; set; }
        public int FontSize { get; set; }
        public int CornerRadius { get; set; }
        public int BorderThickness { get; set; }


        public Color BorderColor { get; set; }
        public Color BorderColorSelected { get; set; }
        public Color BorderColorDisabled { get; set; }
        public Color BorderColorDown { get; set; }


        public new Color BackgroundColor { get; set; }
        public Color BackgroundColorSelected { get; set; }
        public Color BackgroundColorDown { get; set; }
        public Color BackgroundColorDisabled { get; set; }
        public Color TextColor { get; set; }
        public Color TextColorSelected { get; set; }
        public Color TextColorDisabled { get; set; }
        public Color TextColorDown { get; set; }

        public FormattedTextAlignment HorizontalAlignment {get;set;}


        bool pressed;
        bool hover;
        bool mouseDown;

        public RoundedButton() : base()
        {
            base.BackgroundColor = Color.FromGrayscale(0, 0); //transparent

            BackgroundColor = Color.FromGrayscale(0.8f);
            BackgroundColorSelected = Color.FromGrayscale(0.9f);
            BackgroundColorDisabled = Color.FromGrayscale(0.7f, 0.7f);
            BackgroundColorDown = Color.FromGrayscale(0.5f, 0.3f);


            BorderColor = Color.FromGrayscale(0.95f);
            BorderColorSelected = Color.FromGrayscale(1.0f);
            BorderColorDisabled = Color.FromGrayscale(0.73f, 0.8f);
            BorderColorDown = Color.FromGrayscale(0.95f);

            TextColor = Color.FromGrayscale(0.2f);
            TextColorSelected = Color.FromGrayscale(0.4f);
            TextColorDisabled = Color.FromGrayscale(0.7f);
            TextColorDown = Color.FromGrayscale(0.1f);

            //TextColor = TextColorSelected = TextColorDisabled = TextColorDown = Colors.Red;

            HorizontalAlignment = FormattedTextAlignment.Center;

            CornerRadius = 5;
            BorderThickness = 2;

            FontSize = 10;

            ToggleMode = ToggleModes.Press;

            Enabled = true;
        }

        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                if (base.Enabled != value)
                {
                    base.Enabled = value;
                    if (Loaded)
                        Invalidate();
                }
            }
        }

        public bool Pressed
        {
            get { return pressed; }
            set
            {
                if (pressed != value)
                {
                    pressed = value;
                    mouseDown = false;
                    if (Loaded)
                        Invalidate();
                }
            }
        }

        public ToggleModes ToggleMode { get; set; }

        public enum ToggleModes
        {
            Press,
            Toggle
        }

        public bool Persistent { get; set; }

        public event EventHandler<MouseEventArgs> Click;

        protected virtual void OnClick(MouseEventArgs e)
        {
            if (e.Buttons == MouseButtons.Primary)
                Click?.Invoke(this, e);
        }

        

        

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Loaded)
                Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            
            if (Enabled && e.Buttons == MouseButtons.Primary)
            {
                mouseDown = true;
                Invalidate();
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            hover = false;
  
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseDown = false;

            if (hover)
             {
                if (ToggleMode == ToggleModes.Toggle)
                    pressed = !pressed;
                else if (Persistent)
                    pressed = true;
                else
                    pressed = false;

                mouseDown = false;

                Invalidate();

                if (Enabled && e.Buttons == MouseButtons.Primary)
                {
                    OnClick(e);
                    return;
                }
            }
            else
            {
                mouseDown = false;
                Invalidate();
            }

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {

            // For some odd reason we have to reduce the rectangle size a bit
            var rectBorder = new Rectangle(new Size(this.Size.Width - BorderThickness, this.Size.Height - BorderThickness));
            rectBorder.Left = (int)(BorderThickness * 0.5f);
            rectBorder.Top = (int)(BorderThickness * 0.5f);
            var roundPathBorder = GraphicsPath.GetRoundRect(rectBorder, CornerRadius);

            // FILL
            var backgroundColor = Enabled ? (mouseDown ? BackgroundColorDown : (hover ? BackgroundColorSelected : BackgroundColor)) : BackgroundColorDisabled;
            Brush backgroundBrush = new SolidBrush(backgroundColor);
            var rectFill = new Rectangle(new Size(this.Size.Width - BorderThickness * 2, this.Size.Height - BorderThickness*2));
            var roundPathFill = GraphicsPath.GetRoundRect(rectFill, CornerRadius);
            pe.Graphics.FillPath(backgroundBrush, roundPathBorder);


            // BORDER
            var borderCol = Enabled ? (mouseDown ? BorderColorDown : (hover ? BorderColorSelected : BorderColor)) : BorderColorDisabled;
            Pen pen = new Pen(new SolidBrush(borderCol), BorderThickness);
            pe.Graphics.DrawPath(pen, roundPathBorder);

            

            // TEXT
            var textColor = Enabled ? (mouseDown ? TextColorDown : (hover ? TextColorSelected : TextColor)) : TextColorDisabled;
            var fontFamily = new FontFamily("Montserrat") ?? new FontFamily("Times New Roman");
            Brush textBrush = new SolidBrush(textColor);
            //RectangleF rectf = new RectangleF(rectBorder.X + 2, rectBorder.Y + 2, rectBorder.Width - 4, rectBorder.Height - 4);
            pe.Graphics.DrawText(new Font(fontFamily, FontSize), textBrush, rectBorder, Text, alignment: FormattedTextAlignment.Center);
            //pe.Graphics.DrawText(new Font(fontFamily, FontSize), textBrush, new Eto.Drawing.Point(this.Location.X+2, this.Location.Y+2), Text);

            //pe.Graphics.FillRectangle(bgCol, rect);
            //pe.Graphics.DrawInsetRectangle(Colors.Gray, Colors.White, rect);



            //Rectangle rect2 = new Rectangle(rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10);
            //Pen pen = new Pen(bgBrush, 10);
            //pe.Graphics.DrawRectangle(pen, rect2);
            //pe.Graphics.DrawPath(Colors.Green, GraphicsPath.GetRoundRect(rect2, 10));
            //FormattedText t = new FormattedText()
            //{
            //    Text = "hi",
            //    Font = new Font(fontFamily, 12)

            //};


            base.OnPaint(pe);
        }
    }
}

