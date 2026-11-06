using Savanna.Core.Configuration;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class AnimalSpawnService
{
    private readonly AnimalCatalog _catalog;
    private readonly Random _random;

    /// Summary:
    /// Creates the animal spawning service.
    public AnimalSpawnService(AnimalCatalog catalog, Random random)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(random);

        _catalog = catalog;
        _random = random;
    }

    /// Summary:
    /// Adds an animal to a random empty cell, returning false if full.
    public bool TryAddAnimal(SavannaState state, string type)
    {
        ArgumentNullException.ThrowIfNull(state);

        _catalog.GetOptions(type);

        var occupiedPositions = state.Animals
            .Select(animal => animal.Position)
            .ToHashSet();

        var emptyPositions = new List<Position>();

        for (int row = 0; row < state.Height; row++)
        {
            for (int column = 0; column < state.Width; column++)
            {
                var position = new Position(row, column);

                if (!occupiedPositions.Contains(position))
                {
                    emptyPositions.Add(position);
                }
            }
        }

        if (emptyPositions.Count == 0)
        {
            return false;
        }

        var startingPosition =
            emptyPositions[_random.Next(emptyPositions.Count)];

        return TryAddAnimalAt(state, type, startingPosition);
    }

    /// Summary:
    /// Adds an animal at a specific cell if that cell is empty.
    public bool TryAddAnimalAt(
        SavannaState state,
        string type,
        Position position)
    {
        ArgumentNullException.ThrowIfNull(state);

        var animalOptions = _catalog.GetOptions(type);

        if (position.Row >= state.Height || position.Column >= state.Width)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (state.Animals.Any(animal => animal.Position == position))
        {
            return false;
        }

        state.MutableAnimals.Add(new Animal(
            type,
            animalOptions.Speed,
            animalOptions.VisionRange,
            position,
            animalOptions.InitialHealth,
            animalOptions.IsPredator));

        return true;
    }
}
