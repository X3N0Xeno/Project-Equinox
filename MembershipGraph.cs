using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SeasonFuzzy
{
    public class MembershipGraph : Panel
    {
        public class SetDef
        {
            public string Name;
            public Color Color;
            public PointF[] Points;
            public double CurrentValue;
            public PointF CustomLabelPos = PointF.Empty;
        }

        public List<SetDef> Sets = new List<SetDef>();
        public double MinX = 0;
        public double MaxX = 100;
        public double CurrentX = 0;

        public MembershipGraph()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(32, 28, 24);
            Padding = new Padding(25, 26, 20, 22);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(Padding.Left, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical);

            using (Pen axisPen = new Pen(DstColors.BorderSepia, 1.5f))
            {
                g.DrawLine(axisPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                g.DrawLine(axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
            }

            Func<double, float> mapX = x => (float)(rect.Left + (x - MinX) / (MaxX - MinX) * rect.Width);
            Func<double, float> mapY = y => (float)(rect.Bottom - y * rect.Height);

            foreach (var set in Sets)
            {
                if (set.Points == null || set.Points.Length == 0) continue;
                using (Pen pen = new Pen(set.Color, 2f))
                {
                    for (int i = 0; i < set.Points.Length - 1; i++)
                    {
                        PointF p1 = new PointF(mapX(set.Points[i].X), mapY(set.Points[i].Y));
                        PointF p2 = new PointF(mapX(set.Points[i + 1].X), mapY(set.Points[i + 1].Y));
                        g.DrawLine(pen, p1, p2);
                    }
                }

                float lx = set.CustomLabelPos.IsEmpty ? mapX(set.Points[set.Points.Length / 2].X) - 14 : mapX(set.CustomLabelPos.X);
                float ly = set.CustomLabelPos.IsEmpty ? mapY(1.0f) - 16 : mapY(set.CustomLabelPos.Y);

                using (Font f = new Font("Georgia", 8f, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(set.Color))
                {
                    g.DrawString(set.Name, f, b, lx, ly);
                }
            }

            float screenX = mapX(CurrentX);
            if (screenX >= rect.Left && screenX <= rect.Right)
            {
                using (Pen linePen = new Pen(DstColors.AccentRed, 1.5f) { DashStyle = DashStyle.Dash })
                    g.DrawLine(linePen, screenX, rect.Top, screenX, rect.Bottom);

                foreach (var set in Sets)
                {
                    if (set.CurrentValue > 0.01)
                    {
                        float screenY = mapY(set.CurrentValue);
                        using (SolidBrush brush = new SolidBrush(set.Color))
                        using (Pen outline = new Pen(DstColors.Parchment, 1.5f))
                        {
                            g.FillEllipse(brush, screenX - 4, screenY - 4, 8, 8);
                            g.DrawEllipse(outline, screenX - 4, screenY - 4, 8, 8);

                            using (Font font = new Font("Georgia", 8f, FontStyle.Bold))
                                g.DrawString(set.CurrentValue.ToString("0.00"), font, brush, screenX + 6, screenY - 7);
                        }
                    }
                }
            }
        }
    }
}