namespace IntegrationTests;

/// <summary>
/// HTTP 客户端 Fixture - 用于集成测试
/// </summary>
public class HttpClientFixture : IDisposable
{
    public HttpClient Client { get; }

    public HttpClientFixture()
    {
        // 从环境变量读取基础 URL，默认为本地 Docker 服务地址
        var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:8081";
        
        Client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}
