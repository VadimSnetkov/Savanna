using Savanna.Core.Behavior;
using Savanna.Core.Models;

namespace Savanna.Tests.Helpers;

public sealed class TestAnimalBehavior : IAnimalBehavior
{
    public AnimalType Type { get; }

    public Func<Animal, IReadOnlyList<Position>, Position> ChoosePosition { get; set; }
        = (animal, _) => animal.Position;

    /// Summary:
    /// Creates a controllable behaviour for deterministic tests.
    public TestAnimalBehavior(AnimalType type)
    {
        Type = type;
    }

    /// Summary:
    /// Returns the position selected by the test.
    public Position GetNextPosition(
        Animal animal,
        IReadOnlyList<Animal> animals,
        IReadOnlyList<Position> allowedPositions)
    {
        return ChoosePosition(animal, allowedPositions);
    }
}