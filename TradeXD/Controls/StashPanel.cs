using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TradeXD.Controls
{
    internal class StashPanel : Panel
    {
        private Rectangle _rect;
        private int _tileSize;

        internal StashPanel(Size size)
        {
            _rect = new Rectangle();
            Location = new Point(0, 0);
            SetSize(size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (Pen pen = new Pen(Color.Red, 1))
            {
                //using (Font font = new Font(FontFamily.GenericSansSerif, 12)) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(Color.Magenta);
                //e.Graphics.DrawString(Text, font, new SolidBrush(Color.Red), _rect.X, _rect.Y - 16);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                //}
            }
        }

        internal Point GetItemPoint(int x, int y, bool quad)
        {
            Point point = new Point
            {
                X = x * _tileSize + _tileSize / 2,
                Y = y * _tileSize + _tileSize / 2
            };

            if (!quad) return PointToScreen(point);

            point.X /= 2;
            point.Y /= 2;
            return PointToScreen(point);
        }

        internal void SetSize(Size size)
        {
            _rect.X = (int) (size.Width * PoEHelper.XMult);
            _rect.Y = (int) (size.Height * PoEHelper.YMult);
            _rect.Width = (int) (size.Height * PoEHelper.SizeMult);
            _rect.Height = (int) (size.Height * PoEHelper.SizeMult);
            Location = _rect.Location;
            Size = _rect.Size;
            _tileSize = _rect.Width / 12;
        }
    }
}