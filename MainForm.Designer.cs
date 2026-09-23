using System.Drawing;
using System.Windows.Forms;

namespace SeasonFuzzy
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sceneCard = new SeasonFuzzy.DstCard();
            this.sceneView = new SeasonFuzzy.SceneView();
            this.inputsCard = new SeasonFuzzy.DstCard();
            this.seasonCard = new SeasonFuzzy.DstCard();
            this.seasonGraph = new SeasonFuzzy.MembershipGraph();
            this.timeCard = new SeasonFuzzy.DstCard();
            this.timeGraph = new SeasonFuzzy.MembershipGraph();
            this.surfaceCard = new SeasonFuzzy.DstCard();
            this.surfaceView = new SeasonFuzzy.SurfaceView();
            this.rulesCard = new SeasonFuzzy.DstCard();
            this.lstActiveRules = new System.Windows.Forms.ListBox();
            this.outputsCard = new SeasonFuzzy.DstCard();
            this.cmbSurfaceVar = new System.Windows.Forms.ComboBox();
            this.sldMonth = new SeasonFuzzy.FlatSlider();
            this.sldDay = new SeasonFuzzy.FlatSlider();
            this.sldTime = new SeasonFuzzy.FlatSlider();
            this.sldRain = new SeasonFuzzy.FlatSlider();
            this.lblDoy = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // mainTableLayout
            // 
            this.mainTableLayout.ColumnCount = 3;
            this.mainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.mainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.mainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.mainTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayout.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayout.Name = "mainTableLayout";
            this.mainTableLayout.Padding = new System.Windows.Forms.Padding(8);
            this.mainTableLayout.RowCount = 3;
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 425F)); // Increased from 360F
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayout.Size = new System.Drawing.Size(1180, 920);
            this.mainTableLayout.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1180, 920);
            this.Controls.Add(this.mainTableLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 840);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Project Equinox: Dynamic Biome Simulator (Zero-Order Sugeno FIS)";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel mainTableLayout;
        private SeasonFuzzy.DstCard sceneCard;
        private SeasonFuzzy.SceneView sceneView;
        private SeasonFuzzy.DstCard inputsCard;
        private SeasonFuzzy.DstCard seasonCard;
        private SeasonFuzzy.MembershipGraph seasonGraph;
        private SeasonFuzzy.DstCard timeCard;
        private SeasonFuzzy.MembershipGraph timeGraph;
        private SeasonFuzzy.DstCard surfaceCard;
        private SeasonFuzzy.SurfaceView surfaceView;
        private SeasonFuzzy.DstCard rulesCard;
        private System.Windows.Forms.ListBox lstActiveRules;
        private SeasonFuzzy.DstCard outputsCard;
        private System.Windows.Forms.ComboBox cmbSurfaceVar;
        private SeasonFuzzy.FlatSlider sldMonth;
        private SeasonFuzzy.FlatSlider sldDay;
        private SeasonFuzzy.FlatSlider sldTime;
        private SeasonFuzzy.FlatSlider sldRain;
        private System.Windows.Forms.Label lblDoy;
    }
}