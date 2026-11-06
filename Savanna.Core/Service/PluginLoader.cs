using Savanna.Core.Behavior;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class PluginLoader
{
    private const string PluginPattern = "Savanna.Animals.*.dll";

    /// Summary:
    /// Loads public animal behaviours from plugin assemblies.
    public PluginLoadResult Load(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var result = new PluginLoadResult();

        if (!Directory.Exists(directory))
        {
            return result;
        }

        string[] files;

        try
        {
            files = Directory.GetFiles(directory, PluginPattern, SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            result.Warnings.Add($"Cannot read plugin folder: {exception.Message}");
            return result;
        }

        foreach (string file in files)
        {
            try
            {
                string path = Path.GetFullPath(file);
                var context = new PluginLoadContext(path);
                var assembly = context.LoadFromAssemblyPath(path);

                var types = assembly.GetExportedTypes()
                    .Where(type =>
                        type.IsClass && !type.IsAbstract &&
                        !type.ContainsGenericParameters &&
                        typeof(IAnimalBehavior).IsAssignableFrom(type))
                    .ToArray();

                if (types.Length == 0)
                {
                    result.Warnings.Add($"{Path.GetFileName(file)}: no compatible behaviours.");
                }

                foreach (var type in types)
                {
                    try
                    {
                        if (type.GetConstructor(Type.EmptyTypes) is null)
                        {
                            throw new InvalidOperationException(
                                "A public parameterless constructor is required.");
                        }

                        var behavior = (IAnimalBehavior)Activator.CreateInstance(type)!;
                        ArgumentException.ThrowIfNullOrWhiteSpace(behavior.Type);
                        result.Behaviors.Add(behavior);
                    }
                    catch (Exception exception)
                    {
                        result.Warnings.Add(
                            $"{type.Name}: {exception.GetBaseException().Message}");
                    }
                }
            }
            catch (Exception exception)
            {
                result.Warnings.Add(
                    $"{Path.GetFileName(file)}: {exception.GetBaseException().Message}");
            }
        }

        return result;
    }
}
