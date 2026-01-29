namespace BluetoothCardReaderTool.Models;

/// <summary>
/// 应用配置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 蓝牙配置
    /// </summary>
    public BluetoothConfig Bluetooth { get; set; } = new();

    /// <summary>
    /// OCR 配置
    /// </summary>
    public OcrConfig Ocr { get; set; } = new();

    /// <summary>
    /// 服务配置
    /// </summary>
    public ServiceConfig Service { get; set; } = new();

    /// <summary>
    /// 后台配置
    /// </summary>
    public BackgroundConfig Background { get; set; } = new();
}

/// <summary>
/// 蓝牙配置
/// </summary>
public class BluetoothConfig
{
    /// <summary>
    /// 上次选择的设备句柄
    /// </summary>
    public string LastDeviceHandle { get; set; } = "";

    /// <summary>
    /// 上次选择的设备名称
    /// </summary>
    public string LastDeviceName { get; set; } = "";

    /// <summary>
    /// HID 设备关键字（用于匹配）
    /// </summary>
    public string HidKeywords { get; set; } = "Bluetooth;HID";

    /// <summary>
    /// 卡号长度
    /// </summary>
    public int CardLength { get; set; } = 10;

    /// <summary>
    /// 是否需要 Enter 结束
    /// </summary>
    public bool RequireEnter { get; set; } = false;
}

/// <summary>
/// OCR 配置
/// </summary>
public class OcrConfig
{
    /// <summary>
    /// OCR 字段列表
    /// </summary>
    public List<OcrField> Fields { get; set; } = new();
}

/// <summary>
/// OCR 字段
/// </summary>
public class OcrField
{
    /// <summary>
    /// 字段名称（显示用）
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 参数名（接口字段名）
    /// </summary>
    public string ParamName { get; set; } = "";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 识别区域
    /// </summary>
    public OcrRegion? Region { get; set; }

    /// <summary>
    /// 默认值（分号分隔多选项）
    /// </summary>
    public string DefaultValue { get; set; } = "";

    /// <summary>
    /// 识别示例
    /// </summary>
    public string Example { get; set; } = "";
}

/// <summary>
/// OCR 识别区域
/// </summary>
public class OcrRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// 服务配置
/// </summary>
public class ServiceConfig
{
    /// <summary>
    /// 当前使用的系统版本
    /// </summary>
    public string Version { get; set; } = "V2.0";

    /// <summary>
    /// V1 系统配置
    /// </summary>
    public SystemConfig V1 { get; set; } = new();

    /// <summary>
    /// V2 系统配置
    /// </summary>
    public SystemConfig V2 { get; set; } = new();

    /// <summary>
    /// 是否启用洗消验证
    /// </summary>
    public bool EnableVerify { get; set; } = true;

    /// <summary>
    /// 是否显示结果弹窗
    /// </summary>
    public bool ShowResultPopup { get; set; } = true;
}

/// <summary>
/// 系统配置
/// </summary>
public class SystemConfig
{
    /// <summary>
    /// 验证接口 URL
    /// </summary>
    public string VerifyUrl { get; set; } = "";

    /// <summary>
    /// 绑定接口 URL
    /// </summary>
    public string BindUrl { get; set; } = "";
}

/// <summary>
/// 后台配置
/// </summary>
public class BackgroundConfig
{
    /// <summary>
    /// 提交模式（manual/auto）
    /// </summary>
    public string SubmitMode { get; set; } = "manual";

    /// <summary>
    /// 自动提交倒计时（秒）
    /// </summary>
    public int Countdown { get; set; } = 5;

    /// <summary>
    /// 开机自启动
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// 启用浮球输入
    /// </summary>
    public bool EnableFloatingBall { get; set; } = false;

    /// <summary>
    /// 启用后台服务
    /// </summary>
    public bool EnableService { get; set; } = true;
}
