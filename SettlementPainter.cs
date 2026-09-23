using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SeasonFuzzy
{
    public static class SettlementPainter
    {
        public static void DrawLiveWorldFeed(Graphics g, SocietyOutputs s)
        {
            int startX = 12, startY = 12;
            using (Font titleF = new Font("Georgia", 7.5f, FontStyle.Bold))
            using (Font itemF = new Font("Consolas", 7.2f))
            using (Font btnF = new Font("Segoe UI", 7.0f, FontStyle.Bold))
            using (Brush gold = new SolidBrush(Color.FromArgb(235, 195, 75)))
            using (Brush bg = new SolidBrush(Color.FromArgb(235, 18, 14, 10)))
            using (Pen border = new Pen(Color.FromArgb(100, 80, 60), 1.2f))
            {
                string titleText = $"WORLD CHRONICLE | ERA: {s.Era.ToString().ToUpper()} (YR {s.CurrentYear:0})";

                if (SocietyEngine.IsFeedMinimized)
                {
                    int minW = (int)g.MeasureString(titleText, titleF).Width + 34;
                    Rectangle minBox = new Rectangle(startX, startY, minW, 24);

                    using (Brush minBg = new SolidBrush(Color.FromArgb(51, 18, 14, 10)))
                    using (Pen minBorder = new Pen(Color.FromArgb(51, 100, 80, 60), 1.2f))
                    using (Brush minGold = new SolidBrush(Color.FromArgb(51, 235, 195, 75)))
                    {
                        g.FillRectangle(minBg, minBox);
                        g.DrawRectangle(minBorder, minBox);
                        g.DrawString(titleText, titleF, minGold, startX + 6, startY + 5);
                        g.DrawString("[+]", btnF, minGold, startX + minW - 22, startY + 5);
                    }
                    return;
                }

                float maxW = g.MeasureString(titleText, titleF).Width;
                int count = Math.Min(4, s.LiveFeed.Count);
                for (int i = 0; i < count; i++)
                {
                    float w = g.MeasureString($"[{s.LiveFeed[i].Year:0}y] {s.LiveFeed[i].Text}", itemF).Width;
                    if (w > maxW) maxW = w;
                }

                int dynamicW = Math.Max(220, Math.Min(440, (int)maxW + 34));
                int dynamicH = 26 + (count == 0 ? 18 : count * 15) + 6;

                Rectangle box = new Rectangle(startX, startY, dynamicW, dynamicH);
                g.FillRectangle(bg, box);
                g.DrawRectangle(border, box);
                g.DrawString(titleText, titleF, gold, startX + 6, startY + 6);
                g.DrawString("[—]", btnF, gold, startX + dynamicW - 22, startY + 5);

                int yOffset = startY + 24;
                for (int i = 0; i < count; i++)
                {
                    using (Brush eb = new SolidBrush(s.LiveFeed[i].TextColor))
                        g.DrawString($"[{s.LiveFeed[i].Year:0}y] {s.LiveFeed[i].Text}", itemF, eb, startX + 6, yOffset);
                    yOffset += 15;
                }
            }
        }

        public static void DrawSelectedSettlementPopup(Graphics g, SocietyOutputs s)
        {
            if (s.SelectedSettlementId == -1) return;
            var node = SocietyEngine.Settlements.Find(n => n.Id == s.SelectedSettlementId && n.IsEstablished);
            if (node == null) return;

            Rectangle box = new Rectangle((int)Math.Max(120f, Math.Min(520f, node.X)) - 100, 135, 200, 80);
            using (Brush bg = new SolidBrush(Color.FromArgb(245, 24, 20, 16)))
            using (Pen border = new Pen(Color.FromArgb(235, 195, 75), 1.4f))
            using (Font titleF = new Font("Georgia", 8f, FontStyle.Bold))
            using (Font regF = new Font("Georgia", 7.2f))
            using (Brush gold = new SolidBrush(Color.FromArgb(235, 195, 75)))
            using (Brush text = new SolidBrush(Color.FromArgb(220, 210, 195)))
            {
                g.FillRectangle(bg, box);
                g.DrawRectangle(border, box);
                Color creedCol = node.Creed == SocietalCreed.GodFearing ? Color.FromArgb(120, 210, 120) : node.Creed == SocietalCreed.Savages ? Color.FromArgb(240, 70, 60) : Color.FromArgb(220, 180, 100);

                using (Brush cb = new SolidBrush(creedCol))
                {
                    g.DrawString(node.Name.ToUpper(), titleF, gold, box.X + 8, box.Y + 8);
                    g.DrawString($"Creed: {node.Creed}", regF, cb, box.X + 8, box.Y + 24);
                }
                g.DrawString($"Ruler: {node.LeaderName}", regF, text, box.X + 8, box.Y + 40);
                g.DrawString($"Citizens: {SocietyEngine.AllCitizens.FindAll(c => c.IsAlive && c.AssignedSettlementId == node.Id).Count} | State: {(node.IsAtWar ? "WAR" : "Peace")}", regF, text, box.X + 8, box.Y + 56);
            }
        }

        public static void DrawSettlementHearth(Graphics g, float x, float y, float flame, SocietalCreed creed, HistoricalEra era, bool isCapital)
        {
            if (era == HistoricalEra.StoneAge)
            {
                using (Brush stoneB = new SolidBrush(Color.FromArgb(40, 34, 30)))
                    g.FillEllipse(stoneB, x - 12, y - 4, 24, 8);
                if (creed == SocietalCreed.GodFearing)
                {
                    using (Pen p = new Pen(Color.FromArgb(235, 195, 95), 2.4f)) { g.DrawLine(p, x - 16, y + 2, x - 16, y - 18); g.DrawLine(p, x - 21, y - 12, x - 11, y - 12); }
                }
                else if (creed == SocietalCreed.Savages || creed == SocietalCreed.IdolWorshippers)
                {
                    using (Pen p = new Pen(Color.FromArgb(180, 40, 30), 2.5f)) g.DrawLine(p, x + 16, y + 2, x + 16, y - 16);
                }
            }
            else if (era == HistoricalEra.MedievalEra)
            {
                using (Brush woodB = new SolidBrush(Color.FromArgb(65, 45, 30))) g.FillRectangle(woodB, x - 20, y - 20, 24, 20);
            }
            else if (era == HistoricalEra.IndustrialEra)
            {
                using (Brush ironB = new SolidBrush(Color.FromArgb(35, 35, 40))) g.FillRectangle(ironB, x - 8, y - 12, 22, 14);
            }
            else
            {
                using (Brush steelB = new SolidBrush(Color.FromArgb(50, 60, 75))) g.FillRectangle(steelB, x - 15, y - 26, 30, 26);
            }

            if (flame > 0.05f)
            {
                float h = 15f * flame;
                PointF[] flamePts = { new PointF(x - 5, y), new PointF(x, y - h), new PointF(x + 5, y) };
                using (Brush fb = new SolidBrush(creed == SocietalCreed.GodFearing ? Color.FromArgb(255, 205, 50) : Color.FromArgb(235, 80, 25)))
                    g.FillPolygon(fb, flamePts);
            }
        }

        public static void DrawStickmen(Graphics g, float animTimer, HistoricalEra era)
        {
            Color silhouette = Color.FromArgb(22, 18, 14);
            using (Brush b = new SolidBrush(silhouette))
            using (Pen p = new Pen(silhouette, 1.8f))
            using (Pen babyPen = new Pen(silhouette, 1.2f))
            using (Pen spearPen = new Pen(Color.FromArgb(175, 160, 140), 1.6f))
            using (Brush crownB = new SolidBrush(Color.FromArgb(255, 215, 60)))
            {
                foreach (var c in SocietyEngine.AllCitizens)
                {
                    if (!c.IsAlive) continue;
                    float x = c.CurrentX;
                    float y = 246f + (float)Math.Sin((x / 640f) * Math.PI) * 12f;
                    float legSwing = (float)Math.Sin(c.WalkCycle) * 3f;

                    if (c.Stage == LifeStage.Baby)
                    {
                        g.FillEllipse(b, x - 2f, y - 10, 4, 4);
                        g.DrawLine(babyPen, x, y - 7, x, y - 3);
                        g.DrawLine(babyPen, x, y - 3, x - 2 + legSwing * 0.6f, y);
                        g.DrawLine(babyPen, x, y - 3, x + 2 - legSwing * 0.6f, y);
                    }
                    else
                    {
                        g.FillEllipse(b, x - 2.5f, y - 17, 5, 5);
                        if (c.IsLeader)
                        {
                            PointF[] crown = { new PointF(x - 3.5f, y - 18), new PointF(x - 3.5f, y - 22), new PointF(x, y - 23), new PointF(x + 3.5f, y - 22), new PointF(x + 3.5f, y - 18) };
                            g.FillPolygon(crownB, crown);
                        }
                        g.DrawLine(p, x, y - 12, x, y - 5);
                        g.DrawLine(p, x, y - 5, x - 3 + legSwing, y);
                        g.DrawLine(p, x, y - 5, x + 3 - legSwing, y);
                        g.DrawLine(p, x, y - 10, x - 4, y - 6);
                        g.DrawLine(p, x, y - 10, x + 4, y - 7);

                        if (c.IsArmed) g.DrawLine(spearPen, x + 3, y - 18, x + 7, y);
                    }

                    if (!string.IsNullOrEmpty(c.SpeechText))
                        DrawBubble(g, x, y - 20, c.SpeechText);
                }

                foreach (var smite in SocietyEngine.Smites)
                {
                    using (Pen smiteCore = new Pen(Color.FromArgb((int)(smite.Timer * 255), 255, 255, 220), 3f))
                        g.DrawLine(smiteCore, smite.X + 15, 0, smite.X, smite.Y);
                }
            }
        }

        static void DrawBubble(Graphics g, float x, float y, string text)
        {
            using (Font f = new Font("Georgia", 6.5f, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(text, f);
                float bw = sz.Width + 8, bh = sz.Height + 4, bx = x - bw / 2f, by = y - bh - 6;
                using (Brush bg = new SolidBrush(Color.FromArgb(230, 32, 26, 20)))
                using (Pen border = new Pen(Color.FromArgb(115, 95, 75), 1f))
                using (Brush textB = new SolidBrush(Color.FromArgb(240, 230, 205)))
                {
                    g.FillRectangle(bg, bx, by, bw, bh);
                    g.DrawRectangle(border, bx, by, bw, bh);
                    g.DrawString(text, f, textB, bx + 4, by + 2);
                }
            }
        }
    }
}