using PluginSystem.Core;

var builder = WebApplication.CreateBuilder(args);

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ============================================================
// 1. 插件系统初始化
// ============================================================

// 1.1 加载插件配置
var pluginOptions = new PluginOptions();
builder.Configuration.GetSection("Plugins").Bind(pluginOptions);

// 1.2 创建日志工厂
var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// 1.3 创建插件管理器
var pluginManager = new PluginManager(loggerFactory.CreateLogger<PluginManager>());

// 1.4 加载所有插件（包含配置文件加载）
var pluginsPath = Path.Combine(AppContext.BaseDirectory, pluginOptions.PluginPath);
var plugins = pluginManager.LoadPlugins(pluginsPath);

// ============================================================
// 2. 插件服务容器（支持热重载的独立 DI 容器）
// ============================================================

// 2.1 创建插件服务提供器
var pluginServiceProvider = new PluginServiceProvider(
    loggerFactory.CreateLogger<PluginServiceProvider>());

// 2.2 注册插件到独立容器（调用 ConfigureServices 进行依赖注入）
foreach (var plugin in plugins)
{
    // 使用插件自带的配置（已在 LoadPlugin 时通过 ConfigurationBuilder 加载）
    pluginServiceProvider.RegisterPlugin(plugin, plugin.Configuration);
}

// ============================================================
// 3. 主容器服务注册
// ============================================================

// 3.1 注册核心服务
builder.Services.AddSingleton(pluginOptions);
builder.Services.AddSingleton(pluginManager);
builder.Services.AddSingleton(pluginServiceProvider);

// 3.2 注册插件服务访问器（控制器通过它动态获取插件服务）
builder.Services.AddPluginServiceAccessor(pluginServiceProvider);

// 3.3 添加插件 MVC 支持（使用 AddApplicationPart 注册插件控制器）
builder.Services.AddPluginMvc(plugins);

// 3.4 注册插件热重载后台服务
builder.Services.AddHostedService<PluginReloadService>();

// 3.5 添加 Swagger 支持
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Plugin System API", Version = "v1" });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});

// 3.6 添加健康检查
builder.Services.AddHealthChecks();

var app = builder.Build();

// ============================================================
// 4. HTTP 管道配置
// ============================================================

// 4.1 Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Plugin System API v1");
    c.RoutePrefix = "swagger";
});

// 4.2 异常处理
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Internal Server Error\"}");
    });
});

// 4.3 路由和授权
app.UseRouting();
app.UseAuthorization();

// 4.4 使用插件中间件
app.UsePlugins(plugins);

// 4.5 映射控制器
app.MapControllers();

// 4.6 健康检查端点
app.MapHealthChecks("/health");

// ============================================================
// 5. 插件模块初始化
// ============================================================
await app.InitializePluginsAsync(plugins);

// ============================================================
// 6. 启动应用
// ============================================================
app.Run();
