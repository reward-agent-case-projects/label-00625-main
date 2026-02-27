using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluginSystem.Contracts;

namespace PluginA.Controllers;

/// <summary>
/// 计算器控制器 - 通过 IPluginServiceAccessor 动态获取服务，支持热重载
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CalculatorController : ControllerBase
{
    private readonly IPluginServiceAccessor _serviceAccessor;
    private readonly ILogger<CalculatorController> _logger;

    // 动态获取服务，每次请求都从最新的插件容器获取
    private ICalculatorService CalculatorService => 
        _serviceAccessor.GetRequiredService<ICalculatorService>("PluginA");

    public CalculatorController(IPluginServiceAccessor serviceAccessor, ILogger<CalculatorController> logger)
    {
        _serviceAccessor = serviceAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 加法运算
    /// </summary>
    [HttpPost("add")]
    public ActionResult<CalculationResult> Add([FromBody] TwoOperandRequest request)
    {
        _logger.LogInformation("执行加法: {A} + {B}", request.A, request.B);
        var result = CalculatorService.Add(request.A, request.B);
        return Ok(new CalculationResult { Result = result, Expression = $"{request.A} + {request.B}" });
    }

    /// <summary>
    /// 减法运算
    /// </summary>
    [HttpPost("subtract")]
    public ActionResult<CalculationResult> Subtract([FromBody] TwoOperandRequest request)
    {
        _logger.LogInformation("执行减法: {A} - {B}", request.A, request.B);
        var result = CalculatorService.Subtract(request.A, request.B);
        return Ok(new CalculationResult { Result = result, Expression = $"{request.A} - {request.B}" });
    }

    /// <summary>
    /// 乘法运算
    /// </summary>
    [HttpPost("multiply")]
    public ActionResult<CalculationResult> Multiply([FromBody] TwoOperandRequest request)
    {
        _logger.LogInformation("执行乘法: {A} × {B}", request.A, request.B);
        var result = CalculatorService.Multiply(request.A, request.B);
        return Ok(new CalculationResult { Result = result, Expression = $"{request.A} × {request.B}" });
    }

    /// <summary>
    /// 除法运算
    /// </summary>
    [HttpPost("divide")]
    public ActionResult<CalculationResult> Divide([FromBody] TwoOperandRequest request)
    {
        try
        {
            _logger.LogInformation("执行除法: {A} ÷ {B}", request.A, request.B);
            var result = CalculatorService.Divide(request.A, request.B);
            return Ok(new CalculationResult { Result = result, Expression = $"{request.A} ÷ {request.B}" });
        }
        catch (DivideByZeroException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 幂运算
    /// </summary>
    [HttpPost("power")]
    public ActionResult<CalculationResult> Power([FromBody] TwoOperandRequest request)
    {
        _logger.LogInformation("执行幂运算: {A} ^ {B}", request.A, request.B);
        var result = CalculatorService.Power(request.A, request.B);
        return Ok(new CalculationResult { Result = result, Expression = $"{request.A} ^ {request.B}" });
    }

    /// <summary>
    /// 平方根运算
    /// </summary>
    [HttpPost("sqrt")]
    public ActionResult<CalculationResult> Sqrt([FromBody] SingleOperandRequest request)
    {
        try
        {
            _logger.LogInformation("执行平方根: √{Value}", request.Value);
            var result = CalculatorService.Sqrt(request.Value);
            return Ok(new CalculationResult { Result = result, Expression = $"√{request.Value}" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取计算历史
    /// </summary>
    [HttpGet("history")]
    public ActionResult<IEnumerable<CalculationRecord>> GetHistory()
    {
        var history = CalculatorService.GetHistory();
        return Ok(history);
    }
}

public class TwoOperandRequest
{
    public double A { get; set; }
    public double B { get; set; }
}

public class SingleOperandRequest
{
    public double Value { get; set; }
}

public class CalculationResult
{
    public double Result { get; set; }
    public string Expression { get; set; } = string.Empty;
}
