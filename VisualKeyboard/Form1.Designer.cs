using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System;
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

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

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
            keyConfig.Add(new List<Keys> { Keys.A, Keys.B });
            keyConfig.Add(new List<Keys> { Keys.C, Keys.D, Keys.E });

            List<List<InputKey>> layout = keyConfig.Select(row =>
            {
                return row.Select(key =>
                {
                    // Alt = 1, Ctrl = 2, Shift = 4, Win = 8
                    RegisterHotKey(Handle, (int)key, 0, (int)key);
                    InputKey inputKey = new InputKey(key);
                    inputKeys.Add(key, inputKey);
                    return inputKey;
                }).ToList();
            }).ToList();

            this.Controls.Add(buildLayoutPanel(layout));
            this.SuspendLayout();

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form1";
            this.Text = "Form1";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDown);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Dictionary<Keys, InputKey> inputKeys = new Dictionary<Keys, InputKey>();
        private List<List<Keys>> keyLayout = new List<List<Keys>>();
    }
}

