using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

namespace VisualKeyboard
{
    partial class Form1
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

        private Panel buildLayoutPanel(List<List<InputKey>> keyLayout)
        {
            FlowLayoutPanel columnPanel = new FlowLayoutPanel();
            columnPanel.FlowDirection = FlowDirection.TopDown;
            columnPanel.SuspendLayout();
            columnPanel.AutoSize = true;
            foreach (List<InputKey> row in keyLayout)
            {
                FlowLayoutPanel rowPanel = new FlowLayoutPanel();
                rowPanel.FlowDirection = FlowDirection.LeftToRight;
                rowPanel.BackColor = System.Drawing.Color.Pink;
                rowPanel.SuspendLayout();
                rowPanel.AutoSize = true;
                foreach (InputKey input in row)
                {
                    rowPanel.Controls.Add(input);
                }
                rowPanel.ResumeLayout();
                columnPanel.Controls.Add(rowPanel);
            }
            columnPanel.ResumeLayout();
            return columnPanel;
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            List<List<Keys>> keyConfig = new List<List<Keys>>();
            keyConfig.Add(new List<Keys> { Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P });
            keyConfig.Add(new List<Keys> { Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L });
            keyConfig.Add(new List<Keys> { Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M });

            List<List<InputKey>> layout = keyConfig.Select(row =>
            {
                return row.Select(key =>
                {
                    InputKey inputKey = new InputKey(key);
                    inputKeys.Add(key, inputKey);
                    return inputKey;
                }).ToList();
            }).ToList();

            Controls.Add(buildLayoutPanel(layout));
            SuspendLayout();

            // 
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.None;
            Name = "Form1";
            Text = "Form1";
            TopMost = true;
            MouseDown += new MouseEventHandler(Form1_MouseDown);
            AutoSize = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private static Dictionary<Keys, InputKey> inputKeys = new Dictionary<Keys, InputKey>();
        private List<List<Keys>> keyLayout = new List<List<Keys>>();
    }
}

