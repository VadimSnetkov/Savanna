using Savanna.Core.Models;
using Savanna.Core.Service;

namespace Savanna.Core.Behavior;

public sealed class AntelopeBehavior : IAnimalBehavior
{
    private readonly MovementService _movementService;

    public AnimalType Type => AnimalType.Antelope;

    /// Summary:
    /// Creates the antelope behaviour with shared movement calculations.
    public AntelopeBehavior(MovementService movementService)
    {
        ArgumentNullException.ThrowIfNull(movementService);

        _movementService = movementService;
    }

    /// Summary:
    /// Moves away from the closest visible lion or wanders.
    public Position GetNextPosition(
        Animal animal,
        IReadOnlyList<Animal> animals,
        IReadOnlyList<Position> allowedPositions)
    {
        var lion = _movementService.FindNearestVisibleAnimal(
            animal,
            animals,
            AnimalType.Lion);

        if (lion is null)
        {
            return _movementService.ChooseWanderingPosition(
                animal,
                allowedPositions);
        }

        int greatestDistance = allowedPositions.Max(position =>
            MovementService.GetDistance(position, lion.Position));

        var safestPositions = allowedPositions
            .Where(position =>
                MovementService.GetDistance(position, lion.Position)
                    == greatestDistance)
            .ToArray();

        return _movementService.ChooseRandomPosition(safestPositions);
    }
}