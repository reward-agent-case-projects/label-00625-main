namespace PluginSystem.Core;

/// <summary>
/// 插件程序集部分 - 用于将插件程序集添加到 MVC 应用程序部分
/// </summary>
public class PluginAssemblyPart : ApplicationPart, IApplicationPartTypeProvider
{
    public Assembly Assembly { get; }

    public override string Name => Assembly.GetName().Name ?? "Unknown";

    public IEnumerable<TypeInfo> Types { get; }

    public PluginAssemblyPart(Assembly assembly)
    {
        Assembly = assembly;
        Types = assembly.DefinedTypes.ToList();
    }
}

/// <summary>
/// 插件控制器特性提供器 - 用于发现插件中的控制器
/// </summary>
public class PluginControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly IEnumerable<Type> _controllerTypes;

    public PluginControllerFeatureProvider(IEnumerable<Type> controllerTypes)
    {
        _controllerTypes = controllerTypes;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var controllerType in _controllerTypes)
        {
            feature.Controllers.Add(controllerType.GetTypeInfo());
        }
    }
}
