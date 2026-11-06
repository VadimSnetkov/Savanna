using Savanna.Core.Configuration;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class HealthService
{
    private const decimal HealthCostPerMove = 0.5m;

    private readonly decimal _predatorEatingHealthGain;

    /// Summary:
    /// Creates the health service with the configured eating reward.
    public HealthService(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            options.PredatorEatingHealthGain);

        _predatorEatingHealthGain = options.PredatorEatingHealthGain;
    }

    /// Summary:
    /// Deducts health for one completed cell movement.
    public void ApplyMovementCost(Animal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);

        animal.Health = Math.Max(0m, animal.Health - HealthCostPerMove);
    }

    /// Summary:
    /// Restores a predator's health after it eats prey.
    public void FeedPredator(Animal predator)
    {
        ArgumentNullException.ThrowIfNull(predator);

        if (!predator.IsPredator)
        {
            throw new ArgumentException(
                "The animal must be a predator.",
                nameof(predator));
        }

        predator.Health += _predatorEatingHealthGain;
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
