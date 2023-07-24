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
    public class CustomButton : Drawable
    {
        bool pressed;
        bool hover;
        bool mouseDown;

        public static Color DisabledColor = Color.FromGrayscale(0.4f, 0.3f);
        public static Color EnabledColor = Colors.Black;

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

        public Color DrawColor
        {
            get { return Enabled ? EnabledColor : DisabledColor; }
        }

        public bool Toggle { get; set; }

        public bool Persistent { get; set; }

        public event EventHandler<MouseEventArgs> Click;

        protected virtual void OnClick(MouseEventArgs e)
        {
            if (e.Buttons == MouseButtons.Primary)
                Click?.Invoke(this, e);
        }

        

        public CustomButton()
        {
            Enabled = true;
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

            var rect = new Rectangle(this.Location.X, this.Location.Y, this.Size.Width, this.Size.Height);

            if (mouseDown && hover/* && rect.Contains((Eto.Drawing.Point)e.Location)*/)
            {
                if (Toggle)
                    pressed = !pressed;
                else if (Persistent)
                    pressed = true;
                else
                    pressed = false;
                mouseDown = false;

                this.Invalidate();

                if (Enabled && e.Buttons == MouseButtons.Primary)
                {
                    OnClick(e);
                    return;
                }
            }
            else
            {
                mouseDown = false;
                this.Invalidate();
            }

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            var rect = new Rectangle(new Size(this.Size.Width - 2, this.Size.Height - 2));
            var roundPath = GraphicsPath.GetRoundRect(rect, 10);



            var bgCol = Color.FromGrayscale(Enabled ? (mouseDown ? 0.5f : (hover ? 0.75f : 0.8f)) : 0.6f);
            var borderCol = Color.FromGrayscale(hover && Enabled ? 0.95f : 0.87f);

            Brush bgBrush = new SolidBrush(bgCol);
            Brush borderBrush = new SolidBrush(borderCol);

            var fontFamily = new FontFamily("Montserrat") ?? new FontFamily("Times New Roman");

            pe.Graphics.FillPath(bgBrush, roundPath);

            pe.Graphics.DrawPath(borderCol, roundPath);

            RectangleF rectf = new RectangleF(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);

            pe.Graphics.DrawText(new Font(fontFamily, 12), borderBrush, rectf, "hrello", alignment: FormattedTextAlignment.Center);

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

