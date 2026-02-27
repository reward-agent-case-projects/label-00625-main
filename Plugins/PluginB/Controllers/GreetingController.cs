using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluginSystem.Contracts;

namespace PluginB.Controllers;

/// <summary>
/// 问候控制器 - 通过 IPluginServiceAccessor 动态获取服务，支持热重载
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GreetingController : ControllerBase
{
    private readonly IPluginServiceAccessor _serviceAccessor;
    private readonly ILogger<GreetingController> _logger;

    // 动态获取服务，每次请求都从最新的插件容器获取
    private IGreetingService GreetingService => 
        _serviceAccessor.GetRequiredService<IGreetingService>("PluginB");

    public GreetingController(IPluginServiceAccessor serviceAccessor, ILogger<GreetingController> logger)
    {
        _serviceAccessor = serviceAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 获取问候语
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="language">语言代码（可选）</param>
    [HttpGet("{name}")]
    public ActionResult<GreetingResponse> Greet(string name, [FromQuery] string? language = null)
    {
        _logger.LogInformation("生成问候语: Name={Name}, Language={Language}", name, language ?? "default");
        
        var greeting = GreetingService.Greet(name, language);
        return Ok(new GreetingResponse
        {
            Name = name,
            Language = language ?? "default",
            Greeting = greeting
        });
    }

    /// <summary>
    /// 获取支持的语言列表
    /// </summary>
    [HttpGet("languages")]
    public ActionResult<IEnumerable<LanguageInfo>> GetLanguages()
    {
        var languages = GreetingService.GetSupportedLanguages();
        return Ok(languages);
    }

    /// <summary>
    /// 添加自定义问候语
    /// </summary>
    [HttpPost]
    public ActionResult AddCustomGreeting([FromBody] CustomGreetingRequest request)
    {
        try
        {
            _logger.LogInformation("添加自定义问候语: {Language}", request.Language);
            GreetingService.AddCustomGreeting(request.Language, request.Template);
            return Ok(new { message = "自定义问候语添加成功" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class GreetingResponse
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Greeting { get; set; } = string.Empty;
}

public class CustomGreetingRequest
{
    public string Language { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
}
