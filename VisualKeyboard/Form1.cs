﻿using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

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

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;

        private LowLevelKeyboardProc _proc = hookProc;

        private static IntPtr hhook = IntPtr.Zero;

        public void SetHook()
        {
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, LoadLibrary("User32"), 0);
        }

        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }

        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys keyId = (Keys)Marshal.ReadInt32(lParam);
                if (inputKeys.ContainsKey(keyId))
                {
                    inputKeys[keyId].Flash();
                }
            }
            return CallNextHookEx(hhook, code, (int)wParam, lParam);
        }

        public Form1()
        {
            SetHook();
            InitializeComponent();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }


        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private Panel buildLayoutPanel(List<List<InputKey>> keyLayout)
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

        private void InitializeComponent()
        {
            List<List<Keys>> keyConfig = new List<List<Keys>>();
            keyConfig.Add(new List<Keys> { Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P });
            keyConfig.Add(new List<Keys> { Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L });
            keyConfig.Add(new List<Keys> { Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M });

            List<List<InputKey>> layout = keyConfig.Select(row =>
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
            Name = "Form1";
            Text = "Form1";
            TopMost = true;
            MouseDown += new MouseEventHandler(Form1_MouseDown);
            AutoSize = true;
            ResumeLayout(false);
            PerformLayout();
        }

        private static Dictionary<Keys, InputKey> inputKeys = new Dictionary<Keys, InputKey>();
        private List<List<Keys>> keyLayout = new List<List<Keys>>();
    }
}
