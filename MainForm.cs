using System;
using System.Drawing;
using System.Windows.Forms;

namespace SeasonFuzzy
{
    public partial class MainForm : Form
    {
        static readonly string[] MonthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        BarRow[] outBars;
        Timer simTimer = new Timer();
        Button btnPlayPause, btnResetSim, btnToggleView;
        ComboBox cmbSimSpeed, cmbPresets;
        FlatSlider sldYearScrub;

        double fractionalHour = 18.0;
        double speedMultiplier = 1.0;
        bool isMonitoringHumans = false;
        bool isInternalYearUpdate = false;

        Panel pnlHumanRegistry;
        ListView lvHumans;
        Label lblRegistryStats;

        Random weatherRng = new Random(42);
        CheckBox chkAutoWeather;
        double targetRain = 0.0, currentRainFloat = 15.0, weatherDurationHours = 0.0;
        bool isStormFront = false;

        public MainForm()
        {
            InitializeComponent();
            BuildUiContents();
            BuildHumanRegistryPanel();

            simTimer.Interval = 33;
            simTimer.Tick += SimTimer_Tick;

            sldMonth.Value = 7; UpdateDayBounds();
            sldDay.Value = 15; sldTime.Value = 18; sldRain.Value = 15;
            Recalculate();
        }

        void ToggleInspectorView()
        {
            isMonitoringHumans = !isMonitoringHumans;
            btnToggleView.Text = isMonitoringHumans ? "📈 SWITCH TO FUZZY MEMBERSHIP GRAPHS" : "👥 VIEW HUMAN PEDIGREE REGISTRY";
            if (isMonitoringHumans)
            {
                mainTableLayout.Controls.Remove(seasonCard); mainTableLayout.Controls.Remove(timeCard);
                mainTableLayout.Controls.Add(pnlHumanRegistry, 0, 1);
                mainTableLayout.SetColumnSpan(pnlHumanRegistry, 2);
                pnlHumanRegistry.Visible = true;
                UpdateHumanRegistryTable();
            }
            else
            {
                pnlHumanRegistry.Visible = false;
                mainTableLayout.Controls.Remove(pnlHumanRegistry);
                mainTableLayout.Controls.Add(seasonCard, 0, 1);
                mainTableLayout.Controls.Add(timeCard, 1, 1);
            }
        }

        void UpdateHumanRegistryTable()
        {
            if (!isMonitoringHumans) return;
            lvHumans.BeginUpdate();
            lvHumans.Items.Clear();
            int alive = 0, dead = 0;

            foreach (var h in SocietyEngine.AllCitizens)
            {
                if (h.IsAlive) alive++; else dead++;
                ListViewItem item = new ListViewItem(h.Id.ToString());
                item.SubItems.Add((h.IsLeader ? "👑 " : "") + h.FullName);
                item.SubItems.Add(h.Gender);
                item.SubItems.Add(h.AgeYears.ToString("0.0") + "y");
                item.SubItems.Add(h.IsAlive ? "Alive" : $"Dead ({h.CauseOfDeath})");
                item.SubItems.Add($"{h.FatherName} & {h.MotherName}");
                item.SubItems.Add(h.Genome.Health.ToString("0"));
                item.SubItems.Add(h.Genome.Intelligence.ToString("0"));
                item.SubItems.Add(h.Genome.Strength.ToString("0"));
                item.SubItems.Add(h.Genome.Agility.ToString("0"));
                item.SubItems.Add(h.Genome.Potency.ToString("0"));
                item.ForeColor = !h.IsAlive ? Color.FromArgb(145, 95, 85) : h.IsLeader ? Color.FromArgb(255, 215, 75) : h.Stage == LifeStage.Baby ? Color.FromArgb(245, 215, 120) : DstColors.Parchment;
                lvHumans.Items.Add(item);
            }
            lblRegistryStats.Text = $"Active: {alive}  |  Ancestral Graves: {dead}";
            lvHumans.EndUpdate();
        }

        void ToggleSimulation()
        {
            simTimer.Enabled = !simTimer.Enabled;
            btnPlayPause.Text = simTimer.Enabled ? "⏸ PAUSE" : "▶ SIMULATE";
            btnPlayPause.ForeColor = simTimer.Enabled ? DstColors.AccentRed : DstColors.AccentGold;
        }

        void SimTimer_Tick(object sender, EventArgs e)
        {
            double step = 0.05 * speedMultiplier;
            fractionalHour += step;

            if (fractionalHour >= 24.0)
            {
                fractionalHour -= 24.0;
                int maxD = FuzzyEngine.GetDaysInMonth(sldMonth.Value), nextD = sldDay.Value + 1;
                if (nextD > maxD) { sldMonth.Value = (sldMonth.Value % 12) + 1; UpdateDayBounds(); nextD = 1; }
                sldDay.Value = nextD;
            }

            // Sync visual time slider smoothly without interrupting user interaction
            int currentHourInt = (int)Math.Floor(fractionalHour);
            if (sldTime.Value != currentHourInt)
            {
                sldTime.Value = currentHourInt;
            }

            int m = sldMonth.Value, d = Math.Min(sldDay.Value, FuzzyEngine.GetDaysInMonth(m));
            StepWeather(step, FuzzyEngine.Infer(FuzzyEngine.DayOfYear(m, d), fractionalHour, sldRain.Value));
            Recalculate(step / 24.0 / 365.0 * (speedMultiplier * 2.0));
        }

        void StepWeather(double step, SceneOutputs o)
        {
            if (!chkAutoWeather.Checked) return;
            if ((weatherDurationHours -= step) <= 0)
            {
                isStormFront = !isStormFront;
                targetRain = !isStormFront ? (weatherRng.NextDouble() < 0.25 ? weatherRng.Next(3, 8) : 0) :
                             weatherRng.NextDouble() < (o.Season.Winter * 0.55) ? weatherRng.Next(60, 95) :
                             weatherRng.NextDouble() < (o.Season.Spring * 0.50) ? weatherRng.Next(20, 50) : weatherRng.Next(15, 40);
                weatherDurationHours = weatherRng.Next(8, 24);
            }
            currentRainFloat += (targetRain - currentRainFloat) * 0.08 * Math.Min(1.0, step * 4.0);
            sldRain.Value = (int)Math.Round(currentRainFloat);
        }

        void CmbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPresets.SelectedIndex <= 0) return;

            int[] months = { 0, 7, 1, 4, 10 }, days = { 0, 15, 10, 20, 25 }, hours = { 0, 12, 22, 7, 18 }, rains = { 0, 0, 85, 45, 25 };
            int idx = cmbPresets.SelectedIndex;
            sldMonth.Value = months[idx];
            sldDay.Value = days[idx];
            sldTime.Value = hours[idx];
            sldRain.Value = rains[idx];
            fractionalHour = sldTime.Value;
            currentRainFloat = sldRain.Value;
            targetRain = sldRain.Value;

            UpdateDayBounds();
            Recalculate();
        }

        void ShowModalFullscreen(string title, Control view)
        {
            Control parent = view.Parent; DockStyle dock = view.Dock;
            Form modal = new Form { Text = title + " - [Press ESC to Return]", Size = new Size(1100, 780), StartPosition = FormStartPosition.CenterParent, BackColor = DstColors.Background, KeyPreview = true };
            Panel head = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = DstColors.CardBg };
            Button close = new Button { Text = "✕ CLOSE", Dock = DockStyle.Right, Width = 90, FlatStyle = FlatStyle.Flat, ForeColor = DstColors.AccentRed, Font = new Font("Georgia", 8f, FontStyle.Bold) };
            close.Click += (s, e) => modal.Close();
            head.Controls.Add(new Label { Text = title.ToUpper() + " (FULLSCREEN INSPECTOR)", Dock = DockStyle.Left, ForeColor = DstColors.Parchment, Font = new Font("Georgia", 9f, FontStyle.Bold), Padding = new Padding(12, 6, 0, 0), Width = 600 });
            head.Controls.Add(close);
            modal.Controls.Add(view); modal.Controls.Add(head);
            view.Dock = DockStyle.Fill; view.BringToFront();
            modal.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) modal.Close(); };
            modal.FormClosed += (s, e) => { parent.Controls.Add(view); view.Dock = dock; view.Invalidate(); };
            modal.ShowDialog(this);
        }

        void UpdateDayBounds() => sldDay.Maximum = FuzzyEngine.GetDaysInMonth(sldMonth.Value);

        void Recalculate(double deltaYears = 0.0)
        {
            if (outBars == null || seasonGraph == null) return;
            int m = sldMonth.Value, d = Math.Min(sldDay.Value, FuzzyEngine.GetDaysInMonth(m)), doy = FuzzyEngine.DayOfYear(m, d);
            double h = simTimer.Enabled ? fractionalHour : sldTime.Value;

            lblDoy.Text = $"Calendar: {MonthNames[m - 1]} {d}  |  Day: {doy}  |  Year {SocietyEngine.CurrentYear:0}";

            // Protect against recursive scrub feedback loops
            if (!isInternalYearUpdate && sldYearScrub.Value != (int)SocietyEngine.CurrentYear && (int)SocietyEngine.CurrentYear <= sldYearScrub.Maximum)
            {
                isInternalYearUpdate = true;
                sldYearScrub.Value = (int)SocietyEngine.CurrentYear;
                isInternalYearUpdate = false;
            }

            SceneOutputs o = FuzzyEngine.Infer(doy, h, sldRain.Value);
            SocietyOutputs soc = SocietyEngine.Step(o, h, deltaYears);

            seasonGraph.CurrentX = doy;
            seasonGraph.Sets[0].CurrentValue = o.Season.Winter; seasonGraph.Sets[1].CurrentValue = o.Season.Spring;
            seasonGraph.Sets[2].CurrentValue = o.Season.Summer; seasonGraph.Sets[3].CurrentValue = o.Season.Autumn;
            seasonGraph.Invalidate();

            timeGraph.CurrentX = h;
            timeGraph.Sets[0].CurrentValue = o.Time.Night; timeGraph.Sets[1].CurrentValue = o.Time.Dawn;
            timeGraph.Sets[2].CurrentValue = o.Time.Day; timeGraph.Sets[3].CurrentValue = o.Time.Dusk;
            timeGraph.Invalidate();

            lstActiveRules.BeginUpdate();
            lstActiveRules.Items.Clear();
            foreach (var r in o.ActiveRules) lstActiveRules.Items.Add($"[w={r.Weight:0.00}] {r.Name} -> {r.ConsequentDescription}");
            lstActiveRules.EndUpdate();

            double[] vals = { o.SkyBrightness, o.CloudCover, o.Stars, o.Rain, o.Snow, o.Foliage, o.GroundSnow };
            for (int i = 0; i < vals.Length; i++) outBars[i].SetValue(vals[i]);

            sceneView.Outputs = o; sceneView.Society = soc; sceneView.Hour = h; sceneView.Invalidate();
            surfaceView.UpdateState(sldRain.Value, doy, h);

            if (isMonitoringHumans) UpdateHumanRegistryTable();
        }
    }
}