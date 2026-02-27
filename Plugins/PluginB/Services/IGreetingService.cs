namespace PluginB;

/// <summary>
/// 问候服务接口
/// </summary>
public interface IGreetingService
{
    string Greet(string name, string? language = null);
    IEnumerable<LanguageInfo> GetSupportedLanguages();
    void AddCustomGreeting(string language, string template);
}

/// <summary>
/// 语言信息
/// </summary>
public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
}
