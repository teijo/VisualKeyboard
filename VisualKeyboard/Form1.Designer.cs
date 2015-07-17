using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System;

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

        private void createInputKey(Keys key)
        {
            // Alt = 1, Ctrl = 2, Shift = 4, Win = 8
            RegisterHotKey(this.Handle, (int)key, 0, (int)key);

            InputKey inputKey = new InputKey(key);
            this.Controls.Add(inputKey);
            inputKeys.Add(key, inputKey);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            createInputKey(Keys.A);
            createInputKey(Keys.B);

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
    }
}

