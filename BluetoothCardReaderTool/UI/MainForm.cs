using System.Drawing;
using System.Windows.Forms;
using BluetoothCardReaderTool.Models;
using BluetoothCardReaderTool.Utils;
using BluetoothCardReaderTool.Core;

namespace BluetoothCardReaderTool.UI;

public partial class MainForm : Form
{
    private TabControl? _tabControl;
    private TextBox? _txtLog;
    private NotifyIcon? _notifyIcon;
    private AppSettings _settings;

    // 蓝牙配置页控件
    private BluetoothManager? _bluetoothManager;
    private Button? _btnSelectDevice;
    private Label? _lblCurrentDevice;
    private Button? _btnStartListening;
    private Button? _btnStopListening;
    private Button? _btnTestDevice;
    private NumericUpDown? _numCardLength;
    private CheckBox? _chkRequireEnter;
    private TextBox? _txtHidKeywords;
    private TextBox? _txtLatestCard;
    private TextBox? _txtListenLog;
    private Button? _btnSaveBluetoothConfig;
    private Button? _btnResetBluetoothConfig;

    // OCR 配置页控件
    private OcrService? _ocrService;
    private DataGridView? _dgvFields;
    private Button? _btnAddField;
    private Button? _btnImportTemplate;
    private Button? _btnExportTemplate;
    private ComboBox? _cmbTestField;
    private Button? _btnTestOcr;
    private Button? _btnViewRegion;
    private TextBox? _txtOcrResult;
    private Label? _lblOcrConfidence;
    private Button? _btnSaveOcrConfig;
    private Button? _btnResetOcrConfig;

    public MainForm()
    {
        InitializeComponent();
        _settings = ConfigManager.Load();
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 设置窗体
        this.Text = "蓝牙刷卡器工具 v1.0";
        this.Size = new Size(1000, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(900, 650);

        // 系统托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "蓝牙刷卡器工具 - 后台运行中",
            Visible = true
        };
        _notifyIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("显示窗口", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
        contextMenu.Items.Add("退出", null, (s, e) => { Application.Exit(); });
        _notifyIcon.ContextMenuStrip = contextMenu;

        // 创建标签页控件
        _tabControl = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(960, 550),
            Font = new Font("微软雅黑", 10)
        };

        // 添加四个标签页
        _tabControl.TabPages.Add(CreateBluetoothTab());
        _tabControl.TabPages.Add(CreateOcrTab());
        _tabControl.TabPages.Add(CreateServiceTab());
        _tabControl.TabPages.Add(CreateBackgroundTab());

        this.Controls.Add(_tabControl);

        // 日志窗口
        var lblLog = new Label
        {
            Text = "运行日志：",
            Location = new Point(10, 570),
            Size = new Size(100, 25),
            Font = new Font("微软雅黑", 10)
        };
        this.Controls.Add(lblLog);

        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(10, 600),
            Size = new Size(960, 100),
            Font = new Font("Consolas", 9),
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.LightGreen
        };
        this.Controls.Add(_txtLog);

        Log("=== 蓝牙刷卡器工具已启动 ===");
        Log("请在各标签页中进行配置");
        Log("");
    }

    private TabPage CreateBluetoothTab()
    {
        var tab = new TabPage("蓝牙配置");

        // 创建 BluetoothManager 实例
        _bluetoothManager = new BluetoothManager();
        _bluetoothManager.CardNumberReceived += OnCardNumberReceived;
        _bluetoothManager.LogMessage += OnBluetoothLog;

        // 注册 Raw Input
        _bluetoothManager.RegisterRawInput(this.Handle);

        int yPos = 20;

        // ===== 设备选择组 =====
        var grpDevice = new GroupBox
        {
            Text = "设备选择",
            Location = new Point(20, yPos),
            Size = new Size(900, 100),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpDevice);

        _btnSelectDevice = new Button
        {
            Text = "选择蓝牙刷卡器",
            Location = new Point(15, 30),
            Size = new Size(150, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnSelectDevice.Click += BtnSelectDevice_Click;
        grpDevice.Controls.Add(_btnSelectDevice);

        _lblCurrentDevice = new Label
        {
            Text = "当前设备: 未选择",
            Location = new Point(180, 30),
            Size = new Size(500, 35),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Red
        };
        grpDevice.Controls.Add(_lblCurrentDevice);

        _btnStartListening = new Button
        {
            Text = "开始监听",
            Location = new Point(15, 70),
            Size = new Size(100, 30),
            Font = new Font("微软雅黑", 9),
            Enabled = false
        };
        _btnStartListening.Click += BtnStartListening_Click;
        grpDevice.Controls.Add(_btnStartListening);

        _btnStopListening = new Button
        {
            Text = "停止监听",
            Location = new Point(125, 70),
            Size = new Size(100, 30),
            Font = new Font("微软雅黑", 9),
            Enabled = false
        };
        _btnStopListening.Click += BtnStopListening_Click;
        grpDevice.Controls.Add(_btnStopListening);

        _btnTestDevice = new Button
        {
            Text = "测试设备",
            Location = new Point(235, 70),
            Size = new Size(100, 30),
            Font = new Font("微软雅黑", 9)
        };
        _btnTestDevice.Click += BtnTestDevice_Click;
        grpDevice.Controls.Add(_btnTestDevice);

        yPos += 110;

        // ===== 配置参数组 =====
        var grpConfig = new GroupBox
        {
            Text = "配置参数",
            Location = new Point(20, yPos),
            Size = new Size(900, 100),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpConfig);

        var lblCardLength = new Label
        {
            Text = "卡号长度:",
            Location = new Point(15, 35),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpConfig.Controls.Add(lblCardLength);

        _numCardLength = new NumericUpDown
        {
            Location = new Point(105, 33),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            Minimum = 1,
            Maximum = 20,
            Value = _settings.Bluetooth.CardLength
        };
        grpConfig.Controls.Add(_numCardLength);

        var lblBits = new Label
        {
            Text = "位",
            Location = new Point(195, 35),
            Size = new Size(30, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft
        };
        grpConfig.Controls.Add(lblBits);

        _chkRequireEnter = new CheckBox
        {
            Text = "需要 Enter 键结束输入",
            Location = new Point(250, 35),
            Size = new Size(200, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Bluetooth.RequireEnter
        };
        grpConfig.Controls.Add(_chkRequireEnter);

        var lblHidKeywords = new Label
        {
            Text = "HID 关键字:",
            Location = new Point(15, 70),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpConfig.Controls.Add(lblHidKeywords);

        _txtHidKeywords = new TextBox
        {
            Location = new Point(105, 68),
            Size = new Size(300, 25),
            Font = new Font("微软雅黑", 9),
            Text = _settings.Bluetooth.HidKeywords
        };
        grpConfig.Controls.Add(_txtHidKeywords);

        var lblHidKeywordsDesc = new Label
        {
            Text = "（用分号分隔多个关键字，用于设备筛选）",
            Location = new Point(415, 70),
            Size = new Size(300, 25),
            Font = new Font("微软雅黑", 8),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft
        };
        grpConfig.Controls.Add(lblHidKeywordsDesc);

        yPos += 110;

        // ===== 实时监听组 =====
        var grpListen = new GroupBox
        {
            Text = "实时监听",
            Location = new Point(20, yPos),
            Size = new Size(900, 200),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpListen);

        var lblLatestCard = new Label
        {
            Text = "最新卡号:",
            Location = new Point(15, 30),
            Size = new Size(80, 30),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpListen.Controls.Add(lblLatestCard);

        _txtLatestCard = new TextBox
        {
            Location = new Point(105, 28),
            Size = new Size(250, 30),
            Font = new Font("Consolas", 16, FontStyle.Bold),
            ReadOnly = true,
            BackColor = Color.LightYellow,
            ForeColor = Color.Red,
            TextAlign = HorizontalAlignment.Center,
            Text = "0000000000"
        };
        grpListen.Controls.Add(_txtLatestCard);

        var lblListenLog = new Label
        {
            Text = "监听日志:",
            Location = new Point(15, 70),
            Size = new Size(80, 20),
            Font = new Font("微软雅黑", 9)
        };
        grpListen.Controls.Add(lblListenLog);

        _txtListenLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(15, 95),
            Size = new Size(870, 90),
            Font = new Font("Consolas", 8),
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.LightGreen
        };
        grpListen.Controls.Add(_txtListenLog);

        yPos += 210;

        // ===== 底部按钮 =====
        _btnSaveBluetoothConfig = new Button
        {
            Text = "保存配置",
            Location = new Point(720, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnSaveBluetoothConfig.Click += BtnSaveBluetoothConfig_Click;
        tab.Controls.Add(_btnSaveBluetoothConfig);

        _btnResetBluetoothConfig = new Button
        {
            Text = "恢复默认",
            Location = new Point(830, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnResetBluetoothConfig.Click += BtnResetBluetoothConfig_Click;
        tab.Controls.Add(_btnResetBluetoothConfig);

        // 加载配置
        LoadBluetoothConfig();

        return tab;
    }

    private TabPage CreateOcrTab()
    {
        var tab = new TabPage("OCR配置");

        // 创建 OcrService 实例并异步初始化
        _ocrService = new OcrService();
        Task.Run(async () =>
        {
            try
            {
                await _ocrService.InitializeAsync();
                this.Invoke(new Action(() => Log("OCR 引擎初始化完成")));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => Log($"OCR 引擎初始化失败: {ex.Message}")));
            }
        });

        int yPos = 20;

        // ===== 字段列表组 =====
        var grpFields = new GroupBox
        {
            Text = "字段列表",
            Location = new Point(20, yPos),
            Size = new Size(900, 280),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpFields);

        // 按钮行
        _btnAddField = new Button
        {
            Text = "新增字段",
            Location = new Point(15, 25),
            Size = new Size(100, 30),
            Font = new Font("微软雅黑", 9)
        };
        _btnAddField.Click += BtnAddField_Click;
        grpFields.Controls.Add(_btnAddField);

        _btnImportTemplate = new Button
        {
            Text = "导入模板",
            Location = new Point(125, 25),
            Size = new Size(100, 30),
            Font = new Font("微软雅黑", 9)
        };
        _btnImportTemplate.Click += BtnImportTemplate_Click;
        grpFields.Controls.Add(_btnImportTemplate);

        _btnExportTemplate = new Button
        {
            Text = "导出模板",
            Location = new Point(235, 25),
            Size = new Size(100, 30),
            Font = new Font("微软雅黑", 9)
        };
        _btnExportTemplate.Click += BtnExportTemplate_Click;
        grpFields.Controls.Add(_btnExportTemplate);

        // DataGridView
        _dgvFields = new DataGridView
        {
            Location = new Point(15, 65),
            Size = new Size(870, 200),
            Font = new Font("微软雅黑", 9),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White
        };

        // 添加列
        _dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Index",
            HeaderText = "序号",
            Width = 50,
            ReadOnly = true
        });

        _dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "字段名",
            Width = 120,
            ReadOnly = true
        });

        _dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ParamName",
            HeaderText = "参数名",
            Width = 120,
            ReadOnly = true
        });

        _dgvFields.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "启用",
            Width = 60,
            ReadOnly = false
        });

        _dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RegionStatus",
            HeaderText = "区域状态",
            Width = 100,
            ReadOnly = true
        });

        _dgvFields.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Edit",
            HeaderText = "操作",
            Text = "编辑",
            UseColumnTextForButtonValue = true,
            Width = 80
        });

        _dgvFields.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Delete",
            HeaderText = "",
            Text = "删除",
            UseColumnTextForButtonValue = true,
            Width = 80
        });

        _dgvFields.CellContentClick += DgvFields_CellContentClick;
        _dgvFields.CellValueChanged += DgvFields_CellValueChanged;
        grpFields.Controls.Add(_dgvFields);

        yPos += 290;

        // ===== 快速测试组 =====
        var grpTest = new GroupBox
        {
            Text = "快速测试",
            Location = new Point(20, yPos),
            Size = new Size(900, 120),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpTest);

        var lblTestField = new Label
        {
            Text = "选择字段:",
            Location = new Point(15, 30),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpTest.Controls.Add(lblTestField);

        _cmbTestField = new ComboBox
        {
            Location = new Point(105, 28),
            Size = new Size(200, 25),
            Font = new Font("微软雅黑", 9),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        grpTest.Controls.Add(_cmbTestField);

        _btnTestOcr = new Button
        {
            Text = "测试识别",
            Location = new Point(315, 26),
            Size = new Size(90, 30),
            Font = new Font("微软雅黑", 9)
        };
        _btnTestOcr.Click += BtnTestOcr_Click;
        grpTest.Controls.Add(_btnTestOcr);

        _btnViewRegion = new Button
        {
            Text = "查看区域",
            Location = new Point(415, 26),
            Size = new Size(90, 30),
            Font = new Font("微软雅黑", 9)
        };
        _btnViewRegion.Click += BtnViewRegion_Click;
        grpTest.Controls.Add(_btnViewRegion);

        var lblOcrResult = new Label
        {
            Text = "识别结果:",
            Location = new Point(15, 70),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpTest.Controls.Add(lblOcrResult);

        _txtOcrResult = new TextBox
        {
            Location = new Point(105, 68),
            Size = new Size(300, 25),
            Font = new Font("微软雅黑", 9),
            ReadOnly = true,
            BackColor = Color.LightYellow
        };
        grpTest.Controls.Add(_txtOcrResult);

        var lblConfidenceLabel = new Label
        {
            Text = "置信度:",
            Location = new Point(420, 70),
            Size = new Size(60, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        grpTest.Controls.Add(lblConfidenceLabel);

        _lblOcrConfidence = new Label
        {
            Text = "0.00%",
            Location = new Point(490, 70),
            Size = new Size(100, 25),
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        grpTest.Controls.Add(_lblOcrConfidence);

        yPos += 130;

        // ===== 底部按钮 =====
        _btnSaveOcrConfig = new Button
        {
            Text = "保存配置",
            Location = new Point(720, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnSaveOcrConfig.Click += BtnSaveOcrConfig_Click;
        tab.Controls.Add(_btnSaveOcrConfig);

        _btnResetOcrConfig = new Button
        {
            Text = "恢复默认",
            Location = new Point(830, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnResetOcrConfig.Click += BtnResetOcrConfig_Click;
        tab.Controls.Add(_btnResetOcrConfig);

        // 加载配置
        LoadOcrConfig();

        return tab;
    }

    private TabPage CreateServiceTab()
    {
        var tab = new TabPage("服务配置");

        var label = new Label
        {
            Text = "服务配置页面（待实现）",
            Location = new Point(20, 20),
            Size = new Size(900, 30),
            Font = new Font("微软雅黑", 12)
        };
        tab.Controls.Add(label);

        return tab;
    }

    private TabPage CreateBackgroundTab()
    {
        var tab = new TabPage("后台配置");

        var label = new Label
        {
            Text = "后台配置页面（待实现）",
            Location = new Point(20, 20),
            Size = new Size(900, 30),
            Font = new Font("微软雅黑", 12)
        };
        tab.Controls.Add(label);

        return tab;
    }

    public void Log(string message)
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

    #region 蓝牙配置页事件处理

    private void LoadBluetoothConfig()
    {
        // 应用配置到 BluetoothManager
        _bluetoothManager?.Configure(_settings.Bluetooth.CardLength, _settings.Bluetooth.RequireEnter);

        // 尝试恢复上次选择的设备
        if (!string.IsNullOrEmpty(_settings.Bluetooth.LastDeviceHandle) &&
            !string.IsNullOrEmpty(_settings.Bluetooth.LastDeviceName))
        {
            try
            {
                IntPtr handle = new IntPtr(long.Parse(_settings.Bluetooth.LastDeviceHandle));
                if (handle != IntPtr.Zero)
                {
                    _bluetoothManager?.SelectDevice(handle, _settings.Bluetooth.LastDeviceName);
                    UpdateCurrentDeviceLabel(_settings.Bluetooth.LastDeviceName, true);
                    _btnStartListening!.Enabled = true;
                }
            }
            catch
            {
                // 忽略恢复失败
            }
        }
    }

    private void BtnSelectDevice_Click(object? sender, EventArgs e)
    {
        if (_bluetoothManager == null) return;

        try
        {
            var devices = _bluetoothManager.GetAvailableDevices();

            if (devices.Count == 0)
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

            foreach (var device in devices)
            {
                listBox.Items.Add(device.FriendlyName);
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
                var selected = devices[listBox.SelectedIndex];
                _bluetoothManager.SelectDevice(selected.Handle, selected.Name);
                UpdateCurrentDeviceLabel(selected.Name, true);
                _btnStartListening!.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            Log($"选择设备失败：{ex.Message}");
            MessageBox.Show($"选择设备失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnStartListening_Click(object? sender, EventArgs e)
    {
        if (_bluetoothManager == null) return;

        // 应用当前配置
        int cardLength = (int)_numCardLength!.Value;
        bool requireEnter = _chkRequireEnter!.Checked;
        _bluetoothManager.Configure(cardLength, requireEnter);

        // 更新按钮状态
        _btnStartListening!.Enabled = false;
        _btnStopListening!.Enabled = true;
        _btnSelectDevice!.Enabled = false;

        Log("开始监听蓝牙刷卡器...");
    }

    private void BtnStopListening_Click(object? sender, EventArgs e)
    {
        if (_bluetoothManager == null) return;

        _bluetoothManager.StopListening();

        // 更新按钮状态
        _btnStartListening!.Enabled = true;
        _btnStopListening!.Enabled = false;
        _btnSelectDevice!.Enabled = true;

        UpdateCurrentDeviceLabel("", false);
        Log("已停止监听");
    }

    private void BtnTestDevice_Click(object? sender, EventArgs e)
    {
        if (_bluetoothManager == null) return;

        var devices = _bluetoothManager.GetAvailableDevices();
        MessageBox.Show($"找到 {devices.Count} 个键盘设备", "设备测试", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnSaveBluetoothConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            // 保存配置
            _settings.Bluetooth.CardLength = (int)_numCardLength!.Value;
            _settings.Bluetooth.RequireEnter = _chkRequireEnter!.Checked;
            _settings.Bluetooth.HidKeywords = _txtHidKeywords!.Text;

            if (_bluetoothManager != null && _bluetoothManager.IsListening)
            {
                _settings.Bluetooth.LastDeviceHandle = _bluetoothManager.CurrentDeviceHandle.ToString();
                _settings.Bluetooth.LastDeviceName = _bluetoothManager.CurrentDeviceName;
            }

            ConfigManager.Save(_settings);
            Log("蓝牙配置已保存");
            MessageBox.Show("配置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"保存配置失败：{ex.Message}");
            MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnResetBluetoothConfig_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("确定要恢复默认配置吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            _numCardLength!.Value = 10;
            _chkRequireEnter!.Checked = false;
            _txtHidKeywords!.Text = "Bluetooth;HID";
            Log("已恢复默认配置");
        }
    }

    private void OnCardNumberReceived(object? sender, CardNumberReceivedEventArgs e)
    {
        // 更新最新卡号显示
        if (_txtLatestCard != null)
        {
            if (_txtLatestCard.InvokeRequired)
            {
                _txtLatestCard.Invoke(new Action(() => OnCardNumberReceived(sender, e)));
                return;
            }

            _txtLatestCard.Text = e.CardNumber;
            _txtLatestCard.BackColor = Color.LightGreen;

            // 2 秒后恢复背景色
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += (s, args) =>
            {
                _txtLatestCard.BackColor = Color.LightYellow;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        // 播放提示音
        System.Media.SystemSounds.Beep.Play();

        // 显示托盘通知
        _notifyIcon?.ShowBalloonTip(2000, "检测到卡号", $"卡号：{e.CardNumber}", ToolTipIcon.Info);

        // 记录到主日志
        Log($"接收到卡号: {e.CardNumber}");
    }

    private void OnBluetoothLog(object? sender, LogEventArgs e)
    {
        // 输出到监听日志
        if (_txtListenLog != null)
        {
            if (_txtListenLog.InvokeRequired)
            {
                _txtListenLog.Invoke(new Action(() => OnBluetoothLog(sender, e)));
                return;
            }

            _txtListenLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {e.Message}\r\n");
            _txtListenLog.SelectionStart = _txtListenLog.Text.Length;
            _txtListenLog.ScrollToCaret();
        }
    }

    private void UpdateCurrentDeviceLabel(string deviceName, bool isConnected)
    {
        if (_lblCurrentDevice == null) return;

        if (_lblCurrentDevice.InvokeRequired)
        {
            _lblCurrentDevice.Invoke(new Action(() => UpdateCurrentDeviceLabel(deviceName, isConnected)));
            return;
        }

        if (isConnected && !string.IsNullOrEmpty(deviceName))
        {
            string shortName = GetShortDeviceName(deviceName);
            _lblCurrentDevice.Text = $"当前设备: {shortName}";
            _lblCurrentDevice.ForeColor = Color.Green;
        }
        else
        {
            _lblCurrentDevice.Text = "当前设备: 未选择";
            _lblCurrentDevice.ForeColor = Color.Red;
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

    #endregion

    #region OCR 配置页事件处理

    private void LoadOcrConfig()
    {
        RefreshFieldsList();
        RefreshTestFieldComboBox();
    }

    private void RefreshFieldsList()
    {
        if (_dgvFields == null) return;

        _dgvFields.Rows.Clear();

        for (int i = 0; i < _settings.Ocr.Fields.Count; i++)
        {
            var field = _settings.Ocr.Fields[i];
            string regionStatus = field.Region != null && field.Region.Width > 0 && field.Region.Height > 0
                ? "已定位"
                : "未定位";

            _dgvFields.Rows.Add(
                i + 1,
                field.Name,
                field.ParamName,
                field.Enabled,
                regionStatus
            );
        }
    }

    private void RefreshTestFieldComboBox()
    {
        if (_cmbTestField == null) return;

        _cmbTestField.Items.Clear();

        foreach (var field in _settings.Ocr.Fields)
        {
            if (field.Enabled)
            {
                _cmbTestField.Items.Add(field.Name);
            }
        }

        if (_cmbTestField.Items.Count > 0)
        {
            _cmbTestField.SelectedIndex = 0;
        }
    }

    private void BtnAddField_Click(object? sender, EventArgs e)
    {
        if (_ocrService == null)
        {
            MessageBox.Show("OCR 服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var dialog = new FieldEditDialog(_ocrService, _settings.Ocr.Fields);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var newField = dialog.GetField();
                _settings.Ocr.Fields.Add(newField);
                RefreshFieldsList();
                RefreshTestFieldComboBox();
                Log($"新增字段: {newField.Name}");
            }
        }
        catch (Exception ex)
        {
            Log($"新增字段失败: {ex.Message}");
            MessageBox.Show($"新增字段失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvFields_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_dgvFields == null || e.RowIndex < 0) return;

        var columnName = _dgvFields.Columns[e.ColumnIndex].Name;

        if (columnName == "Edit")
        {
            // 编辑字段
            EditField(e.RowIndex);
        }
        else if (columnName == "Delete")
        {
            // 删除字段
            DeleteField(e.RowIndex);
        }
    }

    private void EditField(int rowIndex)
    {
        if (_ocrService == null || rowIndex >= _settings.Ocr.Fields.Count) return;

        try
        {
            var field = _settings.Ocr.Fields[rowIndex];
            using var dialog = new FieldEditDialog(_ocrService, _settings.Ocr.Fields, field);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RefreshFieldsList();
                RefreshTestFieldComboBox();
                Log($"编辑字段: {field.Name}");
            }
        }
        catch (Exception ex)
        {
            Log($"编辑字段失败: {ex.Message}");
            MessageBox.Show($"编辑字段失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteField(int rowIndex)
    {
        if (rowIndex >= _settings.Ocr.Fields.Count) return;

        var field = _settings.Ocr.Fields[rowIndex];
        var result = MessageBox.Show(
            $"确定要删除字段 '{field.Name}' 吗？",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            _settings.Ocr.Fields.RemoveAt(rowIndex);
            RefreshFieldsList();
            RefreshTestFieldComboBox();
            Log($"删除字段: {field.Name}");
        }
    }

    private void DgvFields_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_dgvFields == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var columnName = _dgvFields.Columns[e.ColumnIndex].Name;

        if (columnName == "Enabled" && e.RowIndex < _settings.Ocr.Fields.Count)
        {
            // 更新启用状态
            var enabled = (bool)_dgvFields.Rows[e.RowIndex].Cells["Enabled"].Value;
            _settings.Ocr.Fields[e.RowIndex].Enabled = enabled;
            RefreshTestFieldComboBox();
            Log($"字段 '{_settings.Ocr.Fields[e.RowIndex].Name}' 已{(enabled ? "启用" : "禁用")}");
        }
    }

    private void BtnTestOcr_Click(object? sender, EventArgs e)
    {
        if (_ocrService == null)
        {
            MessageBox.Show("OCR 服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_cmbTestField == null || _cmbTestField.SelectedIndex < 0)
        {
            MessageBox.Show("请选择要测试的字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var fieldName = _cmbTestField.SelectedItem?.ToString();
            var field = _settings.Ocr.Fields.FirstOrDefault(f => f.Name == fieldName);

            if (field == null || field.Region == null)
            {
                MessageBox.Show("字段未定位识别区域", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnTestOcr!.Enabled = false;
            _btnTestOcr.Text = "识别中...";
            _txtOcrResult!.Text = "正在识别...";
            _lblOcrConfidence!.Text = "0.00%";
            Application.DoEvents();

            // 截取屏幕
            using var screenshot = _ocrService.CaptureScreen();

            // 识别
            var result = _ocrService.RecognizeRegion(screenshot, field.Region);

            if (result.Success)
            {
                _txtOcrResult.Text = string.IsNullOrEmpty(result.Text) ? "(未识别到文本)" : result.Text;
                _lblOcrConfidence.Text = $"{result.Confidence:P2}";
                _lblOcrConfidence.ForeColor = result.Confidence >= 0.85 ? Color.Green : Color.Orange;
                Log($"测试识别 '{field.Name}': {result.Text} (置信度: {result.Confidence:P2})");
            }
            else
            {
                _txtOcrResult.Text = result.ErrorMessage;
                _lblOcrConfidence.Text = "0.00%";
                _lblOcrConfidence.ForeColor = Color.Red;
                Log($"测试识别失败: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Log($"测试识别失败: {ex.Message}");
            MessageBox.Show($"测试识别失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (_btnTestOcr != null)
            {
                _btnTestOcr.Enabled = true;
                _btnTestOcr.Text = "测试识别";
            }
        }
    }

    private void BtnViewRegion_Click(object? sender, EventArgs e)
    {
        if (_ocrService == null)
        {
            MessageBox.Show("OCR 服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_cmbTestField == null || _cmbTestField.SelectedIndex < 0)
        {
            MessageBox.Show("请选择要查看的字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var fieldName = _cmbTestField.SelectedItem?.ToString();
            var field = _settings.Ocr.Fields.FirstOrDefault(f => f.Name == fieldName);

            if (field == null || field.Region == null)
            {
                MessageBox.Show("字段未定位识别区域", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 截取当前屏幕
            using var screenshot = _ocrService.CaptureScreen();

            // 创建预览窗口
            var previewForm = new Form
            {
                Text = $"区域预览 - {field.Name}",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent
            };

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // 在截图上绘制选择框
            using var graphics = Graphics.FromImage(screenshot);
            using var pen = new Pen(Color.Red, 3);
            var region = new Rectangle(field.Region.X, field.Region.Y, field.Region.Width, field.Region.Height);
            graphics.DrawRectangle(pen, region);

            pictureBox.Image = (Bitmap)screenshot.Clone();
            previewForm.Controls.Add(pictureBox);
            previewForm.ShowDialog();
        }
        catch (Exception ex)
        {
            Log($"查看区域失败: {ex.Message}");
            MessageBox.Show($"查看区域失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnImportTemplate_Click(object? sender, EventArgs e)
    {
        try
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "导入 OCR 模板",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var json = File.ReadAllText(openFileDialog.FileName);
                var fields = System.Text.Json.JsonSerializer.Deserialize<List<OcrField>>(json);

                if (fields != null)
                {
                    _settings.Ocr.Fields = fields;
                    RefreshFieldsList();
                    RefreshTestFieldComboBox();
                    Log($"导入模板成功: {fields.Count} 个字段");
                    MessageBox.Show($"导入成功，共 {fields.Count} 个字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"导入模板失败: {ex.Message}");
            MessageBox.Show($"导入模板失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnExportTemplate_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_settings.Ocr.Fields.Count == 0)
            {
                MessageBox.Show("没有可导出的字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "导出 OCR 模板",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                FileName = "ocr_template.json"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = System.Text.Json.JsonSerializer.Serialize(_settings.Ocr.Fields, options);
                File.WriteAllText(saveFileDialog.FileName, json, System.Text.Encoding.UTF8);

                Log($"导出模板成功: {_settings.Ocr.Fields.Count} 个字段");
                MessageBox.Show("导出成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log($"导出模板失败: {ex.Message}");
            MessageBox.Show($"导出模板失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSaveOcrConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            ConfigManager.Save(_settings);
            Log("OCR 配置已保存");
            MessageBox.Show("配置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"保存配置失败: {ex.Message}");
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnResetOcrConfig_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "确定要清空所有 OCR 字段吗？此操作不可恢复！",
            "确认",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (result == DialogResult.Yes)
        {
            _settings.Ocr.Fields.Clear();
            RefreshFieldsList();
            RefreshTestFieldComboBox();
            Log("已清空 OCR 字段");
        }
    }

    #endregion

    protected override void WndProc(ref Message m)
    {
        // 处理 Raw Input 消息
        if (m.Msg == 0x00FF && _bluetoothManager != null)
        {
            _bluetoothManager.ProcessRawInputMessage(m.LParam);
        }
        base.WndProc(ref m);
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
            // 保存配置
            ConfigManager.Save(_settings);

            // 清理资源
            _bluetoothManager?.Dispose();
            _ocrService?.Dispose();
            _notifyIcon?.Dispose();

            base.OnFormClosing(e);
        }
    }
}
