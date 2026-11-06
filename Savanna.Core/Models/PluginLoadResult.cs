using Savanna.Core.Behavior;

namespace Savanna.Core.Models;

public sealed class PluginLoadResult
{
    public List<IAnimalBehavior> Behaviors { get; } = [];
    public List<string> Warnings { get; } = [];
}
