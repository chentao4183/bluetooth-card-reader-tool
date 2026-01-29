using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BluetoothCardReaderTool.Core;

/// <summary>
/// HID 设备信息
/// </summary>
public class HidDevice
{
    public IntPtr Handle { get; set; }
    public string Name { get; set; } = "";
    public string FriendlyName { get; set; } = "";
}

/// <summary>
/// 卡号接收事件参数
/// </summary>
public class CardNumberReceivedEventArgs : EventArgs
{
    public string CardNumber { get; set; } = "";
}

/// <summary>
/// 日志事件参数
/// </summary>
public class LogEventArgs : EventArgs
{
    public string Message { get; set; } = "";
}

/// <summary>
/// 蓝牙刷卡器管理器
/// 封装 HID 键盘监听的所有底层逻辑
/// </summary>
public class BluetoothManager : IDisposable
{
    #region Raw Input 常量

    private const int WM_INPUT = 0x00FF;
    private const int RID_INPUT = 0x10000003;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int RIM_TYPEKEYBOARD = 1;
    private const int RIDI_DEVICENAME = 0x20000007;
    private const int RIDI_DEVICEINFO = 0x2000000b;

    #endregion

    #region Raw Input 结构体

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

    #endregion

    #region Windows API

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

    #endregion

    #region 事件

    /// <summary>
    /// 接收到卡号事件
    /// </summary>
    public event EventHandler<CardNumberReceivedEventArgs>? CardNumberReceived;

    /// <summary>
    /// 日志消息事件
    /// </summary>
    public event EventHandler<LogEventArgs>? LogMessage;

    #endregion

    #region 属性

    /// <summary>
    /// 是否正在监听
    /// </summary>
    public bool IsListening => _targetDeviceHandle != IntPtr.Zero;

    /// <summary>
    /// 当前设备名称
    /// </summary>
    public string CurrentDeviceName => _targetDeviceName;

    /// <summary>
    /// 当前设备句柄
    /// </summary>
    public IntPtr CurrentDeviceHandle => _targetDeviceHandle;

    #endregion

    #region 私有字段

    private IntPtr _targetDeviceHandle = IntPtr.Zero;
    private string _targetDeviceName = "";
    private StringBuilder _inputBuffer = new StringBuilder();
    private System.Windows.Forms.Timer? _resetTimer;
    private int _cardLength = 10;
    private bool _requireEnter = false;
    private readonly object _bufferLock = new object();

    #endregion

    #region 构造函数

    public BluetoothManager()
    {
        // 初始化重置计时器（500ms 内没有新输入则重置缓冲区）
        _resetTimer = new System.Windows.Forms.Timer();
        _resetTimer.Interval = 500;
        _resetTimer.Tick += OnResetTimerTick;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 配置参数
    /// </summary>
    public void Configure(int cardLength, bool requireEnter)
    {
        _cardLength = cardLength;
        _requireEnter = requireEnter;
        RaiseLog($"配置已更新：卡号长度={cardLength}，需要Enter={requireEnter}");
    }

    /// <summary>
    /// 获取可用的 HID 键盘设备列表
    /// </summary>
    public List<HidDevice> GetAvailableDevices()
    {
        var devices = new List<HidDevice>();

        try
        {
            uint deviceCount = 0;
            GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));

            if (deviceCount == 0)
            {
                RaiseLog("未找到任何输入设备");
                return devices;
            }

            IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)) * deviceCount));
            try
            {
                GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));

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
                            devices.Add(new HidDevice
                            {
                                Handle = device.hDevice,
                                Name = deviceName,
                                FriendlyName = GetFriendlyDeviceName(deviceName)
                            });
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pRawInputDeviceList);
            }

            RaiseLog($"找到 {devices.Count} 个键盘设备");
        }
        catch (Exception ex)
        {
            RaiseLog($"枚举设备失败：{ex.Message}");
        }

        return devices;
    }

    /// <summary>
    /// 选择设备并开始监听
    /// </summary>
    public bool SelectDevice(IntPtr deviceHandle, string deviceName)
    {
        try
        {
            _targetDeviceHandle = deviceHandle;
            _targetDeviceName = deviceName;

            lock (_bufferLock)
            {
                _inputBuffer.Clear();
            }

            RaiseLog("");
            RaiseLog("=== 已选择设备 ===");
            RaiseLog($"设备名称：{_targetDeviceName}");
            RaiseLog($"设备句柄：0x{_targetDeviceHandle:X}");
            RaiseLog("");
            RaiseLog("✓ 现在只监听该设备的输入，其他键盘将被忽略");
            RaiseLog("✓ 程序可以后台运行，无需窗口焦点");
            RaiseLog("");
            RaiseLog("请刷卡测试...");
            RaiseLog("");

            return true;
        }
        catch (Exception ex)
        {
            RaiseLog($"选择设备失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 停止监听
    /// </summary>
    public void StopListening()
    {
        _targetDeviceHandle = IntPtr.Zero;
        _targetDeviceName = "";

        lock (_bufferLock)
        {
            _inputBuffer.Clear();
        }

        _resetTimer?.Stop();
        RaiseLog("已停止监听");
    }

    /// <summary>
    /// 注册 Raw Input（需要在窗体创建后调用）
    /// </summary>
    public bool RegisterRawInput(IntPtr windowHandle)
    {
        try
        {
            // 注册 Raw Input 设备（键盘）
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].UsagePage = 0x01; // Generic Desktop
            devices[0].Usage = 0x06;     // Keyboard
            devices[0].Flags = RIDEV_INPUTSINK; // 后台接收输入
            devices[0].Target = windowHandle;

            if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                int error = Marshal.GetLastWin32Error();
                RaiseLog($"✗ 注册 Raw Input 失败，错误代码：{error}");
                return false;
            }
            else
            {
                RaiseLog("✓ Raw Input 注册成功（RIDEV_INPUTSINK 模式）");
                RaiseLog("");
                return true;
            }
        }
        catch (Exception ex)
        {
            RaiseLog($"✗ 设置 Raw Input 失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 处理 Raw Input 消息（在 WndProc 中调用）
    /// </summary>
    public void ProcessRawInputMessage(IntPtr lParam)
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
            RaiseLog($"✗ 处理 Raw Input 失败：{ex.Message}");
        }
    }

    #endregion

    #region 私有方法

    private void OnResetTimerTick(object? sender, EventArgs e)
    {
        _resetTimer?.Stop();

        lock (_bufferLock)
        {
            if (_inputBuffer.Length > 0 && _inputBuffer.Length != _cardLength)
            {
                RaiseLog($"输入超时，清空缓冲区（长度：{_inputBuffer.Length}）");
                _inputBuffer.Clear();
            }
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
            if (_requireEnter)
            {
                ProcessInput();
            }
            return;
        }

        // 数字键（主键盘区 0-9）
        if (vKey >= 0x30 && vKey <= 0x39)
        {
            char digit = (char)vKey;
            lock (_bufferLock)
            {
                _inputBuffer.Append(digit);
                RaiseLog($"接收到数字：{digit}（缓冲区长度：{_inputBuffer.Length}）");

                // 如果达到指定位数，自动处理
                if (_inputBuffer.Length == _cardLength && !_requireEnter)
                {
                    _resetTimer?.Stop();
                    ProcessInput();
                }
            }
            return;
        }

        // 数字键（小键盘区 0-9）
        if (vKey >= 0x60 && vKey <= 0x69)
        {
            char digit = (char)(vKey - 0x60 + '0');
            lock (_bufferLock)
            {
                _inputBuffer.Append(digit);
                RaiseLog($"接收到数字：{digit}（缓冲区长度：{_inputBuffer.Length}）");

                // 如果达到指定位数，自动处理
                if (_inputBuffer.Length == _cardLength && !_requireEnter)
                {
                    _resetTimer?.Stop();
                    ProcessInput();
                }
            }
            return;
        }

        // 其他按键（调试用）
        // RaiseLog($"接收到按键：VKey={vKey:X2}");
    }

    private void ProcessInput()
    {
        string input;
        lock (_bufferLock)
        {
            if (_inputBuffer.Length == 0) return;

            input = _inputBuffer.ToString();
            _inputBuffer.Clear();
        }

        RaiseLog("");
        RaiseLog("=== 处理输入 ===");
        RaiseLog($"输入内容：{input}");
        RaiseLog($"输入长度：{input.Length}");

        if (input.Length == _cardLength && input.All(char.IsDigit))
        {
            RaiseLog($"✓ 识别为卡号：{input}");
            RaiseLog("");

            // 触发卡号接收事件
            CardNumberReceived?.Invoke(this, new CardNumberReceivedEventArgs { CardNumber = input });
        }
        else
        {
            RaiseLog($"✗ 无效输入（长度不是 {_cardLength} 位或包含非数字字符）");
            RaiseLog("");
        }
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

            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    // 尝试获取 FriendlyName
                    object? friendlyName = key.GetValue("DeviceDesc");
                    if (friendlyName != null)
                    {
                        string name = friendlyName.ToString() ?? "";
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

    private void RaiseLog(string message)
    {
        LogMessage?.Invoke(this, new LogEventArgs { Message = message });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _resetTimer?.Stop();
        _resetTimer?.Dispose();
        _resetTimer = null;
    }

    #endregion
}
