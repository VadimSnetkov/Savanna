using Savanna.Core.Configuration;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class HealthService
{
    private const decimal HealthCostPerMove = 0.5m;

    private readonly decimal _lionEatingHealthGain;

    /// Summary:
    /// Creates the health service with the configured eating reward.
    public HealthService(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            options.LionEatingHealthGain);

        _lionEatingHealthGain = options.LionEatingHealthGain;
    }

    /// Summary:
    /// Deducts health for one completed cell movement.
    public void ApplyMovementCost(Animal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        animal.Health = Math.Max(0m, animal.Health - HealthCostPerMove);
    }

    /// Summary:
    /// Restores a lion's health after it eats an antelope.
    public void FeedLion(Animal lion)
    {
        ArgumentNullException.ThrowIfNull(lion);

        if (lion.Type != AnimalType.Lion)
        {
            throw new ArgumentException(
                "The animal must be a lion.",
                nameof(lion));
        }

        lion.Health += _lionEatingHealthGain;
    }

    /// Summary:
    /// Removes an animal whose health has reached zero.
    public bool RemoveIfDead(SavannaState state, Animal animal)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(animal);

        return animal.Health <= 0m && state.MutableAnimals.Remove(animal);
    }
}