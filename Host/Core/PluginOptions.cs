namespace PluginSystem.Core;

/// <summary>
/// 插件系统配置选项
/// </summary>
public class PluginOptions
{
    /// <summary>
    /// 插件目录路径
    /// </summary>
    public string PluginPath { get; set; } = "Plugins";

    /// <summary>
    /// 是否启用热重载
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// 热重载延迟（毫秒）
    /// </summary>
    public int HotReloadDelay { get; set; } = 1000;
}
