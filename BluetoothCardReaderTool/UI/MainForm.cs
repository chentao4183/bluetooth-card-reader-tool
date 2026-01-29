using System.Drawing;
using System.Windows.Forms;
using BluetoothCardReaderTool.Models;
using BluetoothCardReaderTool.Utils;

namespace BluetoothCardReaderTool.UI;

public partial class MainForm : Form
{
    private TabControl? _tabControl;
    private TextBox? _txtLog;
    private NotifyIcon? _notifyIcon;
    private AppSettings _settings;

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

        var label = new Label
        {
            Text = "蓝牙配置页面（待实现）",
            Location = new Point(20, 20),
            Size = new Size(900, 30),
            Font = new Font("微软雅黑", 12)
        };
        tab.Controls.Add(label);

        return tab;
    }

    private TabPage CreateOcrTab()
    {
        var tab = new TabPage("OCR配置");

        var label = new Label
        {
            Text = "OCR 配置页面（待实现）",
            Location = new Point(20, 20),
            Size = new Size(900, 30),
            Font = new Font("微软雅黑", 12)
        };
        tab.Controls.Add(label);

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
            _notifyIcon?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
