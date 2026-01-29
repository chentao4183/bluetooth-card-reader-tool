# Notepad++ + .NET 6 SDK 开发环境搭建指南

## 第一步：安装 .NET 6 SDK

### 1.1 下载 .NET 6 SDK

**下载地址**：https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0

**选择版本**：
- 点击 **SDK 6.0.xxx** 下的 **Windows x64** 下载
- 文件名类似：`dotnet-sdk-6.0.xxx-win-x64.exe`
- 大小约 150MB

### 1.2 安装

1. 双击下载的 `.exe` 文件
2. 点击"安装"
3. 等待安装完成（约 2-3 分钟）
4. 点击"关闭"

### 1.3 验证安装

打开**命令提示符**（CMD）或 **PowerShell**：

```bash
# 方法1：按 Win+R，输入 cmd，回车
# 方法2：在开始菜单搜索"命令提示符"

# 输入以下命令验证
dotnet --version
```

**预期输出**：
```
6.0.xxx
```

如果显示版本号，说明安装成功！

---

## 第二步：创建第一个验证项目（PaddleOCR 测试）

### 2.1 创建项目

打开命令提示符，执行：

```bash
# 进入工作目录
cd C:\Users\Administrator\Desktop\5t

# 创建控制台项目
dotnet new console -n PaddleOcrTest

# 进入项目目录
cd PaddleOcrTest
```

### 2.2 添加 NuGet 包

```bash
# 添加 PaddleOCR 相关包
dotnet add package Sdcb.PaddleOCR
dotnet add package Sdcb.PaddleOCR.Models.LocalV3
dotnet add package Sdcb.PaddleInference
dotnet add package Sdcb.PaddleInference.runtime.win64.mkl

# 添加图像处理库
dotnet add package OpenCvSharp4
dotnet add package OpenCvSharp4.runtime.win
```

### 2.3 准备测试图片

1. 在 `C:\Users\Administrator\Desktop\5t\PaddleOcrTest` 目录下创建一个测试图片
2. 图片名称：`test_image.png`
3. 图片内容：包含中文文字，例如截图包含"姓名：张三"、"年龄：23"、"性别：男"等

**快速生成测试图片的方法**：
- 打开 Word 或记事本
- 输入几行中文文字（姓名、年龄、性别等）
- 按 `Win + Shift + S` 截图
- 保存为 `test_image.png`

### 2.4 编写测试代码

用 **Notepad++** 打开 `Program.cs`，替换为以下内容：

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.LocalV3;
using Sdcb.PaddleOCR.Models;
using System.Diagnostics;

Console.WriteLine("=== PaddleOCR 中文识别测试 ===\n");

// 测试图片路径
string testImagePath = "test_image.png";

if (!File.Exists(testImagePath))
{
    Console.WriteLine("错误：找不到测试图片！");
    Console.WriteLine($"请在以下位置放置测试图片：{Path.GetFullPath(testImagePath)}");
    Console.WriteLine("图片应包含中文文字，例如：姓名、年龄、性别等");
    Console.WriteLine("\n按任意键退出...");
    Console.ReadKey();
    return;
}

try
{
    Console.WriteLine("正在初始化 PaddleOCR（中文模型）...");

    // 初始化 PaddleOCR（中文模型）
    FullOcrModel model = LocalFullModels.ChineseV3;
    using PaddleOcrAll ocr = new PaddleOcrAll(model)
    {
        AllowRotateDetection = true,
        Enable180Classification = false,
    };

    Console.WriteLine("初始化完成！\n");
    Console.WriteLine("开始识别图片...");

    // 开始计时
    Stopwatch sw = Stopwatch.StartNew();

    // 识别图片
    PaddleOcrResult result = ocr.Run(testImagePath);

    // 停止计时
    sw.Stop();

    // 输出结果
    Console.WriteLine($"\n✓ 识别完成！耗时：{sw.ElapsedMilliseconds} ms");
    Console.WriteLine($"✓ 识别到 {result.Regions.Length} 个文本区域\n");
    Console.WriteLine("=== 识别结果 ===\n");

    int index = 1;
    foreach (var region in result.Regions)
    {
        Console.WriteLine($"[{index}] 文本：{region.Text}");
        Console.WriteLine($"    置信度：{region.Score:P2}");
        Console.WriteLine($"    位置：({region.Rect.Location.X}, {region.Rect.Location.Y})");
        Console.WriteLine($"    大小：{region.Rect.Size.Width} x {region.Rect.Size.Height}");
        Console.WriteLine();
        index++;
    }

    // 评估结果
    Console.WriteLine("=== 评估 ===");
    Console.WriteLine($"识别速度：{(sw.ElapsedMilliseconds <= 3000 ? "✓ 通过" : "✗ 超时")} (目标 ≤ 3000ms)");

    double avgScore = result.Regions.Length > 0
        ? result.Regions.Average(r => r.Score)
        : 0;
    Console.WriteLine($"平均置信度：{avgScore:P2} {(avgScore >= 0.85 ? "✓ 通过" : "✗ 偏低")} (目标 ≥ 85%)");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ 错误：{ex.Message}");
    Console.WriteLine($"\n详细信息：\n{ex}");
}

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();
```

**保存文件**（Ctrl+S）

### 2.5 运行测试

在命令提示符中：

```bash
# 编译并运行
dotnet run
```

**预期输出**：
```
=== PaddleOCR 中文识别测试 ===

正在初始化 PaddleOCR（中文模型）...
初始化完成！

开始识别图片...

✓ 识别完成！耗时：1234 ms
✓ 识别到 3 个文本区域

=== 识别结果 ===

[1] 文本：姓名：张三
    置信度：95.67%
    位置：(10, 20)
    大小：150 x 30

[2] 文本：年龄：23
    置信度：92.34%
    位置：(10, 60)
    大小：100 x 30

[3] 文本：性别：男
    置信度：94.12%
    位置：(10, 100)
    大小：100 x 30

=== 评估 ===
识别速度：✓ 通过 (目标 ≤ 3000ms)
平均置信度：94.04% ✓ 通过 (目标 ≥ 85%)

按任意键退出...
```

### 2.6 验证标准

- ✅ 识别速度 ≤ 3000ms
- ✅ 平均置信度 ≥ 85%
- ✅ 能正确识别中文文字

---

## 第三步：创建蓝牙测试项目

### 3.1 创建项目

```bash
# 返回上级目录
cd ..

# 创建 WinForms 项目
dotnet new winforms -n BluetoothTest

# 进入项目目录
cd BluetoothTest
```

### 3.2 添加 NuGet 包

```bash
# 添加蓝牙库
dotnet add package InTheHand.BluetoothLE
```

### 3.3 编写测试代码

用 **Notepad++** 打开 `Form1.cs`，替换为以下内容：

```csharp
using InTheHand.Bluetooth;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluetoothTest
{
    public partial class Form1 : Form
    {
        private BluetoothDevice? _device;
        private TextBox? _txtLog;
        private Button? _btnScan;
        private Button? _btnConnect;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 设置窗体
            this.Text = "蓝牙测试工具";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 扫描按钮
            _btnScan = new Button
            {
                Text = "扫描 BLE 设备",
                Location = new Point(10, 10),
                Size = new Size(150, 35),
                Font = new Font("微软雅黑", 10)
            };
            _btnScan.Click += BtnScan_Click;
            this.Controls.Add(_btnScan);

            // 连接按钮
            _btnConnect = new Button
            {
                Text = "连接设备",
                Location = new Point(170, 10),
                Size = new Size(150, 35),
                Font = new Font("微软雅黑", 10),
                Enabled = false
            };
            _btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(_btnConnect);

            // 日志文本框
            _txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 55),
                Size = new Size(760, 490),
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            this.Controls.Add(_txtLog);

            Log("蓝牙测试工具已启动");
            Log("点击"扫描 BLE 设备"开始...");
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            Log("\n=== 开始扫描 BLE 设备 ===");
            _btnScan!.Enabled = false;

            try
            {
                // 扫描设备
                _device = await Bluetooth.RequestDeviceAsync(new RequestDeviceOptions
                {
                    AcceptAllDevices = true
                });

                if (_device != null)
                {
                    Log($"✓ 发现设备：{_device.Name}");
                    Log($"  设备 ID：{_device.Id}");
                    _btnConnect!.Enabled = true;
                }
                else
                {
                    Log("✗ 未选择设备");
                }
            }
            catch (Exception ex)
            {
                Log($"✗ 扫描失败：{ex.Message}");
            }
            finally
            {
                _btnScan.Enabled = true;
            }
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_device == null) return;

            Log($"\n=== 正在连接 {_device.Name} ===");
            _btnConnect!.Enabled = false;

            try
            {
                // 获取 GATT 服务
                var gatt = _device.Gatt;
                await gatt.ConnectAsync();

                Log("✓ 连接成功！");
                Log("\n正在发现服务...");

                // 获取所有服务
                var services = await gatt.GetPrimaryServicesAsync();
                Log($"✓ 发现 {services.Count} 个服务\n");

                foreach (var service in services)
                {
                    Log($"服务 UUID: {service.Uuid}");

                    // 获取特征
                    var characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics)
                    {
                        Log($"  └─ 特征 UUID: {characteristic.Uuid}");
                        Log($"     属性: {characteristic.Properties}");

                        // 如果支持 Notify，订阅通知
                        if (characteristic.Properties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            Log($"     ✓ 订阅通知...");

                            characteristic.CharacteristicValueChanged += (s, args) =>
                            {
                                byte[] data = args.Value;
                                string hex = BitConverter.ToString(data).Replace("-", " ");
                                string ascii = System.Text.Encoding.ASCII.GetString(data)
                                    .Replace("\r", "\\r")
                                    .Replace("\n", "\\n");

                                Log($"     [通知] HEX: {hex}");
                                Log($"     [通知] ASCII: {ascii}");
                            };

                            await characteristic.StartNotificationsAsync();
                        }
                    }
                    Log("");
                }

                Log("=== 所有服务已发现并订阅通知 ===");
                Log("现在可以刷卡测试，数据会显示在上方");
            }
            catch (Exception ex)
            {
                Log($"✗ 连接失败：{ex.Message}");
                _btnConnect.Enabled = true;
            }
        }

        private void Log(string message)
        {
            if (_txtLog == null) return;

            if (_txtLog.InvokeRequired)
            {
                _txtLog.Invoke(new Action(() => Log(message)));
                return;
            }

            _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
```

**保存文件**（Ctrl+S）

### 3.4 运行测试

```bash
# 编译并运行
dotnet run
```

**操作步骤**：
1. 点击"扫描 BLE 设备"
2. 在弹出的窗口中选择你的蓝牙刷卡器
3. 点击"连接设备"
4. 刷卡测试，观察日志输出

---

## 常见问题

### Q1: 提示"找不到 dotnet 命令"
**解决**：
1. 确认已安装 .NET 6 SDK
2. 重启命令提示符
3. 重启电脑

### Q2: 编译错误
**解决**：
1. 检查代码是否完整复制
2. 确认 NuGet 包已安装：`dotnet restore`
3. 查看错误信息，根据提示修改

### Q3: PaddleOCR 识别失败
**解决**：
1. 确认测试图片存在
2. 图片格式为 PNG/JPG
3. 图片包含清晰的中文文字

### Q4: 蓝牙扫描失败
**解决**：
1. 确认 Windows 蓝牙已开启
2. 确认设备已上电且可被发现
3. 尝试在 Windows 设置中先配对设备

---

## 下一步

完成以上两个验证后，请告诉我结果：
- PaddleOCR 识别速度和准确率如何？
- 蓝牙能否正常扫描和连接？

我会根据验证结果，继续提供后续的开发指导。
