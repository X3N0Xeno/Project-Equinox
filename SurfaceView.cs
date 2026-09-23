using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SeasonFuzzy
{
    public class SurfaceView : Panel
    {
        struct ProjPoint
        {
            public PointF Screen;
            public double CamDist;
        }

        struct Quad
        {
            public PointF[] Poly;
            public Color FillColor;
            public double CamDist;
        }

        double[,] zData;
        readonly int gridX = 17; // Smoother, cleaner mesh
        readonly int gridY = 17;
        int cachedRain = -1;
        int cachedDoy = -1;
        double cachedHour = -1;
        string cachedMetric = "Brightness";

        public string SelectedMetric = "Brightness";

        public float Yaw = 45f;
        public float Pitch = 35f;
        public float Zoom = 1.0f;

        bool isDragging = false;
        Point lastMousePos;

        public SurfaceView()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(22, 19, 16);
            Cursor = Cursors.SizeAll;

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) { isDragging = true; lastMousePos = e.Location; }
                else if (e.Button == MouseButtons.Right) { Yaw = 45f; Pitch = 35f; Zoom = 1.0f; Invalidate(); }
            };
            MouseMove += (s, e) =>
            {
                if (isDragging && e.Button == MouseButtons.Left)
                {
                    Yaw = (Yaw + (e.X - lastMousePos.X) * 0.7f) % 360f;
                    if (Yaw < 0) Yaw += 360f;
                    Pitch = Math.Max(15f, Math.Min(75f, Pitch + (e.Y - lastMousePos.Y) * 0.5f));
                    lastMousePos = e.Location;
                    Invalidate();
                }
            };
            MouseUp += (s, e) => isDragging = false;
            MouseWheel += (s, e) =>
            {
                Zoom = Math.Max(0.6f, Math.Min(2.0f, Zoom + (e.Delta > 0 ? 0.08f : -0.08f)));
                Invalidate();
            };
        }

        double ExtractMetric(SceneOutputs o, string metric)
        {
            switch (metric)
            {
                case "Cloud Cover": return o.CloudCover;
                case "Foliage": return o.Foliage;
                case "Ground Snow": return o.GroundSnow;
                case "Precipitation": return Math.Max(o.Rain, o.Snow);
                default: return o.SkyBrightness;
            }
        }

        public void UpdateState(int rain, int doy, double hour)
        {
            if (rain != cachedRain || SelectedMetric != cachedMetric || zData == null)
            {
                cachedRain = rain;
                cachedMetric = SelectedMetric;
                zData = new double[gridX, gridY];
                for (int i = 0; i < gridX; i++)
                {
                    int d = (int)Math.Max(1, (i / (double)(gridX - 1)) * 365);
                    for (int j = 0; j < gridY; j++)
                    {
                        double h = (j / (double)(gridY - 1)) * 24.0;
                        zData[i, j] = ExtractMetric(FuzzyEngine.Infer(d, h, rain), SelectedMetric);
                    }
                }
            }

            cachedDoy = doy;
            cachedHour = hour;
            Invalidate();
        }

        Color GetSurfaceColor(double z)
        {
            z = Math.Max(0.0, Math.Min(1.0, z));
            int r, g, b;
            if (z < 0.5)
            {
                double t = z / 0.5;
                r = (int)(30 + t * 90);
                g = (int)(25 + t * 65);
                b = (int)(20 + t * 15);
            }
            else
            {
                double t = (z - 0.5) / 0.5;
                r = (int)(120 + t * 125);
                g = (int)(90 + t * 115);
                b = (int)(35 + t * 55);
            }
            return Color.FromArgb(255, Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
        }

        ProjPoint Project3D(float normX, float normY, float normZ, float cx, float cy, float scale)
        {
            float x = normX - 0.5f;
            float y = normY - 0.5f;
            float z = normZ * 0.65f;

            double radYaw = (Yaw * Math.PI) / 180.0;
            double radPitch = (Pitch * Math.PI) / 180.0;

            double x1 = x * Math.Cos(radYaw) - y * Math.Sin(radYaw);
            double y1 = x * Math.Sin(radYaw) + y * Math.Cos(radYaw);
            double z1 = z;

            double y2 = y1 * Math.Cos(radPitch) - z1 * Math.Sin(radPitch);
            double z2 = y1 * Math.Sin(radPitch) + z1 * Math.Cos(radPitch);

            float screenX = cx + (float)(x1 * scale);
            float screenY = cy - (float)(z2 * scale);

            return new ProjPoint { Screen = new PointF(screenX, screenY), CamDist = y2 };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (zData == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = Width / 2f;
            float cy = Height / 2f + 14f;
            float scale = Math.Min(Width, Height) * 0.92f * Zoom;

            // 1. Solid Base Grid
            PointF f00 = Project3D(0, 0, 0, cx, cy, scale).Screen;
            PointF f10 = Project3D(1, 0, 0, cx, cy, scale).Screen;
            PointF f11 = Project3D(1, 1, 0, cx, cy, scale).Screen;
            PointF f01 = Project3D(0, 1, 0, cx, cy, scale).Screen;

            using (Brush floorBrush = new SolidBrush(Color.FromArgb(16, 13, 10)))
            using (Pen floorPen = new Pen(DstColors.BorderSepia, 1.2f))
            {
                PointF[] floorPoly = { f00, f10, f11, f01 };
                g.FillPolygon(floorBrush, floorPoly);
                g.DrawPolygon(floorPen, floorPoly);
            }

            // 2. Mesh Projection
            ProjPoint[,] grid = new ProjPoint[gridX, gridY];
            for (int i = 0; i < gridX; i++)
            {
                float nx = i / (float)(gridX - 1);
                for (int j = 0; j < gridY; j++)
                {
                    float ny = j / (float)(gridY - 1);
                    grid[i, j] = Project3D(nx, ny, (float)zData[i, j], cx, cy, scale);
                }
            }

            Quad[] quads = new Quad[(gridX - 1) * (gridY - 1)];
            int qIdx = 0;
            for (int i = 0; i < gridX - 1; i++)
            {
                for (int j = 0; j < gridY - 1; j++)
                {
                    double avgZ = (zData[i, j] + zData[i + 1, j] + zData[i + 1, j + 1] + zData[i, j + 1]) / 4.0;
                    double dist = (grid[i, j].CamDist + grid[i + 1, j].CamDist + grid[i + 1, j + 1].CamDist + grid[i, j + 1].CamDist) / 4.0;

                    quads[qIdx++] = new Quad
                    {
                        Poly = new PointF[] { grid[i, j].Screen, grid[i + 1, j].Screen, grid[i + 1, j + 1].Screen, grid[i, j + 1].Screen },
                        FillColor = GetSurfaceColor(avgZ),
                        CamDist = dist
                    };
                }
            }

            Array.Sort(quads, (a, b) => a.CamDist.CompareTo(b.CamDist));

            using (Pen meshPen = new Pen(Color.FromArgb(80, 10, 8, 6), 0.75f))
            {
                for (int k = 0; k < quads.Length; k++)
                {
                    using (Brush brush = new SolidBrush(quads[k].FillColor))
                    {
                        g.FillPolygon(brush, quads[k].Poly);
                        g.DrawPolygon(meshPen, quads[k].Poly);
                    }
                }
            }

            // 3. Axis Rays
            using (Pen dayPen = new Pen(Color.FromArgb(220, 140, 60), 2f) { EndCap = LineCap.ArrowAnchor })
            using (Pen timePen = new Pen(Color.FromArgb(130, 160, 220), 2f) { EndCap = LineCap.ArrowAnchor })
            using (Font f = new Font("Georgia", 8f, FontStyle.Bold))
            {
                g.DrawLine(dayPen, f00, f10);
                g.DrawLine(timePen, f00, f01);

                using (Brush b = new SolidBrush(Color.FromArgb(235, 165, 95)))
                    g.DrawString("Calendar Days (1 → 365)", f, b, (f00.X + f10.X) / 2f - 20, (f00.Y + f10.Y) / 2f + 10);

                using (Brush b = new SolidBrush(Color.FromArgb(160, 190, 240)))
                    g.DrawString("Hours (00:00 → 24:00)", f, b, (f00.X + f01.X) / 2f - 60, (f00.Y + f01.Y) / 2f - 16);
            }

            // 4. Current Operating Point
            float floatX = (cachedDoy - 1f) / 364f;
            float floatY = (float)(cachedHour / 24f);
            double currentZ = ExtractMetric(FuzzyEngine.Infer(cachedDoy, cachedHour, cachedRain), SelectedMetric);

            PointF marker = Project3D(floatX, floatY, (float)currentZ, cx, cy, scale).Screen;
            PointF floorPt = Project3D(floatX, floatY, 0, cx, cy, scale).Screen;

            using (Pen dropPen = new Pen(DstColors.AccentRed, 2f) { DashStyle = DashStyle.Dash })
            using (Brush dotBrush = new SolidBrush(DstColors.AccentGold))
            using (Pen dotEdge = new Pen(Color.White, 2f))
            {
                g.DrawLine(dropPen, marker, floorPt);
                g.FillEllipse(dotBrush, marker.X - 5, marker.Y - 5, 10, 10);
                g.DrawEllipse(dotEdge, marker.X - 5, marker.Y - 5, 10, 10);
            }

            // 5. On-Screen HUD (Only visible when maximized / full screen)
            if (Width > 750)
            {
                Rectangle hudRect = new Rectangle(14, 14, 230, 84);
                using (Brush bg = new SolidBrush(Color.FromArgb(210, 20, 16, 12)))
                using (Pen border = new Pen(DstColors.BorderSepia, 1.2f))
                {
                    g.FillRectangle(bg, hudRect);
                    g.DrawRectangle(border, hudRect);
                }

                using (Font titleFont = new Font("Georgia", 8f, FontStyle.Bold))
                using (Font regularFont = new Font("Georgia", 7.5f))
                using (Brush gold = new SolidBrush(DstColors.AccentGold))
                using (Brush text = new SolidBrush(DstColors.Parchment))
                {
                    g.DrawString("SURFACE COORDINATES", titleFont, gold, 20, 18);
                    g.DrawString($"• Day:  {cachedDoy} / 365", regularFont, text, 20, 34);
                    g.DrawString($"• Hour: {cachedHour:00}:00", regularFont, text, 20, 50);
                    g.DrawString($"• Peak: {SelectedMetric} = {currentZ:0.00}", regularFont, text, 20, 66);
                }
            }
        }
    }
}