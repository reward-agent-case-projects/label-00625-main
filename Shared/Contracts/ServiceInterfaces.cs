namespace PluginSystem.Contracts;

/// <summary>
/// 作用域服务标记接口 - 实现此接口的服务将以 Scoped 生命周期注册
/// </summary>
public interface IScopedService { }

/// <summary>
/// 瞬时服务标记接口 - 实现此接口的服务将以 Transient 生命周期注册
/// </summary>
public interface ITransientService { }

/// <summary>
/// 单例服务标记接口 - 实现此接口的服务将以 Singleton 生命周期注册
/// </summary>
public interface ISingletonService { }
