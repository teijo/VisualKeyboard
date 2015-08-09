using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
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

enum EventType {
    DOWN,
    UP
}

static class KeyboardListener
{
    private static NativeMethods.LowLevelKeyboardProc Proc = HookProc;
    private static IntPtr HHook = IntPtr.Zero;

    private static event EventHandler<Tuple<EventType, Keys>> InputEvent;
    private static IObservable<Tuple<EventType, Keys>> AsObservable = Observable
            .FromEventPattern<Tuple<EventType, Keys>>(ev => InputEvent += ev, ev => InputEvent -= ev)
            .Select(ev => ev.EventArgs);

    private static IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        if (code >= 0)
        {
            var keys = (Keys)Marshal.ReadInt32(lParam);
            switch ((int)wParam) {
                case WM_KEYDOWN:
                    InputEvent(null, Tuple.Create(EventType.DOWN, keys));
                    break;
                case WM_KEYUP:
                    InputEvent(null, Tuple.Create(EventType.UP, keys));
                    break;
            }
        }
        return NativeMethods.CallNextHookEx(HHook, code, wParam, lParam);
    }

    public static void UnHook()
    {
        if (HHook == IntPtr.Zero)
        {
            throw new InvalidOperationException("Trying to UnHook keyboard listener second time");
        }
        NativeMethods.UnhookWindowsHookEx(HHook);
        HHook = IntPtr.Zero;
    }

    static KeyboardListener()
    {
        const int WH_KEYBOARD_LL = 13;
        HHook = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, Proc, NativeMethods.LoadLibrary("User32"), 0);
    }

    public static IObservable<EventType> KeyEvents(Keys key)
    {
        return AsObservable
            .Where(e => e.Item2 == key)
            .Select(e => e.Item1)
            .DistinctUntilChanged();
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
                NativeMethods.SendMessage(handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        };
    }
}

struct InputConfig
{
    public readonly Keys Key;
    public readonly int Width;
    public readonly string Label;

    public InputConfig(Keys key, int width, string label)
    {
        Key = key;
        Width = width;
        Label = label;
    }
}

static class Util
{
    public static IObservable<T> ConstantObservable<T>(T value)
    {
        return Observable.Never<T>().StartWith(value);
    }
}

class BlankKey : Label
{
    private const int MarginWidth = 1;

    public BlankKey(InputConfig keyCode, IObservable<Size> windowSizes)
    {
        windowSizes.Subscribe(sizes =>
        {
            var keySize = new Size(
                keyCode.Width * sizes.Width - 2 * MarginWidth,
                sizes.Height - 2 * MarginWidth);
            MinimumSize = keySize;
            Size = MinimumSize;
        });
        AutoSize = true;
        Enabled = false;
        Dock = DockStyle.Left;
        TextAlign = ContentAlignment.MiddleCenter;
        Margin = new Padding(MarginWidth);
    }
}

class InputKey : BlankKey
{
    private readonly IDisposable Unsubscribe;

    private static IObservable<Color> ColorSequence<T>(T _)
    {
        var sequence = new List<Tuple<Color, int>> {
            Tuple.Create(Color.Red, 0),
            Tuple.Create(Color.Yellow, 1000),
            Tuple.Create(Color.DarkGray, 2000)
        };

        return Observable.Generate((IEnumerator<Tuple<Color, int>>)sequence.GetEnumerator(),
            s => s.MoveNext(),
            s => s,
            s => s.Current.Item1,
            s => TimeSpan.FromMilliseconds(s.Current.Item2));
    }

    public InputKey(InputConfig keyCode, IObservable<Size> windowSizes, IObservable<EventType> keyEvents) : base(keyCode, windowSizes)
    {
        BorderStyle = BorderStyle.None;
        Text = keyCode.Label;
        BackColor = Color.DarkGray;

        var downColor = keyEvents
            .Where(eventType => eventType == EventType.DOWN)
            .Select(_ => Util.ConstantObservable(Color.White));

        var upColor = keyEvents
            .Where(eventType => eventType == EventType.UP)
            .Select(ColorSequence);

        Unsubscribe = Observable
            .Switch(Observable.Merge(downColor, upColor))
            .Do((color) => { base.BackColor = color; })
            .Subscribe();
    }

    protected override void Dispose(bool disposing)
    {
        Unsubscribe.Dispose();
        base.Dispose(disposing);
    }
}

class KeyGrid : FlowLayoutPanel
{
    public KeyGrid(IEnumerable<IEnumerable<InputConfig>> layoutConfig, Tuple<int, int> gridDimensions, IObservable<Size> keySizes)
    {
        IEnumerable<IEnumerable<Label>> keyLayout = layoutConfig
            .Select(row => row.Select(keyConfig => (keyConfig.Key == Keys.None) ? new BlankKey(keyConfig, keySizes) : new InputKey(keyConfig, keySizes, KeyboardListener.KeyEvents(keyConfig.Key))).ToList())
            .ToList();

        FlowDirection = FlowDirection.TopDown;
        Dock = DockStyle.Fill;
        BackColor = Color.Black;
        Enabled = false;

        Controls.AddRange(keyLayout
            .Select(Enumerable.ToArray)
            .Select(row =>
            {
                FlowLayoutPanel rowPanel = new FlowLayoutPanel();
                rowPanel.FlowDirection = FlowDirection.LeftToRight;
                rowPanel.AutoSize = true;
                rowPanel.Controls.AddRange(row);
                rowPanel.Margin = new Padding(0);
                return rowPanel;
            })
            .ToArray());
    }
}

class MainWindow : Form
{
    public MainWindow(IEnumerable<IEnumerable<InputConfig>> layoutConfig)
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        ControlBox = false;
        Text = String.Empty;
        TopMost = true;
        MouseDown += new MouseEventHandler(MouseInput.DragWindowFor(Handle));

        var gridDimensions = GridDimensions(layoutConfig);
        var sizeChanges = Observable
            .FromEventPattern<EventArgs>(this, "Resize")
            .Select(_ => ClientSize);

        IObservable<Size> keySizes = sizeChanges
            .Select(size => new Size(size.Width / gridDimensions.Item1, size.Height / gridDimensions.Item2));

        sizeChanges
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Zip(keySizes.Latest(), (_, keySize) => keySize)
            .ObserveOn(SynchronizationContext.Current)
            .Do(keySize => ClientSize = new Size(keySize.Width * gridDimensions.Item1, keySize.Height * gridDimensions.Item2))
            .Subscribe();

        Controls.Add(new KeyGrid(layoutConfig, gridDimensions, keySizes));
        ClientSize = new Size(300, 200); // Initial size

        var resizeEnd = Observable.FromEventPattern<EventArgs>(this, "ResizeEnd");
        resizeEnd.Subscribe(_ => SnapToScreen());

        var sizeToolTip = new ToolTip();
        Observable
            .FromEventPattern<EventArgs>(this, "ResizeBegin")
            .Do(_ =>
            {
                var end = resizeEnd
                    .Take(1)
                    .Delay(TimeSpan.FromSeconds(1))
                    .ObserveOn(SynchronizationContext.Current)
                    .Do(_2 => sizeToolTip.Hide(this));

                sizeChanges
                    .TakeUntil(end)
                    .Do(clientSize => sizeToolTip.Show(clientSize.Width + "x" + clientSize.Height, this, 0, 0))
                    .Subscribe();
            })
            .Subscribe();
    }

    private static Tuple<int, int> GridDimensions(IEnumerable<IEnumerable<InputConfig>> layoutConfig)
    {
        var width = layoutConfig.Aggregate(0, (a, b) => Math.Max(a, b.Count()));
        var height = layoutConfig.Count();
        return Tuple.Create(width, height);
    }

    private static void SnapNear(int delta, Func<int> doSnap)
    {
        const int distance = 40;
        if (Math.Abs(delta) <= distance)
        {
            doSnap();
        }
    }

    private void SnapToScreen()
    {
        var screen = Screen.FromPoint(Location).WorkingArea;
        SnapNear(Left - screen.Left,     () => Left = screen.Left);
        SnapNear(Top - screen.Top,       () => Top = screen.Top);
        SnapNear(screen.Right - Right,   () => Left = screen.Right - Width);
        SnapNear(screen.Bottom - Bottom, () => Top = screen.Bottom - Height);
    }
}

static class KeySupport
{
    private static readonly Dictionary<string, Keys> Map;

    public static Keys GetKey(string keyString)
    {
        return Map[keyString.ToLower()];
    }

    static KeySupport()
    {
        List<Tuple<string, Keys>> entries = new List<Tuple<string, Keys>>();

        var parseableEntries = "qwertyuiopasdfghjklzxcvbnm"
            .ToCharArray()
            .Select(char.ToString)
            .Select(ch => new Tuple<string, Keys>(ch, (Keys)Enum.Parse(typeof(Keys), ch.ToUpper())));

        entries.AddRange(parseableEntries);

        var stringEntries = new List<Tuple<string, Keys>>
        {
            Tuple.Create("space", Keys.Space),
            Tuple.Create("1", Keys.D1),
            Tuple.Create("2", Keys.D2),
            Tuple.Create("3", Keys.D3),
            Tuple.Create("4", Keys.D4),
            Tuple.Create("5", Keys.D5),
            Tuple.Create("6", Keys.D6),
            Tuple.Create("7", Keys.D7),
            Tuple.Create("8", Keys.D8),
            Tuple.Create("9", Keys.D9),
            Tuple.Create("0", Keys.D0),
            Tuple.Create("-", Keys.None),
        };

        entries.AddRange(stringEntries);

        Map = entries.ToDictionary(e => e.Item1, e => e.Item2);
    }
}

static class Program
{
    private static InputConfig ParseKey(string keyConfig)
    {
        var parts = keyConfig.Split(':');
        var width = parts.Length > 1 ? int.Parse(parts[1]) : 1;
        var keyString = parts[0];
        var key = KeySupport.GetKey(keyString);
        var label = parts.Length > 2 ? parts[2] : keyString;
        return new InputConfig(key, width, label);
    }

    private static IEnumerable<IEnumerable<InputConfig>> ParseConfigString(string configString)
    {
        return configString
            .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(row => row.Split(' ').Select(ParseKey));
    }

    [STAThread]
    static void Main()
    {
        KeyboardListener.KeyEvents(Keys.Escape).Subscribe(_ => Application.Exit());

        string layoutConfig = System.Text.Encoding.Default.GetString(Resources.DefaultLayout);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainWindow(ParseConfigString(layoutConfig)));
        KeyboardListener.UnHook();
    }
}
