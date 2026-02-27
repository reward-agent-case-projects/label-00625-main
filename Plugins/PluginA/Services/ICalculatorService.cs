namespace PluginA;

/// <summary>
/// 计算器服务接口
/// </summary>
public interface ICalculatorService
{
    double Add(double a, double b);
    double Subtract(double a, double b);
    double Multiply(double a, double b);
    double Divide(double a, double b);
    double Power(double baseNum, double exponent);
    double Sqrt(double value);
    IEnumerable<CalculationRecord> GetHistory();
}

/// <summary>
/// 计算记录
/// </summary>
public class CalculationRecord
{
    public string Operation { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public double Result { get; set; }
    public DateTime Timestamp { get; set; }
}
