using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SeasonFuzzy
{
    public static class DstColors
    {
        public static readonly Color Background = Color.FromArgb(28, 24, 20);
        public static readonly Color CardBg = Color.FromArgb(42, 36, 30);
        public static readonly Color Parchment = Color.FromArgb(208, 196, 172);
        public static readonly Color DarkInk = Color.FromArgb(18, 15, 12);
        public static readonly Color BorderSepia = Color.FromArgb(85, 72, 58);
        public static readonly Color AccentRed = Color.FromArgb(168, 62, 46);
        public static readonly Color AccentGold = Color.FromArgb(198, 142, 60);
        public static readonly Color BarTrack = Color.FromArgb(24, 20, 16);
    }

    public class DstCard : Panel
    {
        public Panel Content = new Panel();
        public Label HeaderLabel = new Label();
        public Button BtnMaximize = new Button();
        public event EventHandler MaximizeClicked;

        public DstCard()
        {
            BackColor = DstColors.CardBg;
            Padding = new Padding(10, 8, 10, 10);

            Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 22 };

            HeaderLabel.Dock = DockStyle.Fill;
            HeaderLabel.ForeColor = DstColors.Parchment;
            HeaderLabel.Font = new Font("Georgia", 8.5f, FontStyle.Bold);
            HeaderLabel.TextAlign = ContentAlignment.MiddleLeft;

            BtnMaximize.Dock = DockStyle.Right;
            BtnMaximize.Width = 26;
            BtnMaximize.FlatStyle = FlatStyle.Flat;
            BtnMaximize.FlatAppearance.BorderSize = 0;
            BtnMaximize.ForeColor = DstColors.AccentGold;
            BtnMaximize.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            BtnMaximize.Text = "⛶";
            BtnMaximize.Cursor = Cursors.Hand;
            BtnMaximize.Visible = false;
            BtnMaximize.Click += (s, e) => MaximizeClicked?.Invoke(this, EventArgs.Empty);

            titleBar.Controls.Add(HeaderLabel);
            titleBar.Controls.Add(BtnMaximize);

            Panel accentBar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = DstColors.AccentGold };
            Content.Dock = DockStyle.Fill;

            Controls.Add(Content);
            Controls.Add(titleBar);
            Controls.Add(accentBar);
        }

        public void SetTitle(string title, Color accent, bool showMaximize = false)
        {
            HeaderLabel.Text = title.ToUpperInvariant();
            Controls[2].BackColor = accent;
            BtnMaximize.Visible = showMaximize;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen border = new Pen(DstColors.BorderSepia, 1.5f))
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }
    }

    public class SceneView : Panel
    {
        public SceneOutputs Outputs;
        public SocietyOutputs Society;
        public double Hour;

        public SceneView()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(15, 20, 30);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Outputs == null) return;
            ScenePainter.Paint(e.Graphics, ClientSize, Outputs, Hour, Society);
        }
    }

    public class FlatBar : Control
    {
        int val = 0;
        public int Value
        {
            get => val;
            set { val = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public Color FillColor = DstColors.AccentGold;

        public FlatBar()
        {
            DoubleBuffered = true;
            Height = 7;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            using (Brush bg = new SolidBrush(DstColors.BarTrack))
                g.FillRectangle(bg, ClientRectangle);

            int w = (int)(Width * (val / 100.0));
            if (w > 0)
                using (Brush fg = new SolidBrush(FillColor))
                    g.FillRectangle(fg, 0, 0, w, Height);

            using (Pen p = new Pen(DstColors.BorderSepia, 1f))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }

    public class BarRow : Panel
    {
        Label name = new Label();
        Label percent = new Label();
        FlatBar bar = new FlatBar();

        public BarRow(string caption)
        {
            Height = 36;
            Dock = DockStyle.Top;
            Padding = new Padding(0, 0, 0, 4);

            Panel labelRow = new Panel { Dock = DockStyle.Top, Height = 18 };

            name.Text = caption;
            name.Dock = DockStyle.Fill;
            name.ForeColor = DstColors.Parchment;
            name.Font = new Font("Georgia", 8.25f);

            percent.Text = "0%";
            percent.Dock = DockStyle.Right;
            percent.Width = 46;
            percent.TextAlign = ContentAlignment.MiddleRight;
            percent.ForeColor = DstColors.AccentGold;
            percent.Font = new Font("Georgia", 8.5f, FontStyle.Bold);

            labelRow.Controls.Add(name);
            labelRow.Controls.Add(percent);

            bar.Dock = DockStyle.Top;
            bar.Height = 7;

            Controls.Add(bar);
            Controls.Add(labelRow);
        }

        public void SetValue(double v)
        {
            int p = (int)Math.Floor(v * 100 + 0.5);
            percent.Text = p + "%";
            bar.Value = p;
        }
    }

    public class FlatSlider : Control
    {
        public int Minimum = 0;
        public int Maximum = 100;
        int val = 0;

        public int Value
        {
            get => val;
            set
            {
                val = Math.Max(Minimum, Math.Min(Maximum, value));
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Func<int, string> LabelFormatter;
        public Color FillColor = DstColors.AccentGold;
        public event EventHandler ValueChanged;
        bool dragging = false;

        public FlatSlider()
        {
            DoubleBuffered = true;
            Height = 44;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            dragging = true;
            SetFromX(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging) SetFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
        }

        void SetFromX(int x)
        {
            int trackLeft = 12, trackRight = Width - 12;
            if (trackRight <= trackLeft) return;
            double t = (double)(x - trackLeft) / (trackRight - trackLeft);
            Value = Minimum + (int)Math.Round(Math.Max(0, Math.Min(1, t)) * (Maximum - Minimum));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackY = 16;
            int trackLeft = 12, trackRight = Width - 12;

            using (Pen trackPen = new Pen(DstColors.BarTrack, 5))
                g.DrawLine(trackPen, trackLeft, trackY, trackRight, trackY);

            double frac = Maximum > Minimum ? (double)(val - Minimum) / (Maximum - Minimum) : 0;
            float knobX = (float)(trackLeft + frac * (trackRight - trackLeft));

            if (knobX > trackLeft)
                using (Pen fillPen = new Pen(FillColor, 5))
                    g.DrawLine(fillPen, trackLeft, trackY, knobX, trackY);

            using (Brush knobBrush = new SolidBrush(DstColors.Parchment))
            using (Pen knobEdge = new Pen(DstColors.DarkInk, 2f))
            {
                g.FillEllipse(knobBrush, knobX - 7, trackY - 7, 14, 14);
                g.DrawEllipse(knobEdge, knobX - 7, trackY - 7, 14, 14);
            }

            string label = LabelFormatter != null ? LabelFormatter(val) : val.ToString();
            using (Font f = new Font("Georgia", 8.5f, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(DstColors.Parchment))
            {
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, textBrush, (Width - sz.Width) / 2f, trackY + 10);
            }
        }
    }
}