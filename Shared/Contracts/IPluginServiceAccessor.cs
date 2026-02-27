namespace PluginSystem.Contracts;

/// <summary>
/// 插件服务访问器接口 - 用于控制器动态获取插件服务
/// 这是实现真正热重载的关键：控制器不直接依赖插件服务，而是通过此接口动态获取
/// </summary>
public interface IPluginServiceAccessor
{
    /// <summary>
    /// 获取指定插件的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="pluginName">插件名称</param>
    /// <returns>服务实例，如果不存在则返回 null</returns>
    T? GetService<T>(string pluginName) where T : class;

    /// <summary>
    /// 获取指定插件的必需服务（不存在时抛出异常）
    /// </summary>
    T GetRequiredService<T>(string pluginName) where T : class;

    /// <summary>
    /// 获取服务（自动查找所有插件）
    /// </summary>
    T? GetService<T>() where T : class;

    /// <summary>
    /// 获取必需服务（自动查找所有插件）
    /// </summary>
    T GetRequiredService<T>() where T : class;
}
