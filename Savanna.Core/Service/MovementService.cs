using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class MovementService
{
    private readonly Random _random;

    /// Summary:
    /// Creates the movement helper with a random number generator.
    public MovementService(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        _random = random;
    }

    /// Summary:
    /// Calculates distance when diagonal movement is allowed.
    public static int GetDistance(Position first, Position second)
    {
        return Math.Max(
            Math.Abs(first.Row - second.Row),
            Math.Abs(first.Column - second.Column));
    }

    /// Summary:
    /// Finds the closest visible animal of the requested type.
    public Animal? FindNearestVisibleAnimal(
        Animal observer,
        IReadOnlyList<Animal> animals,
        AnimalType targetType)
    {
        return animals
            .Where(animal =>
                animal != observer &&
                animal.Type == targetType &&
                GetDistance(observer.Position, animal.Position)
                    <= observer.VisionRange)
            .OrderBy(animal =>
                GetDistance(observer.Position, animal.Position))
            .FirstOrDefault();
    }

    /// Summary:
    /// Chooses one of the supplied positions randomly.
    public Position ChooseRandomPosition(IReadOnlyList<Position> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        if (positions.Count == 0)
        {
            throw new ArgumentException(
                "At least one position is required.",
                nameof(positions));
        }

        return positions[_random.Next(positions.Count)];
    }

    /// Summary:
    /// Chooses a random neighbouring cell or stays if movement is blocked.
    public Position ChooseWanderingPosition(
        Animal animal,
        IReadOnlyList<Position> positions)
    {
        var movementPositions = positions
            .Where(position => position != animal.Position)
            .ToArray();

        return movementPositions.Length == 0
            ? animal.Position
            : ChooseRandomPosition(movementPositions);
    }
}