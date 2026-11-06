using Savanna.Core.Models;

namespace Savanna.Core.Behavior;

public interface IAnimalBehavior
{
    string Type { get; }

    /// Summary:
    /// Chooses the animal's next position from the allowed positions.
    Position GetNextPosition(
        Animal animal,
        IReadOnlyList<Animal> animals,
        IReadOnlyList<Position> allowedPositions);
}
