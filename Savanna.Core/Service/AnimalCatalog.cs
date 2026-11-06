using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class AnimalCatalog
{
    private readonly Dictionary<string, IAnimalBehavior> _behaviors = [];
    private readonly Dictionary<string, AnimalOptions> _animals = [];

    public IReadOnlyDictionary<string, AnimalOptions> Animals { get; }
    public List<string> Warnings { get; } = [];
    public string? LastBehaviorError { get; private set; }

    /// Summary:
    /// Registers behaviours that have settings and unique species and keys.
    public AnimalCatalog(
        SimulationOptions options,
        IEnumerable<IAnimalBehavior> behaviors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(behaviors);

        var settings = new Dictionary<string, AnimalOptions>(options.AdditionalAnimals)
        {
            [AnimalType.Antelope] = options.Antelope,
            [AnimalType.Lion] = options.Lion
        };

        foreach (var behavior in behaviors)
        {
            string type = behavior.Type;

            if (string.IsNullOrWhiteSpace(type) ||
                !settings.TryGetValue(type, out var animalOptions))
            {
                Warnings.Add($"Skipped '{type}': no matching animal settings.");
                continue;
            }

            if (_behaviors.ContainsKey(type) ||
                _animals.Values.Any(value =>
                    char.ToUpperInvariant(value.SpawnKey) ==
                    char.ToUpperInvariant(animalOptions.SpawnKey)))
            {
                Warnings.Add($"Skipped '{type}': duplicate species or spawn key.");
                continue;
            }

            _behaviors.Add(type, behavior);
            _animals.Add(type, animalOptions);
        }

        foreach (string type in settings.Keys.Except(_animals.Keys))
        {
            Warnings.Add($"'{type}' is unavailable: no registered behaviour.");
        }

        Animals = new System.Collections.ObjectModel.ReadOnlyDictionary<string, AnimalOptions>(
            _animals);
    }

    /// Summary:
    /// Returns settings only for a registered animal species.
    public AnimalOptions GetOptions(string type)
    {
        return _animals.TryGetValue(type, out var options)
            ? options
            : throw new ArgumentException($"Unknown animal: {type}.", nameof(type));
    }

    /// Summary:
    /// Calls a behaviour and rejects exceptions or illegal positions.
    public Position GetNextPosition(
        Animal animal,
        IReadOnlyList<Animal> animals,
        IReadOnlyList<Position> allowedPositions)
    {
        try
        {
            var position = _behaviors[animal.Type]
                .GetNextPosition(animal, animals, allowedPositions);

            if (!allowedPositions.Contains(position))
            {
                throw new InvalidOperationException("Selected a forbidden position.");
            }

            return position;
        }
        catch (Exception exception)
        {
            LastBehaviorError =
                $"{animal.Type}: {exception.GetBaseException().Message}";
            return animal.Position;
        }
    }
}
