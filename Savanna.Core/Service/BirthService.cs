using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class BirthService
{
    private const int RequiredNearbyRounds = 3;
    private const int BirthDistance = 1;

    private readonly AnimalSpawnService _spawnService;
    private readonly Random _random;

    private Dictionary<(Animal First, Animal Second), int> _nearbyRounds = [];

    /// Summary:
    /// Creates the birth service for one simulation.
    public BirthService(AnimalSpawnService spawnService, Random random)
    {
        ArgumentNullException.ThrowIfNull(spawnService);
        ArgumentNullException.ThrowIfNull(random);

        _spawnService = spawnService;
        _random = random;
    }

    /// Summary:
    /// Checks each surviving pair once after all animals finish their turns.
    public void ProcessRound(SavannaState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var parents = state.Animals
            .Where(animal => animal.Health > 0m)
            .ToArray();

        var nextCounts = new Dictionary<(Animal First, Animal Second), int>();

        for (int firstIndex = 0; firstIndex < parents.Length; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1;
                 secondIndex < parents.Length;
                 secondIndex++)
            {
                var first = parents[firstIndex];
                var second = parents[secondIndex];

                if (first.Type != second.Type ||
                    MovementService.GetDistance(first.Position, second.Position)
                        > BirthDistance)
                {
                    continue;
                }

                var pair = (First: first, Second: second);

                _nearbyRounds.TryGetValue(pair, out int previousCount);
                int count = Math.Min(previousCount + 1, RequiredNearbyRounds);

                if (count == RequiredNearbyRounds &&
                    TryGiveBirth(state, first, second))
                {
                    count = 0;
                }

                nextCounts[pair] = count;
            }
        }

        _nearbyRounds = nextCounts;
    }

    /// Summary:
    /// Spawns one offspring in an empty cell next to either parent.
    private bool TryGiveBirth(SavannaState state, Animal first, Animal second)
    {
        if (state.Animals.Count == state.Width * state.Height)
        {
            return false;
        }

        var occupied = state.Animals
            .Select(animal => animal.Position)
            .ToHashSet();

        var emptyPositions = new HashSet<Position>();

        foreach (var parent in new[] { first, second })
        {
            for (int row = Math.Max(0, parent.Position.Row - BirthDistance);
                 row <= Math.Min(
                     state.Height - 1,
                     parent.Position.Row + BirthDistance);
                 row++)
            {
                for (int column = Math.Max(
                         0,
                         parent.Position.Column - BirthDistance);
                     column <= Math.Min(
                         state.Width - 1,
                         parent.Position.Column + BirthDistance);
                     column++)
                {
                    var position = new Position(row, column);

                    if (!occupied.Contains(position))
                    {
                        emptyPositions.Add(position);
                    }
                }
            }
        }

        if (emptyPositions.Count == 0)
        {
            return false;
        }

        var positions = emptyPositions.ToArray();
        var birthPosition = positions[_random.Next(positions.Length)];

        return _spawnService.TryAddAnimalAt(state, first.Type, birthPosition);
    }
}