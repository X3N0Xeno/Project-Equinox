using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SeasonFuzzy
{
    public class Palette
    {
        public Color Sky, MountainBack, MountainFront, SnowCap, Ground, GroundBack, Leaf, Trunk, Cloud, Sun, Rain;
    }

    public static class ScenePainter
    {
        static readonly double[] PX = new double[90];
        static readonly double[] PY = new double[90];
        static float animTimer = 0f;

        static ScenePainter()
        {
            long seed = 7;
            for (int i = 0; i < 90; i++)
            {
                seed = (seed * 16807) % 2147483647;
                PX[i] = seed / 2147483647.0;
                seed = (seed * 16807) % 2147483647;
                PY[i] = seed / 2147483647.0;
            }
        }

        static Color H(string hex) => ColorTranslator.FromHtml(hex);
        static int Round(double v) => (int)Math.Floor(v + 0.5);

        public static Color Mix(Color[] cols, double[] w)
        {
            double r = 0, g = 0, b = 0, t = 0;
            for (int i = 0; i < cols.Length; i++) { r += cols[i].R * w[i]; g += cols[i].G * w[i]; b += cols[i].B * w[i]; t += w[i]; }
            return Color.FromArgb(Round(r / (t <= 0 ? 1 : t)), Round(g / (t <= 0 ? 1 : t)), Round(b / (t <= 0 ? 1 : t)));
        }

        public static Color Mix2(Color a, Color b, double t) => Mix(new Color[] { a, b }, new double[] { 1 - t, t });
        static Color WithOpacity(Color c, double op) => Color.FromArgb(Math.Max(0, Math.Min(255, Round(op * 255))), c);

        public static Palette BuildPalette(SceneOutputs o)
        {
            double[] wv = { o.Season.Winter, o.Season.Spring, o.Season.Summer, o.Season.Autumn };
            Color night = H("#0B1428");
            Func<Color, Color> dm = c => Mix2(c, night, o.DimAmount);

            Color daySky = Mix(new Color[] { H("#B8C4CE"), H("#A9D6F0"), H("#6FB8EE"), H("#D9C7AC") }, wv);
            Color skyBase = Mix(new Color[] { H("#0F1B33"), H("#F2A98B"), daySky, H("#D9758A") }, new double[] { o.Time.Night, o.Time.Dawn, o.Time.Day, o.Time.Dusk });
            Color mtn0 = Mix(new Color[] { H("#7B8794"), H("#6E8A6A"), H("#5F8060"), H("#8A7458") }, wv);
            Color gBase = dm(Mix2(Mix(new Color[] { H("#9A9A88"), H("#7CC061"), H("#4E9A3C"), H("#B9823A") }, wv), H("#F4F7FA"), o.GroundSnow));

            return new Palette
            {
                Sky = Mix2(skyBase, H("#7A8590"), o.RainFraction * 0.5 * (1 - o.Time.Night * 0.6)),
                MountainBack = dm(mtn0),
                MountainFront = dm(Mix2(mtn0, Color.White, 0.15)),
                SnowCap = dm(H("#F4F7FA")),
                Ground = gBase,
                GroundBack = Mix2(gBase, Color.Black, 0.12),
                Leaf = dm(Mix(new Color[] { H("#7A6A5A"), H("#8FD16A"), H("#2F7F2F"), H("#D9642A") }, wv)),
                Trunk = dm(H("#5A4633")),
                Cloud = dm(Mix2(Mix2(Color.White, H("#8E98A3"), o.RainFraction), H("#F5B79A"), o.Twilight * 0.5)),
                Sun = Mix2(H("#FFD25A"), H("#FF8A3D"), FuzzyEngine.Clamp01(o.Twilight * 1.1)),
                Rain = Mix2(H("#3F6FA8"), H("#9FC0E8"), o.Time.Night * 0.7)
            };
        }

        public static void Paint(Graphics g, Size size, SceneOutputs o, double hour, SocietyOutputs society = null)
        {
            animTimer += 0.08f;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float scale = Math.Min(size.Width / 640f, size.Height / 300f);
            g.TranslateTransform((size.Width - 640 * scale) / 2f, (size.Height - 300 * scale) / 2f);
            g.ScaleTransform(scale, scale);
            g.SetClip(new RectangleF(0, 0, 640, 300));

            Palette p = BuildPalette(o);
            using (Brush b = new SolidBrush(p.Sky)) g.FillRectangle(b, 0, 0, 640, 300);

            DrawStars(g, o.Stars);
            DrawSunAndMoon(g, hour, o.CloudCover, p);
            DrawClouds(g, o.CloudCover, p.Cloud);

            // Mountain bases extended downward to Y=265 to completely eliminate any seam/gap
            DrawMountain(g, 470, 110, 340, 610, 265, p.MountainFront, FuzzyEngine.Clamp01(0.1 + 0.65 * o.Season.Winter + 0.15 * o.Snow), p.SnowCap);
            DrawMountain(g, 300, 58, 90, 520, 265, p.MountainBack, FuzzyEngine.Clamp01(0.14 + 0.72 * o.Season.Winter + 0.14 * o.Snow), p.SnowCap);

            DrawGround(g, p, o.GroundSnow);
            DrawColonyTrails(g, o.GroundSnow);

            double[,] trees = { { 50, 246, 0.9 }, { 220, 252, 1.0 }, { 250, 250, 0.8 }, { 395, 254, 0.9 }, { 430, 250, 0.8 }, { 595, 246, 0.95 } };
            for (int i = 0; i < 6; i++) DrawTree(g, trees[i, 0], trees[i, 1], trees[i, 2], p.Leaf, o.Foliage, p.Trunk, o.GroundSnow, p.SnowCap);

            DrawGraves(g);

            if (society != null)
            {
                foreach (var node in SocietyEngine.Settlements)
                {
                    if (!node.IsEstablished) continue;
                    float ny = 248f + (float)Math.Sin(node.X / 640f * Math.PI) * 6f;
                    if (node.Id == society.SelectedSettlementId)
                    {
                        using (Pen selPen = new Pen(Color.FromArgb(235, 195, 75), 1.5f) { DashStyle = DashStyle.Dash })
                            g.DrawEllipse(selPen, node.X - 28, ny - 14, 56, 28);
                    }
                    SettlementPainter.DrawSettlementHearth(g, node.X, ny, (float)society.CampfireFlame, node.Creed, society.Era, node.Id == 1);
                }
            }

            SettlementPainter.DrawStickmen(g, animTimer, society != null ? society.Era : HistoricalEra.StoneAge);
            DrawPrecipitation(g, o, p.Rain);

            if (society != null)
            {
                SettlementPainter.DrawLiveWorldFeed(g, society);
                SettlementPainter.DrawSelectedSettlementPopup(g, society);
            }
        }

        static void DrawColonyTrails(Graphics g, double groundSnow)
        {
            if (groundSnow > 0.6) return;
            using (Pen pathPen = new Pen(Color.FromArgb((int)((1.0 - groundSnow) * 60), 65, 50, 35), 2.2f) { DashStyle = DashStyle.Dot })
            {
                g.DrawLine(pathPen, 140, 248, 310, 250);
                g.DrawLine(pathPen, 330, 250, 500, 248);
            }
        }

        static void DrawGraves(Graphics g)
        {
            using (Pen crossPen = new Pen(Color.FromArgb(60, 48, 38), 1.5f))
            {
                foreach (var grave in SocietyEngine.Graves)
                {
                    g.DrawLine(crossPen, grave.X, grave.Y - 8, grave.X, grave.Y);
                    g.DrawLine(crossPen, grave.X - 3, grave.Y - 6, grave.X + 3, grave.Y - 6);
                }
            }
        }

        static void DrawStars(Graphics g, double stars)
        {
            for (int i = 0; i < 45; i++)
            {
                double so = stars * (0.4 + 0.6 * (i % 3) / 2.0);
                if (so > 0.03)
                {
                    float r = (i % 4 == 0) ? 1.6f : 1.1f;
                    using (Brush b = new SolidBrush(WithOpacity(Color.White, so)))
                        g.FillEllipse(b, (float)(PX[i] * 640) - r, (float)(PY[89 - i] * 140) - r, r * 2, r * 2);
                }
            }
        }

        static void DrawSunAndMoon(Graphics g, double h, double cover, Palette p)
        {
            if (h > 5 && h < 19)
            {
                double t = (h - 6) / 12.0;
                using (Brush b = new SolidBrush(WithOpacity(p.Sun, FuzzyEngine.Clamp01(1.1 - cover * 1.2))))
                    g.FillEllipse(b, (float)(60 + 520 * t) - 20, (float)(235 - 195 * Math.Sin(Math.PI * Math.Max(0, Math.Min(1, t)))) - 20, 40, 40);
            }

            double hh = h < 12 ? h + 24 : h;
            if (hh > 17.5 && hh < 30.5)
            {
                double t = (hh - 18) / 12.0;
                using (Brush b = new SolidBrush(WithOpacity(H("#E8EDF5"), FuzzyEngine.Clamp01(1.05 - cover * 1.2))))
                    g.FillEllipse(b, (float)(60 + 520 * t) - 15, (float)(235 - 195 * Math.Sin(Math.PI * Math.Max(0, Math.Min(1, t)))) - 15, 30, 30);
            }
        }

        static void DrawClouds(Graphics g, double cover, Color cloudColor)
        {
            double[,] cp = { { 120, 62, 1 }, { 270, 44, 1.2 }, { 410, 72, 0.9 }, { 530, 48, 1.1 }, { 200, 100, 0.8 }, { 470, 104, 0.9 } };
            double[] th = { 0.05, 0.15, 0.3, 0.45, 0.6, 0.75 };
            for (int i = 0; i < 6; i++)
            {
                double o = FuzzyEngine.Clamp01((cover - th[i]) / 0.25);
                if (o > 0.02)
                {
                    double x = cp[i, 0], y = cp[i, 1], s = cp[i, 2];
                    using (GraphicsPath path = new GraphicsPath(FillMode.Winding))
                    {
                        path.AddEllipse((float)(x - 34 * s), (float)(y - 13 * s), (float)(68 * s), (float)(26 * s));
                        path.AddEllipse((float)(x - 20 * s - 22 * s), (float)(y + 3 * s - 10 * s), (float)(44 * s), (float)(20 * s));
                        path.AddEllipse((float)(x + 22 * s - 24 * s), (float)(y + 3 * s - 10 * s), (float)(48 * s), (float)(20 * s));
                        using (Brush b = new SolidBrush(WithOpacity(cloudColor, o))) g.FillPath(b, path);
                    }
                }
            }
        }

        static void DrawMountain(Graphics g, double p, double q, double bl, double br, double by, Color col, double cap, Color capCol)
        {
            PointF[] body = { new PointF((float)bl, (float)by), new PointF((float)p, (float)q), new PointF((float)br, (float)by) };
            using (Brush b = new SolidBrush(col)) g.FillPolygon(b, body);

            if (cap > 0.02)
            {
                double yl = q + (by - q) * cap, xl = p - (p - bl) * cap, xr = p + (br - p) * cap, wd = xr - xl;
                PointF[] snow = { new PointF((float)p, (float)q), new PointF((float)xr, (float)yl), new PointF((float)(xr - wd * 0.5), (float)(yl + 6)), new PointF((float)xl, (float)yl) };
                using (Brush b = new SolidBrush(capCol)) g.FillPolygon(b, snow);
            }
        }

        static void DrawGround(Graphics g, Palette p, double groundSnow)
        {
            // Back hill layer: covers bottom of mountains completely
            using (GraphicsPath backHill = new GraphicsPath())
            {
                backHill.AddBezier(new PointF(0, 215), new PointF(180, 192), new PointF(360, 222), new PointF(640, 206));
                backHill.AddLine(640, 206, 640, 300);
                backHill.AddLine(640, 300, 0, 300);
                backHill.CloseFigure();
                using (Brush b = new SolidBrush(p.GroundBack)) g.FillPath(b, backHill);
            }

            // Foreground rolling hill
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddBezier(new PointF(0, 240), new PointF(160, 222), new PointF(330, 242), new PointF(640, 236));
                path.AddLine(640, 236, 640, 300);
                path.AddLine(640, 300, 0, 300);
                path.CloseFigure();
                using (Brush b = new SolidBrush(p.Ground)) g.FillPath(b, path);

                if (groundSnow > 0.05)
                {
                    using (Pen snowPen = new Pen(WithOpacity(p.SnowCap, groundSnow), (float)(groundSnow * 10.0)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        using (GraphicsPath crest = new GraphicsPath())
                        {
                            crest.AddBezier(new PointF(0, 240), new PointF(160, 222), new PointF(330, 242), new PointF(640, 236));
                            g.DrawPath(snowPen, crest);
                        }
                    }
                }
            }
        }

        static void DrawTree(Graphics g, double x, double y, double s, Color leafCol, double leaf, Color trunk, double groundSnow, Color snowCol)
        {
            double h = 28 * s;
            using (Brush b = new SolidBrush(trunk)) g.FillRectangle(b, (float)(x - 3 * s), (float)(y - h), (float)(6 * s), (float)h);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse((float)(x - 20 * s), (float)(y - h - 28 * s), (float)(40 * s), (float)(40 * s));
                using (Brush b = new SolidBrush(WithOpacity(leafCol, leaf))) g.FillPath(b, path);
            }
        }

        static void DrawPrecipitation(Graphics g, SceneOutputs o, Color rainColor)
        {
            int nr = Round(o.Rain * 80), ns = Round(o.Snow * 85);
            if (nr > 0)
            {
                using (Pen pen = new Pen(WithOpacity(rainColor, 0.65), 1.3f))
                {
                    for (int i = 0; i < nr; i++)
                    {
                        float y = (float)((PY[i] * 270 + animTimer * (260f + (i % 5) * 45f)) % 270);
                        float x = (float)(PX[i] * 640 - (y * 0.28f));
                        if (x < 0) x += 640;
                        g.DrawLine(pen, x, y, x - 4, y + 13);
                    }
                }
            }
            if (ns > 0)
            {
                using (Brush fill = new SolidBrush(Color.White))
                {
                    for (int i = 0; i < ns; i++)
                    {
                        float y = (float)((PY[i] * 280 + animTimer * (35f + (i % 4) * 15f)) % 280);
                        float x = (float)(PX[89 - i] * 640 + Math.Sin(animTimer * 2.5f + i) * 6f);
                        if (x < 0) x += 640; if (x > 640) x -= 640;
                        g.FillEllipse(fill, x - 2, y - 2, 4, 4);
                    }
                }
            }
        }
    }
}