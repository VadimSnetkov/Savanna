using System.Reflection;
using System.Runtime.Loader;
using Savanna.Core.Behavior;

namespace Savanna.Core.Service;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// Summary:
    /// Creates a separate dependency context for one plugin assembly.
    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// Summary:
    /// Shares the host's Core assembly and resolves other plugin dependencies.
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var contract = typeof(IAnimalBehavior).Assembly;

        if (assemblyName.Name == contract.GetName().Name)
        {
            return contract;
        }

        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    /// Summary:
    /// Resolves native libraries shipped with a plugin.
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }
}
