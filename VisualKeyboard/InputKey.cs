using System;
using System.Windows.Forms;

namespace VisualKeyboard
{
    class InputKey : TextBox
    {
        public InputKey(String keyLabel)
        {
            this.BackColor = System.Drawing.Color.Yellow;
            this.BorderStyle = BorderStyle.None;
            this.Enabled = false;
            this.Location = new System.Drawing.Point(12, 12);
            this.MinimumSize = new System.Drawing.Size(30, 30);
            this.Name = "InputKey_" + keyLabel;
            this.ReadOnly = true;
            this.Size = new System.Drawing.Size(30, 30);
            this.TabIndex = 0;
            this.Text = keyLabel;
            this.TextAlign = HorizontalAlignment.Center;
        }
    }
}
