using FluentValidation;
using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class SimulationService
{
    private const int NeighbourStep = 1;

    private readonly Dictionary<AnimalType, IAnimalBehavior> _behaviors;
    private readonly Random _random;
    private readonly HealthService _healthService;
    private readonly BirthService _birthService;

    public SavannaState State { get; }

    /// Summary:
    /// Validates the settings and creates an empty simulation.
    public SimulationService(
        SimulationOptions options,
        IValidator<SimulationOptions> validator,
        IEnumerable<IAnimalBehavior> behaviors,
        Random random,
        HealthService healthService,
        BirthService birthService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(healthService);
        ArgumentNullException.ThrowIfNull(birthService);

        validator.ValidateAndThrow(options);

        _behaviors = behaviors.ToDictionary(behavior => behavior.Type);
        _random = random;
        _healthService = healthService;
        _birthService = birthService;

        foreach (var type in Enum.GetValues<AnimalType>())
        {
            if (!_behaviors.ContainsKey(type))
            {
                throw new ArgumentException(
                    $"No behaviour is registered for {type}.",
                    nameof(behaviors));
            }
        }

        State = new SavannaState(options.Width, options.Height);
    }

    /// Summary:
    /// Gives surviving animals their turns, then checks for births.
    public void AdvanceRound()
    {
        var animalsThisRound = State.Animals.ToArray();

        _random.Shuffle(animalsThisRound);

        foreach (var animal in animalsThisRound)
        {
            if (!State.MutableAnimals.Contains(animal) ||
                _healthService.RemoveIfDead(State, animal))
            {
                continue;
            }

            MoveAnimal(animal);
        }

        State.Round++;
        _birthService.ProcessRound(State);
    }

    /// Summary:
    /// Moves an animal up to its speed, applying eating and health rules.
    private void MoveAnimal(Animal animal)
    {
        var behavior = _behaviors[animal.Type];

        for (int step = 0; step < animal.Speed; step++)
        {
            var allowedPositions = GetAllowedPositions(animal);

            var nextPosition = behavior.GetNextPosition(
                animal,
                State.Animals,
                allowedPositions);

            if (!allowedPositions.Contains(nextPosition))
            {
                throw new InvalidOperationException(
                    $"{animal.Type} selected an invalid position.");
            }

            if (nextPosition == animal.Position)
            {
                break;
            }

            var prey = State.Animals.FirstOrDefault(other =>
                other != animal &&
                other.Position == nextPosition);

            if (prey is not null)
            {
                State.MutableAnimals.Remove(prey);
                _healthService.FeedLion(animal);
            }

            animal.Position = nextPosition;
            _healthService.ApplyMovementCost(animal);

            if (_healthService.RemoveIfDead(State, animal) || prey is not null)
            {
                break;
            }
        }
    }

    /// Summary:
    /// Finds neighbouring cells the animal may enter, including its own.
    private List<Position> GetAllowedPositions(Animal animal)
    {
        var positions = new List<Position>();

        for (int rowOffset = -NeighbourStep;
             rowOffset <= NeighbourStep;
             rowOffset++)
        {
            for (int columnOffset = -NeighbourStep;
                 columnOffset <= NeighbourStep;
                 columnOffset++)
            {
                int row = animal.Position.Row + rowOffset;
                int column = animal.Position.Column + columnOffset;

                if (row < 0 || row >= State.Height ||
                    column < 0 || column >= State.Width)
                {
                    continue;
                }

                var position = new Position(row, column);

                var occupant = State.Animals.FirstOrDefault(other =>
                    other.Position == position);

                bool canEnter =
                    occupant is null ||
                    occupant == animal ||
                    (animal.Type == AnimalType.Lion &&
                     occupant.Type == AnimalType.Antelope);

                if (canEnter)
                {
                    positions.Add(position);
                }
            }
        }

        return positions;
    }
}