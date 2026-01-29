using System.Text.Json;
using BluetoothCardReaderTool.Models;

namespace BluetoothCardReaderTool.Utils;

/// <summary>
/// 配置管理器
/// </summary>
public class ConfigManager
{
    private static readonly string ConfigFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "app_settings.json"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 加载配置
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置失败：{ex.Message}");
        }

        return new AppSettings();
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public static void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置失败：{ex.Message}");
            throw;
        }
    }
}
