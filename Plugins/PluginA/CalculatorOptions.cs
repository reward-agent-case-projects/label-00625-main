namespace PluginA;

/// <summary>
/// 计算器配置选项
/// </summary>
public class CalculatorOptions
{
    /// <summary>
    /// 是否启用历史记录
    /// </summary>
    public bool EnableHistory { get; set; } = true;

    /// <summary>
    /// 最大历史记录数量
    /// </summary>
    public int MaxHistoryCount { get; set; } = 100;

    /// <summary>
    /// 小数精度
    /// </summary>
    public int DecimalPrecision { get; set; } = 10;
}
