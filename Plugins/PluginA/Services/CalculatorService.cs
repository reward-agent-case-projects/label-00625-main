using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PluginA;

/// <summary>
/// 计算器服务实现
/// </summary>
public class CalculatorService : ICalculatorService
{
    private readonly ILogger<CalculatorService> _logger;
    private readonly CalculatorOptions _options;
    private readonly List<CalculationRecord> _history = new();

    public CalculatorService(ILogger<CalculatorService> logger, IOptions<CalculatorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public double Add(double a, double b)
    {
        var result = a + b;
        RecordCalculation("Add", $"{a} + {b}", result);
        return result;
    }

    public double Subtract(double a, double b)
    {
        var result = a - b;
        RecordCalculation("Subtract", $"{a} - {b}", result);
        return result;
    }

    public double Multiply(double a, double b)
    {
        var result = a * b;
        RecordCalculation("Multiply", $"{a} × {b}", result);
        return result;
    }

    public double Divide(double a, double b)
    {
        if (b == 0)
        {
            _logger.LogWarning("除数不能为零: {A} / {B}", a, b);
            throw new DivideByZeroException("除数不能为零");
        }

        var result = a / b;
        RecordCalculation("Divide", $"{a} ÷ {b}", result);
        return result;
    }

    public double Power(double baseNum, double exponent)
    {
        var result = Math.Pow(baseNum, exponent);
        RecordCalculation("Power", $"{baseNum} ^ {exponent}", result);
        return result;
    }

    public double Sqrt(double value)
    {
        if (value < 0)
        {
            _logger.LogWarning("不能对负数开平方根: {Value}", value);
            throw new ArgumentException("不能对负数开平方根", nameof(value));
        }

        var result = Math.Sqrt(value);
        RecordCalculation("Sqrt", $"√{value}", result);
        return result;
    }

    public IEnumerable<CalculationRecord> GetHistory()
    {
        return _history.TakeLast(_options.MaxHistoryCount).ToList();
    }

    private void RecordCalculation(string operation, string expression, double result)
    {
        _logger.LogDebug("计算: {Expression} = {Result}", expression, result);

        if (_options.EnableHistory)
        {
            _history.Add(new CalculationRecord
            {
                Operation = operation,
                Expression = expression,
                Result = result,
                Timestamp = DateTime.UtcNow
            });

            // 限制历史记录数量
            while (_history.Count > _options.MaxHistoryCount)
            {
                _history.RemoveAt(0);
            }
        }
    }
}
