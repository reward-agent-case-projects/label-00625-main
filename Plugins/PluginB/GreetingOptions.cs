namespace PluginB;

/// <summary>
/// 问候配置选项
/// </summary>
public class GreetingOptions
{
    /// <summary>
    /// 默认语言
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
