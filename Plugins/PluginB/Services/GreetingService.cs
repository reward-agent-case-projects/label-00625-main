using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PluginB;

/// <summary>
/// 问候服务实现
/// </summary>
public class GreetingService : IGreetingService
{
    private readonly ILogger<GreetingService> _logger;
    private readonly GreetingOptions _options;
    private readonly Dictionary<string, LanguageInfo> _languages;

    public GreetingService(ILogger<GreetingService> logger, IOptions<GreetingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        // 初始化默认语言
        _languages = new Dictionary<string, LanguageInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new() { Code = "en", Name = "English", Template = "Hello, {name}!" },
            ["zh"] = new() { Code = "zh", Name = "中文", Template = "你好，{name}！" },
            ["ja"] = new() { Code = "ja", Name = "日本語", Template = "こんにちは、{name}さん！" },
            ["ko"] = new() { Code = "ko", Name = "한국어", Template = "안녕하세요, {name}님!" },
            ["es"] = new() { Code = "es", Name = "Español", Template = "¡Hola, {name}!" },
            ["fr"] = new() { Code = "fr", Name = "Français", Template = "Bonjour, {name}!" },
            ["de"] = new() { Code = "de", Name = "Deutsch", Template = "Hallo, {name}!" },
            ["ru"] = new() { Code = "ru", Name = "Русский", Template = "Привет, {name}!" }
        };
    }

    public string Greet(string name, string? language = null)
    {
        var lang = language ?? _options.DefaultLanguage;
        
        if (!_languages.TryGetValue(lang, out var langInfo))
        {
            _logger.LogWarning("不支持的语言: {Language}，使用默认语言", lang);
            langInfo = _languages[_options.DefaultLanguage];
        }

        var greeting = langInfo.Template.Replace("{name}", name);
        _logger.LogDebug("生成问候语: {Greeting} (语言: {Language})", greeting, lang);
        
        return greeting;
    }

    public IEnumerable<LanguageInfo> GetSupportedLanguages()
    {
        return _languages.Values.ToList();
    }

    public void AddCustomGreeting(string language, string template)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("语言代码不能为空", nameof(language));
        }

        if (string.IsNullOrWhiteSpace(template) || !template.Contains("{name}"))
        {
            throw new ArgumentException("模板必须包含 {name} 占位符", nameof(template));
        }

        _languages[language] = new LanguageInfo
        {
            Code = language,
            Name = language,
            Template = template
        };

        _logger.LogInformation("添加自定义问候语: {Language} -> {Template}", language, template);
    }
}
