using Savanna.Core.Models;
using Savanna.Core.Service;

namespace Savanna.Core.Behavior;

public sealed class LionBehavior : IAnimalBehavior
{
    private readonly MovementService _movementService;

    public AnimalType Type => AnimalType.Lion;

    /// Summary:
    /// Creates the lion behaviour with shared movement calculations.
    public LionBehavior(MovementService movementService)
    {
        ArgumentNullException.ThrowIfNull(movementService);

        _movementService = movementService;
    }

    /// Summary:
    /// Moves towards the closest visible antelope or wanders.
    public Position GetNextPosition(
        Animal animal,
        IReadOnlyList<Animal> animals,
        IReadOnlyList<Position> allowedPositions)
    {
        var antelope = _movementService.FindNearestVisibleAnimal(
            animal,
            animals,
            AnimalType.Antelope);

        if (antelope is null)
        {
            return _movementService.ChooseWanderingPosition(
                animal,
                allowedPositions);
        }

        int shortestDistance = allowedPositions.Min(position =>
            MovementService.GetDistance(position, antelope.Position));

        var closestPositions = allowedPositions
            .Where(position =>
                MovementService.GetDistance(position, antelope.Position)
                    == shortestDistance)
            .ToArray();

        return _movementService.ChooseRandomPosition(closestPositions);
    }
}