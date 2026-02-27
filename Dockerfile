# 多阶段构建 - 支持 ARM64 和 AMD64
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG TARGETARCH
WORKDIR /src

# 设置运行时标识符
RUN if [ "$TARGETARCH" = "arm64" ]; then echo "linux-arm64" > /tmp/rid; else echo "linux-x64" > /tmp/rid; fi

# 复制项目文件
COPY Shared/Shared.csproj Shared/
COPY Host/Host.csproj Host/
COPY Plugins/PluginA/PluginA.csproj Plugins/PluginA/
COPY Plugins/PluginB/PluginB.csproj Plugins/PluginB/

# 还原所有项目依赖
RUN dotnet restore Shared/Shared.csproj
RUN dotnet restore Plugins/PluginA/PluginA.csproj
RUN dotnet restore Plugins/PluginB/PluginB.csproj
RUN dotnet restore Host/Host.csproj -r $(cat /tmp/rid)

# 复制所有源代码
COPY . .

# 构建共享库
WORKDIR /src/Shared
RUN dotnet build -c Release --no-restore

# 构建插件A
WORKDIR /src/Plugins/PluginA
RUN dotnet publish -c Release --no-restore -o /app/publish/Plugins/PluginA

# 构建插件B
WORKDIR /src/Plugins/PluginB
RUN dotnet publish -c Release --no-restore -o /app/publish/Plugins/PluginB

# 构建并发布主程序
WORKDIR /src/Host
RUN dotnet publish -c Release -r $(cat /tmp/rid) --self-contained false --no-restore -o /app/publish

# 运行时镜像 - 支持 ARM64 和 AMD64
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# 安装 curl 用于健康检查
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 复制发布文件
COPY --from=build /app/publish .

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# 暴露端口
EXPOSE 8080

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "Host.dll"]
