using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VisualKeyboard.Properties;
using Color = System.Drawing.Color;

static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
}

static class KeyboardListener
{
    public static event EventHandler<Keys> InputEvent;

    private static NativeMethods.LowLevelKeyboardProc Proc = HookProc;
    private static IntPtr HHook = IntPtr.Zero;

    private static IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x100;
        if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            InputEvent(null, (Keys)Marshal.ReadInt32(lParam));
        }
        return NativeMethods.CallNextHookEx(HHook, code, wParam, lParam);
    }

    public static void UnHook()
    {
        NativeMethods.UnhookWindowsHookEx(HHook);
    }

    static KeyboardListener()
    {
        const int WH_KEYBOARD_LL = 13;
        HHook = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, Proc, NativeMethods.LoadLibrary("User32"), 0);
    }
}

static class MouseInput
{
    public delegate void EventHandler(object sender, MouseEventArgs e);

    public static EventHandler DragWindowFor(IntPtr handle)
    {
        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        return (object sender, MouseEventArgs e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, (IntPtr)0);
            }
        };
    }
}

class InputKey : TextBox
{
    public readonly Keys Key;
    private readonly IDisposable Unsubscribe;
    private event EventHandler KeyEvent;

    public InputKey(Keys keyCode)
    {
        Key = keyCode;
        BorderStyle = BorderStyle.None;
        Enabled = false;
        Dock = DockStyle.Left;
        MinimumSize = new Size(30, 30);
        Text = Enum.GetName(keyCode.GetType(), keyCode);
        ReadOnly = true;
        Size = new Size(30, 30);
        TextAlign = HorizontalAlignment.Center;

        Unsubscribe = Observable.FromEventPattern(ev => KeyEvent += ev, ev => KeyEvent -= ev)
            .Do(SetColor(Color.Red))
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .Do(SetColor(Color.Yellow)).Subscribe();
    }

    protected override void Dispose(bool disposing)
    {
        Unsubscribe.Dispose();
        base.Dispose(disposing);
    }

    private Action<EventPattern<object>> SetColor(Color color)
    {
        return (_) =>
        {
            BackColor = color;
        };
    }

    public void Trigger()
    {
        KeyEvent(null, EventArgs.Empty);
    }
}

class KeyGrid : FlowLayoutPanel
{
    private readonly IDisposable Unsubscribe;

    public KeyGrid(IEnumerable<IEnumerable<Keys>> layoutConfig)
    {
        IEnumerable<IEnumerable<InputKey>> keyLayout = layoutConfig
            .Select(row => row.Select(key => new InputKey(key)).ToList())
            .ToList();

        FlowDirection = FlowDirection.TopDown;
        SuspendLayout();
        AutoSize = true;

        Controls.AddRange(keyLayout
            .Select(Enumerable.ToArray)
            .Select(row =>
            {
                FlowLayoutPanel rowPanel = new FlowLayoutPanel();
                rowPanel.FlowDirection = FlowDirection.LeftToRight;
                rowPanel.BackColor = System.Drawing.Color.Pink;
                rowPanel.SuspendLayout();
                rowPanel.AutoSize = true;
                rowPanel.Controls.AddRange(row);
                rowPanel.ResumeLayout();
                return rowPanel;
            })
            .ToArray());

        ResumeLayout();

        Dictionary<Keys, InputKey> keyLookup = keyLayout
            .SelectMany(row => row.Select(inputKey => new { inputKey.Key, inputKey }))
            .ToDictionary(entry => entry.Key, entry => entry.inputKey);

        Unsubscribe = Observable.FromEventPattern<Keys>(ev => KeyboardListener.InputEvent += ev, ev => KeyboardListener.InputEvent -= ev)
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
            .Subscribe();
    }

    protected override void Dispose(bool disposing)
    {
        Unsubscribe.Dispose();
        KeyboardListener.UnHook();
        base.Dispose(disposing);
    }
}

class MainWindow : Form
{
    public MainWindow(IEnumerable<IEnumerable<Keys>> layoutConfig)
    {
        Controls.Add(new KeyGrid(layoutConfig));
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
