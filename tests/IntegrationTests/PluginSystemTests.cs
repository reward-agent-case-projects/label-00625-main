using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// 插件系统集成测试
/// 测试前请确保服务已启动: docker-compose up -d
/// </summary>
public class PluginSystemTests : IClassFixture<HttpClientFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PluginSystemTests(HttpClientFixture fixture)
    {
        _client = fixture.Client;
    }

    #region 健康检查测试

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    #endregion

    #region 插件管理 API 测试

    [Fact]
    public async Task GetPlugins_ShouldReturnLoadedPlugins()
    {
        // Act
        var response = await _client.GetAsync("/api/plugins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plugins = await response.Content.ReadFromJsonAsync<List<PluginDto>>(_jsonOptions);
        
        plugins.Should().NotBeNull();
        plugins.Should().HaveCount(2);
        plugins.Should().Contain(p => p.Name == "PluginA");
        plugins.Should().Contain(p => p.Name == "PluginB");
    }

    [Fact]
    public async Task GetPluginStatus_ShouldReturnSystemStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/plugins/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<PluginSystemStatus>(_jsonOptions);
        
        status.Should().NotBeNull();
        status!.LoadedPluginCount.Should().Be(2);
        status.HotReloadEnabled.Should().BeTrue();
        status.Plugins.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPluginDetail_ShouldReturnPluginInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/plugins/PluginA");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<PluginDetailDto>(_jsonOptions);
        
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("PluginA");
        detail.Version.Should().Be("1.0.0");
        detail.Controllers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPluginDetail_NotFound_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/plugins/NonExistentPlugin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region 计算器插件测试

    [Theory]
    [InlineData(10, 5, 15)]
    [InlineData(100, 200, 300)]
    [InlineData(-5, 10, 5)]
    [InlineData(0, 0, 0)]
    public async Task Calculator_Add_ShouldReturnCorrectResult(double a, double b, double expected)
    {
        // Arrange
        var request = new { a, b };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResult>(_jsonOptions);
        
        result.Should().NotBeNull();
        result!.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 5, 5)]
    [InlineData(100, 30, 70)]
    [InlineData(5, 10, -5)]
    public async Task Calculator_Subtract_ShouldReturnCorrectResult(double a, double b, double expected)
    {
        // Arrange
        var request = new { a, b };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/subtract", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResult>(_jsonOptions);
        
        result!.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 5, 50)]
    [InlineData(7, 8, 56)]
    [InlineData(-3, 4, -12)]
    public async Task Calculator_Multiply_ShouldReturnCorrectResult(double a, double b, double expected)
    {
        // Arrange
        var request = new { a, b };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/multiply", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResult>(_jsonOptions);
        
        result!.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData(20, 4, 5)]
    [InlineData(100, 10, 10)]
    [InlineData(15, 3, 5)]
    public async Task Calculator_Divide_ShouldReturnCorrectResult(double a, double b, double expected)
    {
        // Arrange
        var request = new { a, b };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/divide", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResult>(_jsonOptions);
        
        result!.Result.Should().Be(expected);
    }

    [Fact]
    public async Task Calculator_DivideByZero_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { a = 10, b = 0 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/divide", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(2, 3, 8)]
    [InlineData(5, 2, 25)]
    [InlineData(10, 0, 1)]
    public async Task Calculator_Power_ShouldReturnCorrectResult(double baseNum, double exponent, double expected)
    {
        // Arrange
        var request = new { a = baseNum, b = exponent };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/power", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResult>(_jsonOptions);
        
        result!.Result.Should().Be(expected);
    }

    [Theory]
    [InlineData(16, 4)]
    [InlineData(25, 5)]
    [InlineData(100, 10)]
    public async Task Calculator_Sqrt_ShouldReturnCorrectResult(double value, double expected)
    {
        // Arrange
        var request = new { value };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/sqrt", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CalculationResult>(_jsonOptions);
        
        result!.Result.Should().Be(expected);
    }

    [Fact]
    public async Task Calculator_SqrtNegative_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { value = -1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/calculator/sqrt", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Calculator_History_ShouldReturnCalculationHistory()
    {
        // Arrange - 先执行一些计算
        await _client.PostAsJsonAsync("/api/calculator/add", new { a = 1, b = 2 });

        // Act
        var response = await _client.GetAsync("/api/calculator/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<CalculationRecord>>(_jsonOptions);
        
        history.Should().NotBeNull();
    }

    #endregion

    #region 问候插件测试

    [Fact]
    public async Task Greeting_DefaultLanguage_ShouldReturnEnglish()
    {
        // Act
        var response = await _client.GetAsync("/api/greeting/World");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GreetingResponse>(_jsonOptions);
        
        result.Should().NotBeNull();
        result!.Name.Should().Be("World");
        result.Greeting.Should().Be("Hello, World!");
    }

    [Theory]
    [InlineData("zh", "你好，Test！")]
    [InlineData("ja", "こんにちは、Testさん！")]
    [InlineData("ko", "안녕하세요, Test님!")]
    [InlineData("es", "¡Hola, Test!")]
    [InlineData("fr", "Bonjour, Test!")]
    [InlineData("de", "Hallo, Test!")]
    public async Task Greeting_DifferentLanguages_ShouldReturnCorrectGreeting(string language, string expected)
    {
        // Act
        var response = await _client.GetAsync($"/api/greeting/Test?language={language}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GreetingResponse>(_jsonOptions);
        
        result!.Greeting.Should().Be(expected);
    }

    [Fact]
    public async Task Greeting_GetLanguages_ShouldReturnSupportedLanguages()
    {
        // Act
        var response = await _client.GetAsync("/api/greeting/languages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var languages = await response.Content.ReadFromJsonAsync<List<LanguageInfo>>(_jsonOptions);
        
        languages.Should().NotBeNull();
        languages.Should().Contain(l => l.Code == "en");
        languages.Should().Contain(l => l.Code == "zh");
        languages.Should().Contain(l => l.Code == "ja");
    }

    [Fact]
    public async Task Greeting_AddCustomGreeting_ShouldSucceed()
    {
        // Arrange
        var request = new { language = "test", template = "Hi, {name}!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/greeting", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}

#region DTOs

public class PluginDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public int ControllerCount { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class PluginDetailDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ControllerDto> Controllers { get; set; } = new();
}

public class ControllerDto
{
    public string Name { get; set; } = string.Empty;
    public string[] Actions { get; set; } = Array.Empty<string>();
}

public class PluginSystemStatus
{
    public string PluginPath { get; set; } = string.Empty;
    public bool HotReloadEnabled { get; set; }
    public int HotReloadDelay { get; set; }
    public int LoadedPluginCount { get; set; }
    public List<PluginStatusDto> Plugins { get; set; } = new();
}

public class PluginStatusDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
}

public class CalculationResult
{
    public double Result { get; set; }
    public string Expression { get; set; } = string.Empty;
}

public class CalculationRecord
{
    public string Operation { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public double Result { get; set; }
    public DateTime Timestamp { get; set; }
}

public class GreetingResponse
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Greeting { get; set; } = string.Empty;
}

public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
}

#endregion
