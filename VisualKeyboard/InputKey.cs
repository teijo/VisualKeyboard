using System;
using System.Windows.Forms;

namespace VisualKeyboard
{
    class InputKey : TextBox
    {
        public InputKey(Keys key)
        {
            this.BackColor = System.Drawing.Color.Yellow;
            this.BorderStyle = BorderStyle.None;
            this.Enabled = false;
            //this.Location = new System.Drawing.Point(12, 12);
            this.Dock = DockStyle.Left;
            this.MinimumSize = new System.Drawing.Size(30, 30);
            this.Text = Enum.GetName(key.GetType(), key);
            this.Name = "InputKey_" + this.Text;
            this.ReadOnly = true;
            this.Size = new System.Drawing.Size(30, 30);
            this.TabIndex = 0;
            this.TextAlign = HorizontalAlignment.Center;
        }

        internal void Flash()
        {
            this.BackColor = System.Drawing.Color.Red;

            var timer = new Timer();
            timer.Tick += (s, e) =>
            {
                ((Timer)s).Stop();
                this.BackColor = System.Drawing.Color.Yellow;
            };
            timer.Interval = 1000;
            timer.Start();
        }
    }
}
