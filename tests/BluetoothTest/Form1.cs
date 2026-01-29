using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BluetoothTest;

public partial class Form1 : Form
{
    private BluetoothLEDevice? _device;
    private TextBox? _txtLog;
    private Button? _btnScan;
    private Button? _btnConnect;
    private Button? _btnDisconnect;
    private Label? _lblStatus;
    private DeviceInformation? _selectedDeviceInfo;

    public Form1()
    {
        InitializeComponent();
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 设置窗体
        this.Text = "蓝牙测试工具 - BLE 扫描与连接";
        this.Size = new Size(900, 650);
        this.StartPosition = FormStartPosition.CenterScreen;

        // 状态标签
        _lblStatus = new Label
        {
            Text = "状态：未连接",
            Location = new Point(10, 10),
            Size = new Size(880, 25),
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            ForeColor = Color.Gray
        };
        this.Controls.Add(_lblStatus);

        // 扫描按钮
        _btnScan = new Button
        {
            Text = "扫描 BLE 设备",
            Location = new Point(10, 45),
            Size = new Size(150, 40),
            Font = new Font("微软雅黑", 10)
        };
        _btnScan.Click += BtnScan_Click;
        this.Controls.Add(_btnScan);

        // 连接按钮
        _btnConnect = new Button
        {
            Text = "连接设备",
            Location = new Point(170, 45),
            Size = new Size(150, 40),
            Font = new Font("微软雅黑", 10),
            Enabled = false
        };
        _btnConnect.Click += BtnConnect_Click;
        this.Controls.Add(_btnConnect);

        // 断开按钮
        _btnDisconnect = new Button
        {
            Text = "断开连接",
            Location = new Point(330, 45),
            Size = new Size(150, 40),
            Font = new Font("微软雅黑", 10),
            Enabled = false
        };
        _btnDisconnect.Click += BtnDisconnect_Click;
        this.Controls.Add(_btnDisconnect);

        // 日志文本框
        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(10, 95),
            Size = new Size(860, 500),
            Font = new Font("Consolas", 9),
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.LightGreen
        };
        this.Controls.Add(_txtLog);

        Log("=== 蓝牙测试工具已启动 ===");
        Log("点击 [扫描 BLE 设备] 开始...");
        Log("");
    }

    private async void BtnScan_Click(object? sender, EventArgs e)
    {
        Log("=== 开始扫描 BLE 设备 ===");
        _btnScan!.Enabled = false;
        UpdateStatus("正在扫描...", Color.Orange);

        try
        {
            // 使用设备选择器扫描 BLE 设备
            string selector = BluetoothLEDevice.GetDeviceSelector();

            var picker = new DevicePicker();
            picker.Filter.SupportedDeviceSelectors.Add(selector);

            // 显示设备选择器
            var rect = new Windows.Foundation.Rect(100, 100, 300, 300);
            var deviceInfo = await picker.PickSingleDeviceAsync(rect);

            if (deviceInfo != null)
            {
                _selectedDeviceInfo = deviceInfo;
                Log($"✓ 发现设备：{deviceInfo.Name}");
                Log($"  设备 ID：{deviceInfo.Id}");
                Log("");
                _btnConnect!.Enabled = true;
                UpdateStatus($"已选择设备：{deviceInfo.Name}", Color.Blue);
            }
            else
            {
                Log("✗ 未选择设备");
                Log("");
                UpdateStatus("未连接", Color.Gray);
            }
        }
        catch (Exception ex)
        {
            Log($"✗ 扫描失败：{ex.Message}");
            Log("");
            UpdateStatus("扫描失败", Color.Red);
        }
        finally
        {
            _btnScan.Enabled = true;
        }
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_selectedDeviceInfo == null) return;

        Log($"=== 正在连接 {_selectedDeviceInfo.Name} ===");
        _btnConnect!.Enabled = false;
        _btnScan!.Enabled = false;
        UpdateStatus($"正在连接 {_selectedDeviceInfo.Name}...", Color.Orange);

        try
        {
            // 连接设备
            _device = await BluetoothLEDevice.FromIdAsync(_selectedDeviceInfo.Id);

            if (_device == null)
            {
                Log("✗ 连接失败：无法创建设备对象");
                UpdateStatus("连接失败", Color.Red);
                _btnConnect.Enabled = true;
                _btnScan.Enabled = true;
                return;
            }

            Log("✓ 连接成功！");
            Log("");
            Log("正在发现服务...");
            UpdateStatus($"已连接：{_device.Name}", Color.Green);

            // 获取所有 GATT 服务
            var servicesResult = await _device.GetGattServicesAsync();

            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                Log($"✗ 获取服务失败：{servicesResult.Status}");
                UpdateStatus("连接失败", Color.Red);
                _btnConnect.Enabled = true;
                _btnScan.Enabled = true;
                return;
            }

            var services = servicesResult.Services;
            Log($"✓ 发现 {services.Count} 个服务");
            Log("");

            int serviceIndex = 1;
            foreach (var service in services)
            {
                Log($"[服务 {serviceIndex}] UUID: {service.Uuid}");

                // 获取特征
                var characteristicsResult = await service.GetCharacteristicsAsync();

                if (characteristicsResult.Status == GattCommunicationStatus.Success)
                {
                    var characteristics = characteristicsResult.Characteristics;
                    Log($"  └─ 包含 {characteristics.Count} 个特征");

                    int charIndex = 1;
                    foreach (var characteristic in characteristics)
                    {
                        Log($"     [{charIndex}] 特征 UUID: {characteristic.Uuid}");
                        Log($"         属性: {characteristic.CharacteristicProperties}");

                        // 如果支持 Notify，订阅通知
                        if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            Log($"         ✓ 支持通知，正在订阅...");

                            characteristic.ValueChanged += (sender, args) =>
                            {
                                var reader = Windows.Storage.Streams.DataReader.FromBuffer(args.CharacteristicValue);
                                byte[] data = new byte[reader.UnconsumedBufferLength];
                                reader.ReadBytes(data);

                                string hex = BitConverter.ToString(data).Replace("-", " ");

                                // 尝试解析为 ASCII
                                string ascii = "";
                                foreach (byte b in data)
                                {
                                    if (b >= 32 && b <= 126)
                                        ascii += (char)b;
                                    else
                                        ascii += $"[{b:X2}]";
                                }

                                Log($"         [通知数据]");
                                Log($"         HEX:   {hex}");
                                Log($"         ASCII: {ascii}");
                                Log($"         长度:  {data.Length} 字节");
                                Log("");

                                // 如果是 10 位数字，可能是卡号
                                if (data.Length == 10 && data.All(b => b >= '0' && b <= '9'))
                                {
                                    string cardNumber = Encoding.ASCII.GetString(data);
                                    Log($"         ★★★ 检测到卡号：{cardNumber} ★★★");
                                    Log("");
                                }
                            };

                            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.Notify);

                            if (status == GattCommunicationStatus.Success)
                            {
                                Log($"         ✓ 订阅成功");
                            }
                            else
                            {
                                Log($"         ✗ 订阅失败：{status}");
                            }
                        }

                        charIndex++;
                    }
                }

                Log("");
                serviceIndex++;
            }

            Log("=== 所有服务已发现并订阅通知 ===");
            Log("现在可以刷卡测试，数据会实时显示在上方");
            Log("");

            _btnDisconnect!.Enabled = true;
        }
        catch (Exception ex)
        {
            Log($"✗ 连接失败：{ex.Message}");
            Log($"详细信息：{ex.StackTrace}");
            Log("");
            UpdateStatus("连接失败", Color.Red);
            _btnConnect.Enabled = true;
            _btnScan.Enabled = true;
        }
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        if (_device == null) return;

        try
        {
            _device.Dispose();
            _device = null;
            Log("=== 已断开连接 ===");
            Log("");
            UpdateStatus("未连接", Color.Gray);

            _btnDisconnect!.Enabled = false;
            _btnConnect!.Enabled = true;
            _btnScan!.Enabled = true;
        }
        catch (Exception ex)
        {
            Log($"✗ 断开失败：{ex.Message}");
            Log("");
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

        _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        _txtLog.SelectionStart = _txtLog.Text.Length;
        _txtLog.ScrollToCaret();
    }
}
