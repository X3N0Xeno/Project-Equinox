using System;
using System.Drawing;
using System.Windows.Forms;

namespace SeasonFuzzy
{
    public partial class MainForm
    {
        void BuildUiContents()
        {
            BackColor = DstColors.Background;

            sceneCard.SetTitle("Scene Preview (Environment Simulator)", DstColors.AccentGold, true);
            sceneCard.Dock = DockStyle.Fill;
            sceneView.Dock = DockStyle.Fill;
            sceneCard.Content.Controls.Add(sceneView);

            sceneView.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    float scale = Math.Min(sceneView.Width / 640f, sceneView.Height / 300f);
                    float simX = (e.X - (sceneView.Width - 640 * scale) / 2f) / scale;
                    float simY = (e.Y - (sceneView.Height - 300 * scale) / 2f) / scale;

                    if (simX >= 12 && simX <= 280 && simY >= 12 && simY <= 38)
                    {
                        SocietyEngine.IsFeedMinimized = !SocietyEngine.IsFeedMinimized;
                        sceneView.Invalidate();
                        return;
                    }

                    int clickedId = -1;
                    foreach (var node in SocietyEngine.Settlements)
                    {
                        if (!node.IsEstablished) continue;
                        float ny = 248f + (float)Math.Sin(node.X / 640f * Math.PI) * 6f;
                        if (Math.Abs(simX - node.X) < 40 && Math.Abs(simY - ny) < 40)
                        {
                            clickedId = node.Id;
                            break;
                        }
                    }
                    SocietyEngine.SelectedSettlementId = (SocietyEngine.SelectedSettlementId == clickedId) ? -1 : clickedId;
                    sceneView.Invalidate();
                }
            };

            sceneCard.MaximizeClicked += (s, e) => ShowModalFullscreen("Civilization Preview", sceneView);
            mainTableLayout.Controls.Add(sceneCard, 0, 0);
            mainTableLayout.SetColumnSpan(sceneCard, 2);

            inputsCard.SetTitle("System & Timeline Controls", DstColors.AccentRed);
            inputsCard.Dock = DockStyle.Fill;
            inputsCard.Content.AutoScroll = false;
            inputsCard.Content.Padding = new Padding(6, 4, 6, 4);

            sldMonth.Minimum = 1; sldMonth.Maximum = 12;
            sldDay.Minimum = 1; sldDay.Maximum = 31;
            sldTime.Minimum = 0; sldTime.Maximum = 23;
            sldRain.Minimum = 0; sldRain.Maximum = 100;

            sldMonth.LabelFormatter = v => MonthNames[v - 1];
            sldDay.LabelFormatter = v => "Day " + v;
            sldTime.LabelFormatter = v => string.Format("{0:00}:00", v);
            sldRain.LabelFormatter = v => v == 0 ? "0 mm" : v + " mm";

            // Live event listeners: allows adjustments whether paused or simulating
            sldMonth.ValueChanged += (s, e) => { UpdateDayBounds(); Recalculate(); };
            sldDay.ValueChanged += (s, e) => Recalculate();
            sldTime.ValueChanged += (s, e) => { fractionalHour = sldTime.Value; Recalculate(); };
            sldRain.ValueChanged += (s, e) => { currentRainFloat = sldRain.Value; targetRain = sldRain.Value; Recalculate(); };

            TableLayoutPanel ctrlStack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            ctrlStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            ctrlStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            ctrlStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            ctrlStack.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));
            ctrlStack.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));
            ctrlStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));

            ctrlStack.Controls.Add(BuildSimControlRow(), 0, 0);
            ctrlStack.Controls.Add(BuildViewToggleRow(), 0, 1);
            ctrlStack.Controls.Add(BuildPresetRow(), 0, 2);
            ctrlStack.Controls.Add(BuildEnvironmentGrid(), 0, 3);
            ctrlStack.Controls.Add(BuildTimelinePanel(), 0, 4);

            lblDoy.Dock = DockStyle.Fill;
            lblDoy.TextAlign = ContentAlignment.MiddleCenter;
            lblDoy.ForeColor = DstColors.Parchment;
            lblDoy.Font = new Font("Georgia", 8f, FontStyle.Italic);
            ctrlStack.Controls.Add(lblDoy, 0, 5);

            inputsCard.Content.Controls.Add(ctrlStack);
            mainTableLayout.Controls.Add(inputsCard, 2, 0);

            BuildMembershipCards();
            BuildSurfaceAndOutputs();
        }

        void BuildMembershipCards()
        {
            seasonCard.SetTitle("Season Membership Functions", DstColors.Parchment, true);
            seasonCard.Dock = DockStyle.Fill;
            seasonGraph.Dock = DockStyle.Fill;
            seasonGraph.MinX = 0; seasonGraph.MaxX = 365;

            int sSteps = 73;
            PointF[] pW = new PointF[sSteps + 1], pSp = new PointF[sSteps + 1], pSu = new PointF[sSteps + 1], pA = new PointF[sSteps + 1];
            for (int i = 0; i <= sSteps; i++)
            {
                int d = (int)(i * (365.0 / sSteps));
                var m = FuzzyEngine.FuzzifySeason(d);
                pW[i] = new PointF(d, (float)m.Winter);
                pSp[i] = new PointF(d, (float)m.Spring);
                pSu[i] = new PointF(d, (float)m.Summer);
                pA[i] = new PointF(d, (float)m.Autumn);
            }
            seasonGraph.Sets.Clear();
            seasonGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Winter", Color = Color.FromArgb(145, 170, 195), Points = pW, CustomLabelPos = new PointF(15, 0.95f) });
            seasonGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Spring", Color = Color.FromArgb(120, 160, 95), Points = pSp, CustomLabelPos = new PointF(105, 0.95f) });
            seasonGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Summer", Color = Color.FromArgb(205, 130, 45), Points = pSu, CustomLabelPos = new PointF(196, 0.95f) });
            seasonGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Autumn", Color = Color.FromArgb(165, 75, 55), Points = pA, CustomLabelPos = new PointF(288, 0.95f) });
            seasonCard.Content.Controls.Add(seasonGraph);
            seasonCard.MaximizeClicked += (s, e) => ShowModalFullscreen("Season Membership", seasonGraph);
            mainTableLayout.Controls.Add(seasonCard, 0, 1);

            timeCard.SetTitle("Time of Day (24H)", DstColors.Parchment, true);
            timeCard.Dock = DockStyle.Fill;
            timeGraph.Dock = DockStyle.Fill;
            timeGraph.MinX = 0; timeGraph.MaxX = 24;

            int tSteps = 48;
            PointF[] pN = new PointF[tSteps + 1], pDw = new PointF[tSteps + 1], pDy = new PointF[tSteps + 1], pDk = new PointF[tSteps + 1];
            for (int i = 0; i <= tSteps; i++)
            {
                double h = i * (24.0 / tSteps);
                var m = FuzzyEngine.FuzzifyTime(h);
                pN[i] = new PointF((float)h, (float)m.Night);
                pDw[i] = new PointF((float)h, (float)m.Dawn);
                pDy[i] = new PointF((float)h, (float)m.Day);
                pDk[i] = new PointF((float)h, (float)m.Dusk);
            }
            timeGraph.Sets.Clear();
            timeGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Night", Color = Color.FromArgb(140, 135, 185), Points = pN, CustomLabelPos = new PointF(1.0f, 0.95f) });
            timeGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Dawn", Color = Color.FromArgb(195, 115, 95), Points = pDw, CustomLabelPos = new PointF(6.0f, 0.95f) });
            timeGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Day", Color = Color.FromArgb(205, 160, 60), Points = pDy, CustomLabelPos = new PointF(12.5f, 0.95f) });
            timeGraph.Sets.Add(new MembershipGraph.SetDef { Name = "Dusk", Color = Color.FromArgb(170, 95, 140), Points = pDk, CustomLabelPos = new PointF(18.5f, 0.95f) });
            timeCard.Content.Controls.Add(timeGraph);
            timeCard.MaximizeClicked += (s, e) => ShowModalFullscreen("Time Membership", timeGraph);
            mainTableLayout.Controls.Add(timeCard, 1, 1);
        }

        void BuildSurfaceAndOutputs()
        {
            surfaceCard.SetTitle("3D Control Surface", DstColors.AccentGold, true);
            surfaceCard.Dock = DockStyle.Fill;
            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 22 };
            Label lblPick = new Label { Text = "Z Axis:", Dock = DockStyle.Left, Width = 50, ForeColor = DstColors.Parchment, Font = new Font("Georgia", 8f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            cmbSurfaceVar.Dock = DockStyle.Fill; cmbSurfaceVar.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSurfaceVar.BackColor = DstColors.CardBg; cmbSurfaceVar.ForeColor = DstColors.Parchment;
            cmbSurfaceVar.Items.AddRange(new object[] { "Brightness", "Cloud Cover", "Foliage", "Ground Snow", "Precipitation" });
            cmbSurfaceVar.SelectedIndex = 0;
            cmbSurfaceVar.SelectedIndexChanged += (s, e) => { surfaceView.SelectedMetric = cmbSurfaceVar.SelectedItem.ToString(); Recalculate(); };
            topBar.Controls.Add(cmbSurfaceVar); topBar.Controls.Add(lblPick);
            surfaceView.Dock = DockStyle.Fill;
            surfaceCard.Content.Controls.Add(surfaceView); surfaceCard.Content.Controls.Add(topBar);
            surfaceCard.MaximizeClicked += (s, e) => ShowModalFullscreen("3D Surface", surfaceView);
            mainTableLayout.Controls.Add(surfaceCard, 2, 1);

            rulesCard.SetTitle("Fired Sugeno Rules", DstColors.AccentGold);
            rulesCard.Dock = DockStyle.Fill;
            lstActiveRules.Dock = DockStyle.Fill; lstActiveRules.BackColor = Color.FromArgb(24, 20, 16);
            lstActiveRules.ForeColor = DstColors.Parchment; lstActiveRules.Font = new Font("Consolas", 8.25f);
            lstActiveRules.BorderStyle = BorderStyle.None;
            rulesCard.Content.Controls.Add(lstActiveRules);
            mainTableLayout.Controls.Add(rulesCard, 0, 2);

            outputsCard.SetTitle("Defuzzified Outputs", DstColors.AccentGold);
            outputsCard.Dock = DockStyle.Fill;
            string[] names = { "Sky brightness", "Cloud cover", "Stars", "Rain", "Snow", "Foliage", "Ground snow" };
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
            for (int c = 0; c < 4; c++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            for (int r = 0; r < 2; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            outBars = new BarRow[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                outBars[i] = new BarRow(names[i]) { Dock = DockStyle.Fill, Margin = new Padding(3) };
                grid.Controls.Add(outBars[i], i % 4, i / 4);
            }
            outputsCard.Content.Controls.Add(grid);
            mainTableLayout.Controls.Add(outputsCard, 1, 2);
            mainTableLayout.SetColumnSpan(outputsCard, 2);
        }

        Control BuildSimControlRow()
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            btnPlayPause = new Button { Text = "▶ SIMULATE", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 30, 24), ForeColor = DstColors.AccentGold, Font = new Font("Georgia", 8.25f, FontStyle.Bold) };
            btnPlayPause.Click += (s, e) => ToggleSimulation();

            btnResetSim = new Button { Text = "↺ RESET", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 30, 24), ForeColor = DstColors.AccentRed, Font = new Font("Georgia", 8.25f, FontStyle.Bold) };
            btnResetSim.Click += (s, e) => { SocietyEngine.ResetSettlement(); Recalculate(); };

            cmbSimSpeed = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 26, 20), ForeColor = DstColors.Parchment };
            cmbSimSpeed.Items.AddRange(new object[] { "1x Realtime", "5x Fast", "15x Fast-Fwd", "50x Hyperspeed" });
            cmbSimSpeed.SelectedIndex = 1;
            cmbSimSpeed.SelectedIndexChanged += (s, e) => speedMultiplier = cmbSimSpeed.SelectedIndex == 0 ? 0.2 : cmbSimSpeed.SelectedIndex == 1 ? 1.0 : cmbSimSpeed.SelectedIndex == 2 ? 3.5 : 10.0;

            row.Controls.Add(btnPlayPause, 0, 0); row.Controls.Add(btnResetSim, 1, 0); row.Controls.Add(cmbSimSpeed, 2, 0);
            return row;
        }

        Control BuildViewToggleRow()
        {
            btnToggleView = new Button { Text = "👥 VIEW HUMAN PEDIGREE REGISTRY", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(42, 34, 26), ForeColor = DstColors.AccentGold, Font = new Font("Georgia", 8.25f, FontStyle.Bold) };
            btnToggleView.Click += (s, e) => ToggleInspectorView();
            return btnToggleView;
        }

        Control BuildPresetRow()
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            cmbPresets = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 26, 20), ForeColor = DstColors.Parchment };
            cmbPresets.Items.AddRange(new object[] { "-- Scenario Presets --", "Summer Heatwave", "Winter Blizzard", "Spring Showers", "Autumn Drizzle" });
            cmbPresets.SelectedIndex = 0;
            cmbPresets.SelectedIndexChanged += CmbPresets_SelectedIndexChanged;

            chkAutoWeather = new CheckBox { Text = "Auto Weather", Dock = DockStyle.Fill, ForeColor = DstColors.Parchment, Checked = true };
            row.Controls.Add(cmbPresets, 0, 0); row.Controls.Add(chkAutoWeather, 1, 0);
            return row;
        }

        Control BuildEnvironmentGrid()
        {
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.Controls.Add(MakeSliderBlock("MONTH", sldMonth), 0, 0);
            grid.Controls.Add(MakeSliderBlock("DAY", sldDay), 1, 0);
            grid.Controls.Add(MakeSliderBlock("TIME (24H)", sldTime), 0, 1);
            grid.Controls.Add(MakeSliderBlock("RAINFALL", sldRain), 1, 1);
            return grid;
        }

        Control BuildTimelinePanel()
        {
            Panel p = new Panel { Dock = DockStyle.Fill };
            Label lbl = new Label { Text = "TIMELINE FAST-FORWARD / REWIND", Dock = DockStyle.Top, Height = 15, ForeColor = DstColors.AccentGold, Font = new Font("Georgia", 8f, FontStyle.Bold) };
            sldYearScrub = new FlatSlider { Dock = DockStyle.Fill, Minimum = 1, Maximum = 2000, Value = 1 };
            sldYearScrub.LabelFormatter = v => $"Year {v}";

            sldYearScrub.ValueChanged += (s, e) =>
            {
                if (isInternalYearUpdate) return;

                double delta = sldYearScrub.Value - SocietyEngine.CurrentYear;
                if (delta > 0)
                {
                    int curM = sldMonth.Value, curD = Math.Min(sldDay.Value, FuzzyEngine.GetDaysInMonth(curM));
                    SocietyEngine.FastForwardYears(delta, FuzzyEngine.Infer(FuzzyEngine.DayOfYear(curM, curD), fractionalHour, sldRain.Value));
                }
                else if (delta < 0)
                {
                    SocietyEngine.CurrentYear = sldYearScrub.Value;
                    SocietyEngine.PerceivedEraProgress = Math.Max(0.0, (sldYearScrub.Value >= 1400 ? 3.0 : sldYearScrub.Value >= 700 ? 2.0 : sldYearScrub.Value >= 250 ? 1.0 : 0.0));
                }
                Recalculate();
            };

            p.Controls.Add(sldYearScrub); p.Controls.Add(lbl);
            return p;
        }

        Control MakeSliderBlock(string caption, FlatSlider slider)
        {
            Panel block = new Panel { Dock = DockStyle.Fill, Margin = new Padding(2) };
            Label cap = new Label { Text = caption, Dock = DockStyle.Top, Height = 14, ForeColor = DstColors.Parchment, Font = new Font("Georgia", 7.5f, FontStyle.Bold) };
            slider.Dock = DockStyle.Fill;
            block.Controls.Add(slider); block.Controls.Add(cap);
            return block;
        }

        void BuildHumanRegistryPanel()
        {
            pnlHumanRegistry = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 16, 12), Visible = false };
            Panel head = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(34, 26, 20) };
            head.Controls.Add(lblRegistryStats = new Label { Text = "Active: 0 | Ancestral Graves: 0", Dock = DockStyle.Right, ForeColor = DstColors.Parchment, Width = 260, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 12, 0) });
            head.Controls.Add(new Label { Text = "CITIZEN & PEDIGREE REGISTRY", Dock = DockStyle.Left, ForeColor = DstColors.AccentGold, Font = new Font("Georgia", 9f, FontStyle.Bold), Width = 360, Padding = new Padding(8, 8, 0, 0) });

            lvHumans = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BackColor = Color.FromArgb(24, 20, 16), ForeColor = DstColors.Parchment, Font = new Font("Consolas", 8.5f), BorderStyle = BorderStyle.None };
            lvHumans.Columns.Add("ID", 40); lvHumans.Columns.Add("Name", 120); lvHumans.Columns.Add("Sex", 40); lvHumans.Columns.Add("Age", 55);
            lvHumans.Columns.Add("Status / Cause", 110); lvHumans.Columns.Add("Parents (Dad / Mom)", 180);
            lvHumans.Columns.Add("HP", 45); lvHumans.Columns.Add("INT", 45); lvHumans.Columns.Add("STR", 45); lvHumans.Columns.Add("AGI", 45); lvHumans.Columns.Add("POT", 45);

            pnlHumanRegistry.Controls.Add(lvHumans); pnlHumanRegistry.Controls.Add(head);
        }
    }
}