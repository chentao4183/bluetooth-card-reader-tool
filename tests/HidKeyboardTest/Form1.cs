using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;

namespace HidKeyboardTest;

public partial class Form1 : Form
{
    private TextBox? _txtLog;
    private TextBox? _txtCardNumber;
    private Label? _lblStatus;
    private Label? _lblCardNumber;
    private Button? _btnMinimize;
    private Button? _btnSelectDevice;
    private NotifyIcon? _notifyIcon;
    private StringBuilder _inputBuffer = new StringBuilder();
    private System.Windows.Forms.Timer? _resetTimer;
    private IntPtr _targetDeviceHandle = IntPtr.Zero;
    private string _targetDeviceName = "";

    // Raw Input 相关常量
    private const int WM_INPUT = 0x00FF;
    private const int RID_INPUT = 0x10000003;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int RIM_TYPEKEYBOARD = 1;
    private const int RIDI_DEVICENAME = 0x20000007;
    private const int RIDI_DEVICEINFO = 0x2000000b;

    // Raw Input 结构体
    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUT
    {
        public RAWINPUTHEADER Header;
        public RAWKEYBOARD Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint dwType;
    }

    // Windows API
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public Form1()
    {
        InitializeComponent();
        InitializeUI();
        SetupRawInput();
    }

    private void InitializeUI()
    {
        // 设置窗体
        this.Text = "HID 键盘监听测试 - 后台运行模式（设备过滤）";
        this.Size = new Size(900, 700);
        this.StartPosition = FormStartPosition.CenterScreen;

        // 系统托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "HID 键盘监听 - 后台运行中",
            Visible = true
        };
        _notifyIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("显示窗口", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
        contextMenu.Items.Add("退出", null, (s, e) => { Application.Exit(); });
        _notifyIcon.ContextMenuStrip = contextMenu;

        // 状态标签
        _lblStatus = new Label
        {
            Text = "状态：未选择设备",
            Location = new Point(10, 10),
            Size = new Size(880, 30),
            Font = new Font("微软雅黑", 12, FontStyle.Bold),
            ForeColor = Color.Red
        };
        this.Controls.Add(_lblStatus);

        // 选择设备按钮
        _btnSelectDevice = new Button
        {
            Text = "选择蓝牙刷卡器",
            Location = new Point(10, 50),
            Size = new Size(150, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnSelectDevice.Click += BtnSelectDevice_Click;
        this.Controls.Add(_btnSelectDevice);

        // 卡号标签
        _lblCardNumber = new Label
        {
            Text = "当前卡号：",
            Location = new Point(170, 50),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleRight
        };
        this.Controls.Add(_lblCardNumber);

        // 卡号文本框
        _txtCardNumber = new TextBox
        {
            Location = new Point(280, 53),
            Size = new Size(250, 30),
            Font = new Font("Consolas", 14, FontStyle.Bold),
            ReadOnly = true,
            BackColor = Color.LightYellow,
            ForeColor = Color.Red,
            TextAlign = HorizontalAlignment.Center
        };
        this.Controls.Add(_txtCardNumber);

        // 最小化按钮
        _btnMinimize = new Button
        {
            Text = "最小化到托盘",
            Location = new Point(540, 50),
            Size = new Size(120, 35),
            Font = new Font("微软雅黑", 9)
        };
        _btnMinimize.Click += (s, e) => { this.Hide(); };
        this.Controls.Add(_btnMinimize);

        // 说明标签
        var lblInstruction = new Label
        {
            Text = "请先选择蓝牙刷卡器，程序将只监听该设备的输入，忽略其他键盘",
            Location = new Point(10, 95),
            Size = new Size(880, 25),
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.Gray
        };
        this.Controls.Add(lblInstruction);

        // 日志文本框
        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(10, 130),
            Size = new Size(860, 515),
            Font = new Font("Consolas", 9),
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.LightGreen
        };
        this.Controls.Add(_txtLog);

        // 重置计时器（500ms 内没有新输入则重置缓冲区）
        _resetTimer = new System.Windows.Forms.Timer();
        _resetTimer.Interval = 500;
        _resetTimer.Tick += (s, e) =>
        {
            _resetTimer.Stop();
            if (_inputBuffer.Length > 0 && _inputBuffer.Length != 10)
            {
                Log($"输入超时，清空缓冲区（长度：{_inputBuffer.Length}）");
                _inputBuffer.Clear();
            }
        };

        Log("=== HID 键盘监听测试已启动（设备过滤模式）===");
        Log("程序使用 Raw Input API 监听指定蓝牙刷卡器");
        Log("请点击 [选择蓝牙刷卡器] 按钮选择目标设备");
        Log("");
    }

    private void BtnSelectDevice_Click(object? sender, EventArgs e)
    {
        try
        {
            // 获取所有键盘设备
            uint deviceCount = 0;
            GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));

            if (deviceCount == 0)
            {
                MessageBox.Show("未找到任何输入设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)) * deviceCount));
            try
            {
                GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));

                List<(IntPtr handle, string name)> keyboards = new List<(IntPtr, string)>();

                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr devicePtr = IntPtr.Add(pRawInputDeviceList, i * Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
                    RAWINPUTDEVICELIST device = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(devicePtr);

                    // 只处理键盘设备
                    if (device.dwType == RIM_TYPEKEYBOARD)
                    {
                        string deviceName = GetDeviceName(device.hDevice);
                        if (!string.IsNullOrEmpty(deviceName))
                        {
                            keyboards.Add((device.hDevice, deviceName));
                        }
                    }
                }

                if (keyboards.Count == 0)
                {
                    MessageBox.Show("未找到键盘设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 显示设备选择对话框
                var selectForm = new Form
                {
                    Text = "选择蓝牙刷卡器",
                    Size = new Size(700, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var listBox = new ListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(660, 300),
                    Font = new Font("Consolas", 9)
                };

                foreach (var kb in keyboards)
                {
                    string friendlyName = GetFriendlyDeviceName(kb.name);
                    listBox.Items.Add(friendlyName);
                }

                var btnOk = new Button
                {
                    Text = "确定",
                    Location = new Point(500, 320),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "取消",
                    Location = new Point(590, 320),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.Cancel
                };

                selectForm.Controls.Add(listBox);
                selectForm.Controls.Add(btnOk);
                selectForm.Controls.Add(btnCancel);
                selectForm.AcceptButton = btnOk;
                selectForm.CancelButton = btnCancel;

                if (selectForm.ShowDialog() == DialogResult.OK && listBox.SelectedIndex >= 0)
                {
                    var selected = keyboards[listBox.SelectedIndex];
                    _targetDeviceHandle = selected.handle;
                    _targetDeviceName = selected.name;

                    Log("");
                    Log("=== 已选择设备 ===");
                    Log($"设备名称：{_targetDeviceName}");
                    Log($"设备句柄：0x{_targetDeviceHandle:X}");
                    Log("");
                    Log("✓ 现在只监听该设备的输入，其他键盘将被忽略");
                    Log("✓ 程序可以后台运行，无需窗口焦点");
                    Log("");
                    Log("请刷卡测试...");
                    Log("");

                    UpdateStatus($"监听设备：{GetShortDeviceName(_targetDeviceName)}", Color.Green);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pRawInputDeviceList);
            }
        }
        catch (Exception ex)
        {
            Log($"✗ 选择设备失败：{ex.Message}");
            MessageBox.Show($"选择设备失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GetFriendlyDeviceName(string fullName)
    {
        // 从设备路径中提取友好名称
        if (string.IsNullOrEmpty(fullName))
            return "未知设备";

        try
        {
            // 尝试从注册表获取设备友好名称
            string friendlyName = GetDeviceFriendlyNameFromRegistry(fullName);
            if (!string.IsNullOrEmpty(friendlyName))
            {
                return friendlyName;
            }

            // 如果无法从注册表获取，则解析设备路径
            string vid = "";
            string pid = "";

            if (fullName.Contains("VID_"))
            {
                int vidStart = fullName.IndexOf("VID_") + 4;
                vid = fullName.Substring(vidStart, 4);
            }

            if (fullName.Contains("PID_"))
            {
                int pidStart = fullName.IndexOf("PID_") + 4;
                pid = fullName.Substring(pidStart, 4);
            }

            // 判断设备类型
            string deviceType = "键盘设备";
            if (fullName.Contains("Bluetooth") || fullName.ToLower().Contains("bth"))
            {
                deviceType = "蓝牙键盘";
            }
            else if (fullName.Contains("USB"))
            {
                deviceType = "USB 键盘";
            }
            else if (fullName.Contains("HID"))
            {
                deviceType = "HID 键盘";
            }

            // 组合友好名称
            if (!string.IsNullOrEmpty(vid) && !string.IsNullOrEmpty(pid))
            {
                return $"{deviceType} (VID:{vid} PID:{pid})";
            }
            else
            {
                return deviceType;
            }
        }
        catch
        {
            return "未知设备";
        }
    }

    private string GetDeviceFriendlyNameFromRegistry(string devicePath)
    {
        try
        {
            // 从设备路径提取设备实例 ID
            // 例如：\\?\HID#VID_1A2C&PID_6004#7&1234abcd&0&0000#{...}
            // 提取：HID\VID_1A2C&PID_6004\7&1234abcd&0&0000

            if (!devicePath.StartsWith(@"\\?\"))
                return "";

            string path = devicePath.Substring(4); // 移除 \\?\
            int guidIndex = path.LastIndexOf("#{");
            if (guidIndex > 0)
            {
                path = path.Substring(0, guidIndex);
            }

            path = path.Replace("#", "\\");

            // 在注册表中查找设备
            string registryPath = @"SYSTEM\CurrentControlSet\Enum\" + path;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    // 尝试获取 FriendlyName
                    object friendlyName = key.GetValue("DeviceDesc");
                    if (friendlyName != null)
                    {
                        string name = friendlyName.ToString();
                        // 移除 @oem 前缀
                        if (name.Contains(";"))
                        {
                            name = name.Substring(name.LastIndexOf(";") + 1);
                        }
                        return name;
                    }
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return "";
    }

    private string GetDeviceName(IntPtr hDevice)
    {
        uint size = 0;
        GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref size);

        if (size == 0) return "";

        IntPtr buffer = Marshal.AllocHGlobal((int)size * 2);
        try
        {
            GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, buffer, ref size);
            return Marshal.PtrToStringUni(buffer) ?? "";
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private string GetShortDeviceName(string fullName)
    {
        // 提取设备名称的简短版本
        if (fullName.Contains("Bluetooth"))
            return "Bluetooth Keyboard";
        if (fullName.Contains("HID"))
            return "HID Keyboard";
        return "Unknown Device";
    }

    private void SetupRawInput()
    {
        try
        {
            // 注册 Raw Input 设备（键盘）
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].UsagePage = 0x01; // Generic Desktop
            devices[0].Usage = 0x06;     // Keyboard
            devices[0].Flags = RIDEV_INPUTSINK; // 后台接收输入
            devices[0].Target = this.Handle;

            if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                int error = Marshal.GetLastWin32Error();
                Log($"✗ 注册 Raw Input 失败，错误代码：{error}");
                MessageBox.Show($"注册 Raw Input 失败，错误代码：{error}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Log("✓ Raw Input 注册成功（RIDEV_INPUTSINK 模式）");
                Log("");
            }
        }
        catch (Exception ex)
        {
            Log($"✗ 设置 Raw Input 失败：{ex.Message}");
            MessageBox.Show($"设置 Raw Input 失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_INPUT)
        {
            ProcessRawInput(m.LParam);
        }
        base.WndProc(ref m);
    }

    private void ProcessRawInput(IntPtr lParam)
    {
        try
        {
            uint size = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            if (size == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, buffer, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == size)
                {
                    RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);

                    // 只处理键盘输入
                    if (raw.Header.Type == RIM_TYPEKEYBOARD)
                    {
                        // 设备过滤：只处理目标设备
                        if (_targetDeviceHandle != IntPtr.Zero && raw.Header.Device != _targetDeviceHandle)
                        {
                            // 忽略其他设备的输入
                            return;
                        }

                        // 只处理按键按下事件（WM_KEYDOWN）
                        if (raw.Keyboard.Message == 0x0100)
                        {
                            ProcessKeyPress(raw.Keyboard.VKey);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            Log($"✗ 处理 Raw Input 失败：{ex.Message}");
        }
    }

    private void ProcessKeyPress(ushort vKey)
    {
        // 重启计时器
        _resetTimer?.Stop();
        _resetTimer?.Start();

        // 回车键
        if (vKey == 0x0D) // VK_RETURN
        {
            ProcessInput();
            return;
        }

        // 数字键（主键盘区 0-9）
        if (vKey >= 0x30 && vKey <= 0x39)
        {
            char digit = (char)vKey;
            _inputBuffer.Append(digit);
            Log($"接收到数字：{digit}（缓冲区长度：{_inputBuffer.Length}）");

            // 如果达到 10 位，自动处理
            if (_inputBuffer.Length == 10)
            {
                _resetTimer?.Stop();
                ProcessInput();
            }
            return;
        }

        // 数字键（小键盘区 0-9）
        if (vKey >= 0x60 && vKey <= 0x69)
        {
            char digit = (char)(vKey - 0x60 + '0');
            _inputBuffer.Append(digit);
            Log($"接收到数字：{digit}（缓冲区长度：{_inputBuffer.Length}）");

            // 如果达到 10 位，自动处理
            if (_inputBuffer.Length == 10)
            {
                _resetTimer?.Stop();
                ProcessInput();
            }
            return;
        }

        // 其他按键（调试用）
        // Log($"接收到按键：VKey={vKey:X2}");
    }

    private void ProcessInput()
    {
        if (_inputBuffer.Length == 0) return;

        string input = _inputBuffer.ToString();
        _inputBuffer.Clear();

        Log("");
        Log("=== 处理输入 ===");
        Log($"输入内容：{input}");
        Log($"输入长度：{input.Length}");

        if (input.Length == 10 && input.All(char.IsDigit))
        {
            Log($"✓ 识别为卡号：{input}");
            Log("");

            // 显示卡号
            if (_txtCardNumber != null)
            {
                _txtCardNumber.Invoke(new Action(() => _txtCardNumber.Text = input));
            }

            UpdateStatus($"✓ 卡号：{input}", Color.Green);

            // 显示托盘通知
            _notifyIcon?.ShowBalloonTip(2000, "检测到卡号", $"卡号：{input}", ToolTipIcon.Info);

            // 播放提示音
            System.Media.SystemSounds.Beep.Play();
        }
        else
        {
            Log($"✗ 无效输入（长度不是 10 位或包含非数字字符）");
            Log("");
            UpdateStatus($"监听设备：{GetShortDeviceName(_targetDeviceName)}", Color.Green);
        }
    }

    private void UpdateStatus(string text, Color color)
    {
        if (_lblStatus == null) return;

        if (_lblStatus.InvokeRequired)
        {
            _lblStatus.Invoke(new Action(() => UpdateStatus(text, color)));
            return;
        }

        _lblStatus.Text = $"状态：{text}";
        _lblStatus.ForeColor = color;
    }

    private void Log(string message)
    {
        if (_txtLog == null) return;

        if (_txtLog.InvokeRequired)
        {
            _txtLog.Invoke(new Action(() => Log(message)));
            return;
        }

        _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
        _txtLog.SelectionStart = _txtLog.Text.Length;
        _txtLog.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
            _notifyIcon?.ShowBalloonTip(2000, "程序已最小化", "程序仍在后台运行，双击托盘图标可恢复窗口", ToolTipIcon.Info);
        }
        else
        {
            _resetTimer?.Stop();
            _resetTimer?.Dispose();
            _notifyIcon?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
