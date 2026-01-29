# 技术验证计划

## 目标
验证 C# + .NET 6 方案中的关键技术是否满足项目需求，降低开发风险。

---

## 验证项 1：PaddleOCR-Sharp 离线识别能力

### 验证目标
- 确认 PaddleOCR-Sharp 能够在 C# 中正常工作
- 测试中文识别准确率
- 测试识别速度（是否 ≤ 3 秒/字段）
- 确认模型文件大小和部署方式

### 验证步骤

#### 1.1 创建测试项目
```bash
# 创建控制台项目
dotnet new console -n PaddleOcrTest
cd PaddleOcrTest

# 添加 NuGet 包
dotnet add package Sdcb.PaddleOCR
dotnet add package Sdcb.PaddleOCR.Models.LocalV3
```

#### 1.2 编写测试代码
创建 `Program.cs`：
```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.LocalV3;
using Sdcb.PaddleOCR.Models;
using System.Diagnostics;

// 初始化 PaddleOCR（中文模型）
FullOcrModel model = LocalFullModels.ChineseV3;
using PaddleOcrAll ocr = new PaddleOcrAll(model)
{
    AllowRotateDetection = true,
    Enable180Classification = false,
};

// 测试图片路径（准备一张包含中文的测试图片）
string testImagePath = @"C:\test_image.png";

if (!File.Exists(testImagePath))
{
    Console.WriteLine("请准备测试图片：C:\\test_image.png");
    Console.WriteLine("图片应包含中文文字，例如：姓名、年龄、性别等");
    return;
}

// 开始识别
Console.WriteLine("开始识别...");
Stopwatch sw = Stopwatch.StartNew();

PaddleOcrResult result = ocr.Run(testImagePath);

sw.Stop();

// 输出结果
Console.WriteLine($"\n识别耗时：{sw.ElapsedMilliseconds} ms");
Console.WriteLine($"识别到 {result.Regions.Length} 个文本区域：\n");

foreach (var region in result.Regions)
{
    Console.WriteLine($"文本：{region.Text}");
    Console.WriteLine($"置信度：{region.Score:P2}");
    Console.WriteLine($"位置：({region.Rect.Location.X}, {region.Rect.Location.Y})");
    Console.WriteLine("---");
}
```

#### 1.3 运行测试
```bash
dotnet run
```

### 验收标准
- ✅ 能够成功识别中文文字（姓名、年龄、性别等）
- ✅ 识别准确率 ≥ 85%（手动评估）
- ✅ 识别速度 ≤ 3 秒
- ✅ 模型文件大小可接受（≤ 100MB）

### 风险评估
- ❌ 如果识别准确率 < 70%：考虑切换到 Tesseract.Net 或 Windows.Media.Ocr
- ❌ 如果识别速度 > 5 秒：考虑优化或降低图片分辨率
- ❌ 如果模型文件 > 150MB：考虑使用轻量级模型

---

## 验证项 2：InTheHand.BluetoothLE 蓝牙扫描与连接

### 验证目标
- 确认能够扫描 BLE 设备
- 确认能够连接设备并订阅通知
- 测试连接稳定性

### 验证步骤

#### 2.1 创建测试项目
```bash
# 创建 WinForms 项目（需要 UI 线程）
dotnet new winforms -n BluetoothTest
cd BluetoothTest

# 添加 NuGet 包
dotnet add package InTheHand.BluetoothLE
```

#### 2.2 编写测试代码
修改 `Form1.cs`：
```csharp
using InTheHand.Bluetooth;
using System;
using System.Windows.Forms;

namespace BluetoothTest
{
    public partial class Form1 : Form
    {
        private BluetoothDevice? _device;

        public Form1()
        {
            InitializeComponent();

            // 添加扫描按钮
            Button btnScan = new Button
            {
                Text = "扫描 BLE 设备",
                Location = new Point(10, 10),
                Size = new Size(150, 30)
            };
            btnScan.Click += BtnScan_Click;
            Controls.Add(btnScan);

            // 添加连接按钮
            Button btnConnect = new Button
            {
                Text = "连接设备",
                Location = new Point(170, 10),
                Size = new Size(150, 30),
                Enabled = false
            };
            btnConnect.Click += BtnConnect_Click;
            Controls.Add(btnConnect);

            // 添加日志文本框
            TextBox txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 50),
                Size = new Size(760, 500),
                Name = "txtLog"
            };
            Controls.Add(txtLog);
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            Log("开始扫描 BLE 设备...");

            try
            {
                // 扫描设备
                _device = await Bluetooth.RequestDeviceAsync(new RequestDeviceOptions
                {
                    AcceptAllDevices = true
                });

                if (_device != null)
                {
                    Log($"发现设备：{_device.Name} ({_device.Id})");
                    ((Button)Controls[1]).Enabled = true;
                }
                else
                {
                    Log("未选择设备");
                }
            }
            catch (Exception ex)
            {
                Log($"扫描失败：{ex.Message}");
            }
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_device == null) return;

            Log($"正在连接 {_device.Name}...");

            try
            {
                // 获取 GATT 服务
                var gatt = _device.Gatt;
                await gatt.ConnectAsync();

                Log("连接成功！正在发现服务...");

                // 获取所有服务
                var services = await gatt.GetPrimaryServicesAsync();
                Log($"发现 {services.Count} 个服务");

                foreach (var service in services)
                {
                    Log($"服务 UUID: {service.Uuid}");

                    // 获取特征
                    var characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics)
                    {
                        Log($"  特征 UUID: {characteristic.Uuid}");
                        Log($"  属性: {characteristic.Properties}");

                        // 如果支持 Notify，订阅通知
                        if (characteristic.Properties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            Log($"  订阅通知...");
                            characteristic.CharacteristicValueChanged += (s, args) =>
                            {
                                byte[] data = args.Value;
                                string hex = BitConverter.ToString(data).Replace("-", " ");
                                Log($"  收到通知：{hex}");
                            };
                            await characteristic.StartNotificationsAsync();
                        }
                    }
                }

                Log("所有服务已发现并订阅通知");
            }
            catch (Exception ex)
            {
                Log($"连接失败：{ex.Message}");
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Log(message)));
                return;
            }

            var txtLog = (TextBox)Controls["txtLog"];
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
```

#### 2.3 运行测试
```bash
dotnet run
```

### 验收标准
- ✅ 能够扫描并列出 BLE 设备
- ✅ 能够连接设备并获取服务列表
- ✅ 能够订阅 Notify 特征并接收数据
- ✅ 连接稳定，不频繁断开

### 风险评估
- ❌ 如果无法扫描设备：检查 Windows 蓝牙权限、驱动程序
- ❌ 如果连接频繁断开：考虑增加重连机制
- ❌ 如果 InTheHand.BluetoothLE 不稳定：考虑使用 Windows.Devices.Bluetooth（UWP API）

---

## 验证项 3：Windows Raw Input API（HID 键盘监听）

### 验证目标
- 确认能够监听指定 HID 设备的键盘输入
- 确认能够过滤其他键盘输入
- 测试卡号接收准确性

### 验证步骤

#### 3.1 创建测试项目
```bash
# 使用上面的 WinForms 项目
cd BluetoothTest
```

#### 3.2 编写测试代码
创建 `RawInputListener.cs`：
```csharp
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BluetoothTest
{
    public class RawInputListener
    {
        private const int WM_INPUT = 0x00FF;
        private const int RID_INPUT = 0x10000003;

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

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

        public event Action<string>? KeyPressed;

        public void Register(IntPtr hwnd)
        {
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].UsagePage = 0x01; // Generic Desktop
            devices[0].Usage = 0x06;     // Keyboard
            devices[0].Flags = 0x00000100; // RIDEV_INPUTSINK
            devices[0].Target = hwnd;

            if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                throw new Exception("注册 Raw Input 失败");
            }
        }

        public void ProcessMessage(Message m)
        {
            if (m.Msg != WM_INPUT) return;

            uint size = 0;
            GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetRawInputData(m.LParam, RID_INPUT, buffer, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == size)
                {
                    RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);

                    // 只处理按键按下事件
                    if (raw.Keyboard.Message == 0x0100) // WM_KEYDOWN
                    {
                        char key = (char)MapVirtualKey(raw.Keyboard.VKey, 2);
                        if (char.IsDigit(key))
                        {
                            KeyPressed?.Invoke(key.ToString());
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(ushort uCode, uint uMapType);
    }
}
```

修改 `Form1.cs` 添加 HID 测试：
```csharp
// 在 Form1 类中添加
private RawInputListener? _rawInputListener;
private string _cardNumber = "";

protected override void OnLoad(EventArgs e)
{
    base.OnLoad(e);

    // 注册 Raw Input
    _rawInputListener = new RawInputListener();
    _rawInputListener.Register(Handle);
    _rawInputListener.KeyPressed += OnKeyPressed;

    Log("Raw Input 监听已启动，请刷卡测试...");
}

protected override void WndProc(ref Message m)
{
    _rawInputListener?.ProcessMessage(m);
    base.WndProc(ref m);
}

private void OnKeyPressed(string key)
{
    _cardNumber += key;

    if (_cardNumber.Length == 10)
    {
        Log($"接收到卡号：{_cardNumber}");
        _cardNumber = "";
    }
}
```

#### 3.3 运行测试
```bash
dotnet run
```
插入 HID 键盘刷卡器，刷卡测试。

### 验收标准
- ✅ 能够接收 HID 键盘输入
- ✅ 能够正确解析 10 位卡号
- ✅ 不会干扰其他键盘输入

### 风险评估
- ❌ 如果无法接收输入：检查设备是否为 HID 键盘模式
- ❌ 如果接收到其他键盘输入：需要增加设备过滤逻辑

---

## 验证项 4：.NET 6 Self-contained 打包

### 验证目标
- 确认能够打包为单文件 EXE
- 确认打包体积可接受
- 确认在无 .NET 环境的机器上能运行

### 验证步骤

#### 4.1 打包测试项目
```bash
cd BluetoothTest

# Self-contained 单文件发布
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

# 查看输出
dir bin\Release\net6.0-windows\win-x64\publish
```

#### 4.2 测试运行
1. 将 `publish` 目录下的 EXE 复制到另一台**没有安装 .NET 的 Windows 机器**
2. 双击运行，确认能正常启动

### 验收标准
- ✅ 打包成功，生成单文件 EXE
- ✅ 文件大小 ≤ 150MB
- ✅ 在无 .NET 环境的机器上能正常运行

### 风险评估
- ❌ 如果体积 > 200MB：考虑不使用 PublishTrimmed，或排除不必要的依赖
- ❌ 如果无法运行：检查是否缺少 VC++ 运行库

---

## 验证总结

### 验证清单
- [ ] PaddleOCR-Sharp 识别测试（准确率、速度、模型大小）
- [ ] InTheHand.BluetoothLE 蓝牙测试（扫描、连接、通知）
- [ ] Windows Raw Input HID 监听测试（卡号接收）
- [ ] .NET 6 Self-contained 打包测试（体积、兼容性）

### 验证时间
预计 1-2 天完成所有验证。

### 验证通过标准
- 所有验证项 ✅ 通过
- 无重大技术风险
- 可以进入正式开发阶段

### 验证失败应对
如果某项验证失败，根据风险评估采取替代方案：
- PaddleOCR-Sharp 失败 → 切换到 Tesseract.Net 或 Windows.Media.Ocr
- InTheHand.BluetoothLE 失败 → 使用 Windows.Devices.Bluetooth（UWP）
- Raw Input 失败 → 使用全局键盘钩子（SetWindowsHookEx）
- 打包体积过大 → 考虑分离模型文件，首次运行时下载

---

**下一步**：开始执行验证计划，完成后汇报结果。
