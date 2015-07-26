using System;
using System.Drawing;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive;

namespace VisualKeyboard
{
    class InputKey : TextBox
    {
        public readonly Keys Key;
        private IDisposable unsubscribe;
        private event EventHandler KeyEvent;

        public InputKey(Keys key)
        {
            this.Key = key;
            BorderStyle = BorderStyle.None;
            Enabled = false;
            Dock = DockStyle.Left;
            MinimumSize = new Size(30, 30);
            Text = Enum.GetName(key.GetType(), key);
            ReadOnly = true;
            Size = new Size(30, 30);
            TextAlign = HorizontalAlignment.Center;

            unsubscribe = Observable.FromEventPattern(ev => KeyEvent += ev, ev => KeyEvent -= ev)
                .Do(SetColor(Color.Red))
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .Do(SetColor(Color.Yellow)).Subscribe();
        }

        protected override void Dispose(bool disposing)
        {
            unsubscribe.Dispose();
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
}
