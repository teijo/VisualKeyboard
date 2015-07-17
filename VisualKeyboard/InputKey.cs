using System;
using System.Drawing;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace VisualKeyboard
{
    class InputKey : TextBox
    {
        public InputKey(Keys key)
        {
            BackColor = Color.Yellow;
            BorderStyle = BorderStyle.None;
            Enabled = false;
            Dock = DockStyle.Left;
            MinimumSize = new Size(30, 30);
            Text = Enum.GetName(key.GetType(), key);
            ReadOnly = true;
            Size = new Size(30, 30);
            TextAlign = HorizontalAlignment.Center;
        }

        internal void Flash()
        {
            BackColor = Color.Red;

            var timer = new Timer();
            timer.Tick += (s, e) =>
            {
                ((Timer)s).Stop();
                BackColor = Color.Yellow;
            };
            timer.Interval = 1000;
            timer.Start();
        }
    }
}
