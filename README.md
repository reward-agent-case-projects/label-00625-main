# C# 10 插件系统 Demo

一个基于 .NET 6 的完整插件化系统实现，支持动态加载、依赖注入、WebAPI 自动发现、热重载等功能。

## How to Run

### Docker Compose（推荐）

```bash
# 构建并启动
docker-compose up --build -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

### 本地开发

```bash
# 构建插件并复制到输出目录
dotnet build Plugins/PluginA/PluginA.csproj -o Host/bin/Debug/net6.0/Plugins/PluginA
dotnet build Plugins/PluginB/PluginB.csproj -o Host/bin/Debug/net6.0/Plugins/PluginB

# 运行主程序
dotnet run --project Host/Host.csproj
```

### 运行测试

```bash
# Shell 脚本测试
./scripts/test-api.sh

# xUnit 集成测试
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

## Services

| 服务 | 端口 | 描述 |
|------|------|------|
| Host | 8081 | 插件系统 API |

### 访问地址

| 地址 | 描述 |
|------|------|
| http://localhost:8081/swagger | Swagger API 文档 |
| http://localhost:8081/health | 健康检查 |
| http://localhost:8081/api/plugins | 插件管理 API |
| http://localhost:8081/api/calculator | 计算器插件 API |
| http://localhost:8081/api/greeting | 问候插件 API |

## 测试账号

本项目为技术 Demo，无需登录认证。

## 题目内容

实现一个 C# 10 插件系统 Demo，包含以下特性：

- **核心框架**: 使用 .NET 6 的原生 `AssemblyLoadContext`
- **依赖管理**: 使用原生 DI 的模块化注册
- **API 发现**: `AddApplicationPart()` + 命名约定
- **配置管理**: 每个插件独立的 `appsettings.json`

### 核心功能

| 功能 | 说明 |
|------|------|
| 插件隔离加载 | 使用 `AssemblyLoadContext` 实现插件隔离 |
| 依赖注入集成 | 插件可注册服务到主容器 |
| WebAPI 自动发现 | 自动发现并注册插件中的控制器 |
| 独立配置 | 每个插件拥有独立的配置文件 |
| 热重载 | 运行时重载插件服务，无需重启应用 |
| 文件监控 | 自动监控插件目录变化 |
| 插件管理 API | 查看/卸载/重载插件 |

### 热重载 API

```bash
# 重载单个插件服务
curl -X POST http://localhost:8081/api/plugins/PluginA/reload

# 重载所有插件服务
curl -X POST http://localhost:8081/api/plugins/reload

# 卸载单个插件
curl -X POST http://localhost:8081/api/plugins/PluginA/unload

# 卸载所有插件
curl -X POST http://localhost:8081/api/plugins/unload
```

### 项目结构

```
MyPluginSystem/
├── Plugins/                          # 插件目录
│   ├── PluginA/                      # 计算器插件
│   └── PluginB/                      # 问候插件
├── Host/                             # 主程序
├── Shared/                           # 共享接口
├── tests/                            # 测试项目
│   └── IntegrationTests/
├── scripts/                          # 脚本
│   └── test-api.sh
├── Dockerfile
├── docker-compose.yml
├── .gitignore
└── README.md
```

### API 示例

```bash
# 查看已加载插件
curl http://localhost:8081/api/plugins

# 计算器 - 加法
curl -X POST http://localhost:8081/api/calculator/add \
  -H "Content-Type: application/json" \
  -d '{"a": 10, "b": 5}'

# 问候 - 中文
curl "http://localhost:8081/api/greeting/张三?language=zh"
```

### 技术栈

- .NET 6 / C# 10
- ASP.NET Core WebAPI
- AssemblyLoadContext（插件隔离）
- Swagger/OpenAPI
- Docker（支持 ARM64 和 AMD64）
- xUnit + FluentAssertions（测试）

---

项目标签: `label-00315`
