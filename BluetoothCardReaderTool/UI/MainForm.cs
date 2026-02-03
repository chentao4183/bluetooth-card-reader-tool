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

    // 服务配置页控件
    private RadioButton? _rbV1;
    private RadioButton? _rbV2;
    private GroupBox? _grpV1Config;
    private GroupBox? _grpV2Config;
    private TextBox? _txtV1VerifyUrl;
    private TextBox? _txtV1BindUrl;
    private TextBox? _txtV2VerifyUrl;
    private TextBox? _txtV2BindUrl;
    private CheckBox? _chkEnableVerify;
    private CheckBox? _chkShowResultPopup;
    private Button? _btnTestConnection;
    private Button? _btnSaveServiceConfig;

    // 后台配置页控件
    private RadioButton? _rbManualSubmit;
    private RadioButton? _rbAutoSubmit;
    private NumericUpDown? _numCountdown;
    private CheckBox? _chkAutoStart;
    private CheckBox? _chkEnableFloatingBall;
    private CheckBox? _chkEnableService;
    private Button? _btnSaveBackgroundConfig;
    private Button? _btnResetBackgroundConfig;

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

        int yPos = 20;

        // ===== 系统版本选择 =====
        var lblVersion = new Label
        {
            Text = "系统版本:",
            Location = new Point(20, yPos),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleRight
        };
        tab.Controls.Add(lblVersion);

        _rbV1 = new RadioButton
        {
            Text = "V1.0",
            Location = new Point(110, yPos),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Service.Version == "V1.0"
        };
        _rbV1.CheckedChanged += OnVersionChanged;
        tab.Controls.Add(_rbV1);

        _rbV2 = new RadioButton
        {
            Text = "V2.0",
            Location = new Point(200, yPos),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Service.Version == "V2.0"
        };
        _rbV2.CheckedChanged += OnVersionChanged;
        tab.Controls.Add(_rbV2);

        yPos += 40;

        // ===== V1 系统配置组 =====
        _grpV1Config = new GroupBox
        {
            Text = "V1 系统配置",
            Location = new Point(20, yPos),
            Size = new Size(900, 100),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(_grpV1Config);

        var lblV1Verify = new Label
        {
            Text = "验证接口:",
            Location = new Point(15, 35),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        _grpV1Config.Controls.Add(lblV1Verify);

        _txtV1VerifyUrl = new TextBox
        {
            Location = new Point(105, 33),
            Size = new Size(770, 25),
            Font = new Font("微软雅黑", 9),
            Text = _settings.Service.V1.VerifyUrl
        };
        _grpV1Config.Controls.Add(_txtV1VerifyUrl);

        var lblV1Bind = new Label
        {
            Text = "绑定接口:",
            Location = new Point(15, 70),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        _grpV1Config.Controls.Add(lblV1Bind);

        _txtV1BindUrl = new TextBox
        {
            Location = new Point(105, 68),
            Size = new Size(770, 25),
            Font = new Font("微软雅黑", 9),
            Text = _settings.Service.V1.BindUrl
        };
        _grpV1Config.Controls.Add(_txtV1BindUrl);

        yPos += 110;

        // ===== V2 系统配置组 =====
        _grpV2Config = new GroupBox
        {
            Text = "V2 系统配置",
            Location = new Point(20, yPos),
            Size = new Size(900, 100),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(_grpV2Config);

        var lblV2Verify = new Label
        {
            Text = "验证接口:",
            Location = new Point(15, 35),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        _grpV2Config.Controls.Add(lblV2Verify);

        _txtV2VerifyUrl = new TextBox
        {
            Location = new Point(105, 33),
            Size = new Size(770, 25),
            Font = new Font("微软雅黑", 9),
            Text = _settings.Service.V2.VerifyUrl
        };
        _grpV2Config.Controls.Add(_txtV2VerifyUrl);

        var lblV2Bind = new Label
        {
            Text = "绑定接口:",
            Location = new Point(15, 70),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleRight
        };
        _grpV2Config.Controls.Add(lblV2Bind);

        _txtV2BindUrl = new TextBox
        {
            Location = new Point(105, 68),
            Size = new Size(770, 25),
            Font = new Font("微软雅黑", 9),
            Text = _settings.Service.V2.BindUrl
        };
        _grpV2Config.Controls.Add(_txtV2BindUrl);

        yPos += 110;

        // ===== 功能设置组 =====
        var grpFeatures = new GroupBox
        {
            Text = "功能设置",
            Location = new Point(20, yPos),
            Size = new Size(900, 80),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpFeatures);

        _chkEnableVerify = new CheckBox
        {
            Text = "启用洗消验证",
            Location = new Point(15, 30),
            Size = new Size(200, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Service.EnableVerify
        };
        grpFeatures.Controls.Add(_chkEnableVerify);

        _chkShowResultPopup = new CheckBox
        {
            Text = "显示结果弹窗",
            Location = new Point(230, 30),
            Size = new Size(200, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Service.ShowResultPopup
        };
        grpFeatures.Controls.Add(_chkShowResultPopup);

        yPos += 90;

        // ===== 底部按钮 =====
        _btnTestConnection = new Button
        {
            Text = "测试连接",
            Location = new Point(610, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnTestConnection.Click += BtnTestConnection_Click;
        tab.Controls.Add(_btnTestConnection);

        _btnSaveServiceConfig = new Button
        {
            Text = "保存配置",
            Location = new Point(720, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnSaveServiceConfig.Click += BtnSaveServiceConfig_Click;
        tab.Controls.Add(_btnSaveServiceConfig);

        var btnResetServiceConfig = new Button
        {
            Text = "恢复默认",
            Location = new Point(830, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        btnResetServiceConfig.Click += BtnResetServiceConfig_Click;
        tab.Controls.Add(btnResetServiceConfig);

        // 加载配置并更新 UI
        LoadServiceConfig();

        return tab;
    }

    private TabPage CreateBackgroundTab()
    {
        var tab = new TabPage("后台配置");

        int yPos = 20;

        // ===== 提交模式组 =====
        var grpSubmitMode = new GroupBox
        {
            Text = "提交模式",
            Location = new Point(20, yPos),
            Size = new Size(900, 100),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpSubmitMode);

        _rbManualSubmit = new RadioButton
        {
            Text = "手动提交 - 需要点击提交按钮",
            Location = new Point(15, 30),
            Size = new Size(300, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Background.SubmitMode == "manual"
        };
        _rbManualSubmit.CheckedChanged += OnSubmitModeChanged;
        grpSubmitMode.Controls.Add(_rbManualSubmit);

        _rbAutoSubmit = new RadioButton
        {
            Text = "自动提交 - 倒计时后自动提交:",
            Location = new Point(15, 65),
            Size = new Size(250, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Background.SubmitMode == "auto"
        };
        _rbAutoSubmit.CheckedChanged += OnSubmitModeChanged;
        grpSubmitMode.Controls.Add(_rbAutoSubmit);

        _numCountdown = new NumericUpDown
        {
            Location = new Point(275, 63),
            Size = new Size(60, 25),
            Font = new Font("微软雅黑", 10),
            Minimum = 1,
            Maximum = 60,
            Value = _settings.Background.Countdown,
            Enabled = _settings.Background.SubmitMode == "auto"
        };
        grpSubmitMode.Controls.Add(_numCountdown);

        var lblSeconds = new Label
        {
            Text = "秒",
            Location = new Point(345, 65),
            Size = new Size(30, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft
        };
        grpSubmitMode.Controls.Add(lblSeconds);

        yPos += 110;

        // ===== 启动设置组 =====
        var grpStartup = new GroupBox
        {
            Text = "启动设置",
            Location = new Point(20, yPos),
            Size = new Size(900, 80),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpStartup);

        _chkAutoStart = new CheckBox
        {
            Text = "开机自启动",
            Location = new Point(15, 30),
            Size = new Size(200, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Background.AutoStart
        };
        _chkAutoStart.CheckedChanged += OnAutoStartChanged;
        grpStartup.Controls.Add(_chkAutoStart);

        yPos += 90;

        // ===== 功能开关组 =====
        var grpFeatures = new GroupBox
        {
            Text = "功能开关",
            Location = new Point(20, yPos),
            Size = new Size(900, 100),
            Font = new Font("微软雅黑", 10)
        };
        tab.Controls.Add(grpFeatures);

        _chkEnableFloatingBall = new CheckBox
        {
            Text = "启用浮球输入（手动输入卡号）",
            Location = new Point(15, 30),
            Size = new Size(300, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Background.EnableFloatingBall
        };
        grpFeatures.Controls.Add(_chkEnableFloatingBall);

        _chkEnableService = new CheckBox
        {
            Text = "启用后台服务（监听刷卡事件）",
            Location = new Point(15, 65),
            Size = new Size(300, 25),
            Font = new Font("微软雅黑", 10),
            Checked = _settings.Background.EnableService
        };
        grpFeatures.Controls.Add(_chkEnableService);

        yPos += 110;

        // ===== 底部按钮 =====
        _btnSaveBackgroundConfig = new Button
        {
            Text = "保存配置",
            Location = new Point(720, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnSaveBackgroundConfig.Click += BtnSaveBackgroundConfig_Click;
        tab.Controls.Add(_btnSaveBackgroundConfig);

        _btnResetBackgroundConfig = new Button
        {
            Text = "恢复默认",
            Location = new Point(830, yPos),
            Size = new Size(100, 35),
            Font = new Font("微软雅黑", 10)
        };
        _btnResetBackgroundConfig.Click += BtnResetBackgroundConfig_Click;
        tab.Controls.Add(_btnResetBackgroundConfig);

        // 加载配置
        LoadBackgroundConfig();

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

    #region 服务配置页事件处理

    private void LoadServiceConfig()
    {
        // 更新版本选择高亮
        UpdateVersionGroupBoxStyle();
    }

    private void OnVersionChanged(object? sender, EventArgs e)
    {
        UpdateVersionGroupBoxStyle();
    }

    private void UpdateVersionGroupBoxStyle()
    {
        if (_grpV1Config == null || _grpV2Config == null) return;

        if (_rbV1?.Checked == true)
        {
            _grpV1Config.Font = new Font("微软雅黑", 10, FontStyle.Bold);
            _grpV1Config.ForeColor = Color.Blue;
            _grpV2Config.Font = new Font("微软雅黑", 10, FontStyle.Regular);
            _grpV2Config.ForeColor = Color.Black;
        }
        else
        {
            _grpV1Config.Font = new Font("微软雅黑", 10, FontStyle.Regular);
            _grpV1Config.ForeColor = Color.Black;
            _grpV2Config.Font = new Font("微软雅黑", 10, FontStyle.Bold);
            _grpV2Config.ForeColor = Color.Blue;
        }
    }

    private async void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        if (_btnTestConnection == null) return;

        try
        {
            _btnTestConnection.Enabled = false;
            _btnTestConnection.Text = "测试中...";
            Application.DoEvents();

            string verifyUrl = "";
            string bindUrl = "";

            if (_rbV1?.Checked == true)
            {
                verifyUrl = _txtV1VerifyUrl?.Text ?? "";
                bindUrl = _txtV1BindUrl?.Text ?? "";
            }
            else
            {
                verifyUrl = _txtV2VerifyUrl?.Text ?? "";
                bindUrl = _txtV2BindUrl?.Text ?? "";
            }

            // 验证 URL 格式
            if (string.IsNullOrWhiteSpace(verifyUrl) && string.IsNullOrWhiteSpace(bindUrl))
            {
                MessageBox.Show("请至少输入一个接口 URL", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var results = new List<string>();

            // 测试验证接口
            if (!string.IsNullOrWhiteSpace(verifyUrl))
            {
                if (!Uri.TryCreate(verifyUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    results.Add($"验证接口 URL 格式无效: {verifyUrl}");
                }
                else
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(5);
                        var response = await client.GetAsync(verifyUrl);
                        results.Add($"验证接口: {(response.IsSuccessStatusCode ? "连接成功" : $"返回状态码 {response.StatusCode}")}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"验证接口: 连接失败 - {ex.Message}");
                    }
                }
            }

            // 测试绑定接口
            if (!string.IsNullOrWhiteSpace(bindUrl))
            {
                if (!Uri.TryCreate(bindUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    results.Add($"绑定接口 URL 格式无效: {bindUrl}");
                }
                else
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(5);
                        var response = await client.GetAsync(bindUrl);
                        results.Add($"绑定接口: {(response.IsSuccessStatusCode ? "连接成功" : $"返回状态码 {response.StatusCode}")}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"绑定接口: 连接失败 - {ex.Message}");
                    }
                }
            }

            string message = string.Join("\n", results);
            Log($"接口测试结果:\n{message}");
            MessageBox.Show(message, "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"测试连接失败: {ex.Message}");
            MessageBox.Show($"测试连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (_btnTestConnection != null)
            {
                _btnTestConnection.Enabled = true;
                _btnTestConnection.Text = "测试连接";
            }
        }
    }

    private void BtnSaveServiceConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            // 保存版本选择
            _settings.Service.Version = _rbV1?.Checked == true ? "V1.0" : "V2.0";

            // 保存 V1 配置
            _settings.Service.V1.VerifyUrl = _txtV1VerifyUrl?.Text ?? "";
            _settings.Service.V1.BindUrl = _txtV1BindUrl?.Text ?? "";

            // 保存 V2 配置
            _settings.Service.V2.VerifyUrl = _txtV2VerifyUrl?.Text ?? "";
            _settings.Service.V2.BindUrl = _txtV2BindUrl?.Text ?? "";

            // 保存功能开关
            _settings.Service.EnableVerify = _chkEnableVerify?.Checked ?? true;
            _settings.Service.ShowResultPopup = _chkShowResultPopup?.Checked ?? true;

            ConfigManager.Save(_settings);
            Log("服务配置已保存");
            MessageBox.Show("配置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"保存配置失败: {ex.Message}");
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnResetServiceConfig_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("确定要恢复默认配置吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            // 恢复 V2 默认配置
            if (_txtV2VerifyUrl != null)
                _txtV2VerifyUrl.Text = "http://10.10.5.116:8080/api/verify";
            if (_txtV2BindUrl != null)
                _txtV2BindUrl.Text = "http://10.10.5.116:8080/api/bind";

            // 清空 V1 配置
            if (_txtV1VerifyUrl != null)
                _txtV1VerifyUrl.Text = "";
            if (_txtV1BindUrl != null)
                _txtV1BindUrl.Text = "";

            // 恢复功能开关
            if (_chkEnableVerify != null)
                _chkEnableVerify.Checked = true;
            if (_chkShowResultPopup != null)
                _chkShowResultPopup.Checked = true;

            // 选择 V2.0
            if (_rbV2 != null)
                _rbV2.Checked = true;

            Log("已恢复默认配置");
        }
    }

    #endregion

    #region 后台配置页事件处理

    private void LoadBackgroundConfig()
    {
        // 配置已在控件初始化时加载，这里可以添加额外的初始化逻辑
    }

    private void OnSubmitModeChanged(object? sender, EventArgs e)
    {
        if (_numCountdown == null) return;

        // 根据提交模式启用/禁用倒计时输入框
        _numCountdown.Enabled = _rbAutoSubmit?.Checked == true;
    }

    private void OnAutoStartChanged(object? sender, EventArgs e)
    {
        if (_chkAutoStart == null) return;

        try
        {
            if (_chkAutoStart.Checked)
            {
                // 添加到开机自启动
                if (AutoStartupManager.AddToStartup())
                {
                    Log("已添加到开机自启动");
                }
                else
                {
                    Log("添加到开机自启动失败");
                    MessageBox.Show("添加到开机自启动失败，请检查权限", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _chkAutoStart.Checked = false;
                }
            }
            else
            {
                // 从开机自启动中移除
                if (AutoStartupManager.RemoveFromStartup())
                {
                    Log("已从开机自启动中移除");
                }
                else
                {
                    Log("从开机自启动中移除失败");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"设置开机自启动失败: {ex.Message}");
            MessageBox.Show($"设置开机自启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _chkAutoStart.Checked = false;
        }
    }

    private void BtnSaveBackgroundConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            // 保存提交模式
            _settings.Background.SubmitMode = _rbManualSubmit?.Checked == true ? "manual" : "auto";
            _settings.Background.Countdown = (int)(_numCountdown?.Value ?? 5);

            // 保存开机自启动
            _settings.Background.AutoStart = _chkAutoStart?.Checked ?? false;

            // 保存功能开关
            _settings.Background.EnableFloatingBall = _chkEnableFloatingBall?.Checked ?? false;
            _settings.Background.EnableService = _chkEnableService?.Checked ?? true;

            ConfigManager.Save(_settings);
            Log("后台配置已保存");
            MessageBox.Show("配置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"保存配置失败: {ex.Message}");
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnResetBackgroundConfig_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("确定要恢复默认配置吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            // 恢复默认值
            if (_rbManualSubmit != null)
                _rbManualSubmit.Checked = true;
            if (_numCountdown != null)
                _numCountdown.Value = 5;
            if (_chkAutoStart != null && _chkAutoStart.Checked)
            {
                _chkAutoStart.Checked = false; // 这会触发 OnAutoStartChanged 移除自启动
            }
            if (_chkEnableFloatingBall != null)
                _chkEnableFloatingBall.Checked = false;
            if (_chkEnableService != null)
                _chkEnableService.Checked = true;

            Log("已恢复默认配置");
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
