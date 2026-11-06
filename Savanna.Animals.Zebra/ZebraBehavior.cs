using Savanna.Core.Behavior;
using Savanna.Core.Models;
using Savanna.Core.Service;

namespace Savanna.Animals.Zebra;

public sealed class ZebraBehavior : IAnimalBehavior
{
    private readonly MovementService _movement;

    public string Type => "Zebra";

    /// Summary:
    /// Creates a zebra behaviour that the plugin loader can instantiate.
    public ZebraBehavior()
    {
        _movement = new MovementService(Random.Shared);
    }

    /// Summary:
    /// Flees the nearest visible predator or wanders.
    public Position GetNextPosition(
        Animal animal,
        IReadOnlyList<Animal> animals,
        IReadOnlyList<Position> allowedPositions)
    {
        var predator = _movement.FindNearestVisibleAnimal(
            animal, animals, other => other.IsPredator);

        if (predator is null)
        {
            return _movement.ChooseWanderingPosition(animal, allowedPositions);
        }

        int safestDistance = allowedPositions.Max(position =>
            MovementService.GetDistance(position, predator.Position));

        return _movement.ChooseRandomPosition(allowedPositions
            .Where(position =>
                MovementService.GetDistance(position, predator.Position) == safestDistance)
            .ToArray());
    }
}
