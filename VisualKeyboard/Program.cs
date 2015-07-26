using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Diagnostics;
using VisualKeyboard.Properties;

namespace VisualKeyboard
{
    public class KeyboardListener
    {
        public static event EventHandler<Keys> inputEvent;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = hookProc;
        private static IntPtr hhook = IntPtr.Zero;

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYDOWN = 0x100;
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                KeyboardListener.inputEvent(null, (Keys)Marshal.ReadInt32(lParam));
            }
            return CallNextHookEx(hhook, code, (int)wParam, lParam);
        }

        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }

        static KeyboardListener()
        {
            const int WH_KEYBOARD_LL = 13;
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, LoadLibrary("User32"), 0);
        }
    }

    public static class MouseInput
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public delegate void EventHandler(object sender, MouseEventArgs e);

        public static EventHandler DragWindowFor(IntPtr handle)
        {
            return (object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
        }

    }

    public partial class MainWindow : Form
    {
        private readonly IDisposable unsubscribe;

        public MainWindow(IEnumerable<IEnumerable<Keys>> layoutConfig)
        {
            IEnumerable<IEnumerable<InputKey>> layout = layoutConfig
                .Select(row => row.Select(key => new InputKey(key)).ToList())
                .ToList();

            Dictionary<Keys, InputKey> keyLookup = layout
                .SelectMany(row => row.Select(inputKey => new { inputKey.Key, inputKey }))
                .ToDictionary(entry => entry.Key, entry => entry.inputKey);

            InitializeComponent(layout);

            unsubscribe = Observable.FromEventPattern<Keys>(ev => KeyboardListener.inputEvent += ev, ev => KeyboardListener.inputEvent -= ev)
                .Select(ev => ev.EventArgs)
                .Do(key =>
                {
                    if (key == Keys.Escape)
                    {
                        Application.Exit();
                    }
                    if (keyLookup.ContainsKey(key))
                    {
                        keyLookup[key].Trigger();
                    }
                })
                .SubscribeOn(NewThreadScheduler.Default).Subscribe();
        }

        protected override void Dispose(bool disposing)
        {
            unsubscribe.Dispose();
            KeyboardListener.UnHook();
            base.Dispose(disposing);
        }

        private static Panel buildLayoutPanel(IEnumerable<IEnumerable<InputKey>> keyLayout)
        {
            FlowLayoutPanel columnPanel = new FlowLayoutPanel();
            columnPanel.FlowDirection = FlowDirection.TopDown;
            columnPanel.SuspendLayout();
            columnPanel.AutoSize = true;
            foreach (IEnumerable<InputKey> row in keyLayout)
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

        private void InitializeComponent(IEnumerable<IEnumerable<InputKey>> layout)
        {
            Controls.Add(buildLayoutPanel(layout));
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            MouseDown += new MouseEventHandler(MouseInput.DragWindowFor(Handle));
            AutoSize = true;
            ResumeLayout(false);
            PerformLayout();
        }
    }

    static class Program
    {
        private static Keys ParseKey(string key)
        {
            return (Keys)Enum.Parse(typeof(Keys), key.ToUpper());
        }

        private static IEnumerable<IEnumerable<Keys>> ParseKeyConfig(string configString)
        {
            return configString
                .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(row => row.Split(' ').Select(ParseKey));
        }

        [STAThread]
        static void Main()
        {
            string layoutConfig = System.Text.Encoding.Default.GetString(Resources.DefaultLayout);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow(ParseKeyConfig(layoutConfig)));
        }
    }
}
