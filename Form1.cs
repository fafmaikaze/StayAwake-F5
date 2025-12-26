namespace pwCopy
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int VK_F5 = 0x74;
        private IntPtr cachedHwnd = IntPtr.Zero;
        private string _targetWindowName = string.Empty;

        private void SetTargetWindowName(string value)
        {
            if (_targetWindowName != value)
            {
                _targetWindowName = value;
                cachedHwnd = IntPtr.Zero; // 名稱改變時重置快取
                if (textBox1 != null && textBox1.Text != value)
                {
                    textBox1.Text = value;
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            // 初始化Timer
            keyTimer = new System.Windows.Forms.Timer();
            keyTimer.Interval = 1000 * 60 * 10; // 每5分鐘發送一次，可自行調整
            //keyTimer.Interval = 1000 * 5; // 每5秒，測試用
            keyTimer.Tick += KeyTimer_Tick;
        }

        private bool isButtonHelperActivate = false;
        private System.Windows.Forms.Timer keyTimer; // Timer變數

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Clipboard.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            isButtonHelperActivate = !isButtonHelperActivate;
            if (isButtonHelperActivate)
            {
                button5.Text = "自動F5啟動中...";
                keyTimer.Start(); // 開始發送F5
                KeyTimer_Tick(null!, null!); // 立即觸發一次
                textBox1.Enabled = false; // 鎖定目標視窗輸入框
            }
            else
            {
                button5.Text = "自動F5";
                keyTimer.Stop(); // 停止發送F5
                textBox1.Enabled = true; // 解鎖目標視窗輸入框
            }
        }

        private void KeyTimer_Tick_OLD(object sender, EventArgs e)
        {
            SendKeys.Send("{F5}"); // 發送F5
        }

        private class WindowOption
        {
            public IntPtr Hwnd { get; set; }
            public string Title { get; set; } = string.Empty;
            public string ProcessName { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"[{Hwnd}][{ProcessName}] {Title}";
            }
        }

        private static List<WindowOption> GetOpenWindows()
        {
            var windows = new List<WindowOption>();
            IntPtr shellWindow = GetShellWindow();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                System.Text.StringBuilder builder = new(length);
                _ = GetWindowText(hWnd, builder, length + 1);
                string title = builder.ToString();

                // 取得 Process Name
                _ = GetWindowThreadProcessId(hWnd, out uint pid);
                string processName = "Unknown";
                try
                {
                    processName = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName;
                }
                catch { }

                windows.Add(new WindowOption { Hwnd = hWnd, Title = title, ProcessName = processName });
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private static void SendBackgroundF5(IntPtr hWnd)
        {
            byte vkF5 = VK_F5;
            int scanCode = (int)MapVirtualKey(vkF5, 0);

            uint lParamDown = 0x00000001 | ((uint)scanCode << 16);
            uint lParamUp = 0xC0000001 | ((uint)scanCode << 16) | 0xC0000000;

            PostMessage(hWnd, WM_KEYDOWN, (IntPtr)vkF5, (IntPtr)lParamDown);
            PostMessage(hWnd, WM_KEYUP, (IntPtr)vkF5, (IntPtr)lParamUp);
        }

        private static WindowOption SelectTargetWindow()
        {
            var prompt = new Form()
            {
                Width = 800,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "請選擇目標視窗",
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true
            };
            var textLabel = new Label()
            {
                Left = 20,
                Top = 10,
                Text = "請選擇要發送 F5 的視窗 (格式: [Handle][程序] 標題):",
                AutoSize = true
            };
            var cmb = new ComboBox()
            {
                Left = 20,
                Top = 35,
                Width = 740,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var confirmation = new Button()
            {
                Text = "確定",
                Left = 680,
                Width = 80,
                Top = 70,
                DialogResult = DialogResult.OK
            };

            var windows = GetOpenWindows();
            foreach (WindowOption w in windows)
            {
                cmb.Items.Add(w);
            }

            if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(cmb);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? (WindowOption)cmb.SelectedItem : null;
        }

        private void KeyTimer_Tick(object sender, EventArgs e)
        {
            if (cachedHwnd == IntPtr.Zero)
            {
                var selection = SelectTargetWindow();
                if (selection is not null)
                {
                    // 注意順序：先設定名稱 (因為 setter 會清空 cachedHwnd)，再設定 Handle
                    SetTargetWindowName(selection.ToString());
                    cachedHwnd = selection.Hwnd; // 這就是鎖定視窗的 "TAG" (Handle)
                }
            }

            if (cachedHwnd == IntPtr.Zero) return;

            // 檢查快取的 Handle 是否有效
            if (!IsWindow(cachedHwnd))
            {
                cachedHwnd = IntPtr.Zero;
                SetTargetWindowName(""); // 清空顯示
                MessageBox.Show("目標視窗已關閉或失效，請重新選擇。");

                // 重新選擇
                var selection = SelectTargetWindow();
                if (selection is not null)
                {
                    SetTargetWindowName(selection.ToString());
                    cachedHwnd = selection.Hwnd;
                }
            }

            if (cachedHwnd != IntPtr.Zero)
            {
                // 嘗試模仿 AutoHotkey 的 ControlSend
                // AHK 的強大之處在於它會尋找視窗內的「子控制項」(Child Control) 來發送
                // 而不只是發送給最外層的視窗。

                // 1. 先對主視窗發送 (以防萬一)
                SendBackgroundF5(cachedHwnd);

                // 2. 遍歷所有子視窗並發送 F5
                EnumChildWindows(cachedHwnd, delegate (IntPtr childHwnd, IntPtr lParam)
                {
                    SendBackgroundF5(childHwnd);
                    return true; // 繼續遍歷
                }, IntPtr.Zero);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _targetWindowName = textBox1.Text;

            // 當 TextBox 內容改變時，嘗試解析並更新 cachedHwnd
            // 格式預期為: [Handle][Process] Title
            cachedHwnd = IntPtr.Zero;
            if (!string.IsNullOrEmpty(_targetWindowName) && _targetWindowName.StartsWith("["))
            {
                int endBracket = _targetWindowName.IndexOf(']');
                if (endBracket > 1)
                {
                    string handleStr = _targetWindowName.Substring(1, endBracket - 1);
                    if (long.TryParse(handleStr, out long val))
                    {
                        cachedHwnd = (IntPtr)val;
                    }
                }
            }
        }
    }
}