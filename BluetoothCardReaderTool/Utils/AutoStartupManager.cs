using Microsoft.Win32;
using System.Reflection;

namespace BluetoothCardReaderTool.Utils;

/// <summary>
/// 开机自启动管理器
/// </summary>
public static class AutoStartupManager
{
    private const string AppName = "蓝牙刷卡器工具";
    private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// 添加到开机自启动
    /// </summary>
    /// <returns>是否成功</returns>
    public static bool AddToStartup()
    {
        try
        {
            string exePath = Assembly.GetExecutingAssembly().Location;

            // 如果是 .dll，尝试获取 .exe 路径
            if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                exePath = exePath.Replace(".dll", ".exe");
            }

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null)
            {
                return false;
            }

            key.SetValue(AppName, $"\"{exePath}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从开机自启动中移除
    /// </summary>
    /// <returns>是否成功</returns>
    public static bool RemoveFromStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null)
            {
                return false;
            }

            // 检查是否存在
            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查是否已添加到开机自启动
    /// </summary>
    /// <returns>是否已添加</returns>
    public static bool IsInStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            if (key == null)
            {
                return false;
            }

            var value = key.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}
