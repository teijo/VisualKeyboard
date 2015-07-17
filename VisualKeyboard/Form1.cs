using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace VisualKeyboard
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        const int KEY_ID = 1;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        public Form1()
        {
            InitializeComponent();
            // Alt = 1, Ctrl = 2, Shift = 4, Win = 8
            RegisterHotKey(this.Handle, KEY_ID, 0, (int)Keys.A);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == KEY_ID)
            {
                textBox1.BackColor = System.Drawing.Color.Red;
                var timer = new Timer();
                timer.Tick += (s, e) => {
                    ((Timer)s).Stop();
                    textBox1.BackColor = System.Drawing.Color.Yellow;
                };
                timer.Interval = 1000;
                timer.Start();
            }
            base.WndProc(ref m);
        }
    }
}
