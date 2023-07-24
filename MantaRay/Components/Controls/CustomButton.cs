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

        public event EventHandler<EventArgs> Click;

        protected virtual void OnClick(EventArgs e)
        {
            if (Click != null)
                Click(this, e);
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
            base.OnMouseDown(e);
            if (Enabled)
            {
                mouseDown = true;
                Invalidate();
            }
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
            base.OnMouseUp(e);
            var rect = new Rectangle(this.Size);
            if (mouseDown && rect.Contains((Eto.Drawing.Point)e.Location))
            {
                if (Toggle)
                    pressed = !pressed;
                else if (Persistent)
                    pressed = true;
                else
                    pressed = false;
                mouseDown = false;

                this.Invalidate();
                if (Enabled)
                    OnClick(EventArgs.Empty);
            }
            else
            {
                mouseDown = false;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            var rect = new Rectangle(this.Size);
            var col = Color.FromGrayscale(hover && Enabled ? 0.95f : 0.8f);
            if (Enabled && (pressed || mouseDown))
            {
                pe.Graphics.FillRectangle(col, rect);
                pe.Graphics.DrawInsetRectangle(Colors.Gray, Colors.White, rect);
                
            }
            else if (hover && Enabled)
            {
                pe.Graphics.FillRectangle(col, rect);
                pe.Graphics.DrawInsetRectangle(Colors.White, Colors.Gray, rect);
            }
            
            Rectangle rect2 = new Rectangle(rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10);
            Brush brush = new SolidBrush(Colors.Blue);
            Pen pen = new Pen(brush, 10);
            pe.Graphics.DrawRectangle(pen, rect2);

            base.OnPaint(pe);
        }
    }
}

