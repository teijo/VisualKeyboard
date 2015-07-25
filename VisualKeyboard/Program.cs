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
        public MainWindow()
        {
            unsubscribe = Observable.FromEventPattern<Keys>(ev => KeyboardListener.inputEvent += ev, ev => KeyboardListener.inputEvent -= ev)
                .Select(ev => ev.EventArgs)
                .Do(key =>
                {
                    if (key == Keys.Escape)
                    {
                        Application.Exit();
                    }
                    if (inputKeys.ContainsKey(key))
                    {
                        inputKeys[key].Trigger();
                    }
                })
                .SubscribeOn(NewThreadScheduler.Default).Subscribe();
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            unsubscribe.Dispose();
            KeyboardListener.UnHook();
            base.Dispose(disposing);
        }

        private static Panel buildLayoutPanel(List<List<InputKey>> keyLayout)
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

        private static List<List<Keys>> ParseKeyConfig(string configString)
        {
            List<string> keyRows = configString.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            List<List<Keys>> keyConfig = new List<List<Keys>>();
            foreach (string row in keyRows)
            {
                List<Keys> keys = row.Split(' ').ToList().Select(k => (Keys)Enum.Parse(typeof(Keys), k.ToUpper())).ToList();
                keyConfig.Add(keys);
            }
            return keyConfig;
        }

        private void InitializeComponent()
        {
            string layoutConfig = System.Text.Encoding.Default.GetString(Resources.DefaultLayout);

            List<List<InputKey>> layout = ParseKeyConfig(layoutConfig).Select(row =>
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

            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            MouseDown += new MouseEventHandler(MouseInput.DragWindowFor(Handle));
            AutoSize = true;
            ResumeLayout(false);
            PerformLayout();
        }

        private static Dictionary<Keys, InputKey> inputKeys = new Dictionary<Keys, InputKey>();
        private List<List<Keys>> keyLayout = new List<List<Keys>>();
        private IDisposable unsubscribe;
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
