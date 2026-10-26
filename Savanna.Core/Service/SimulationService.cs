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

    public SavannaState State { get; }

    /// Summary:
    /// Validates the settings and creates an empty simulation.
    public SimulationService(
        SimulationOptions options,
        IValidator<SimulationOptions> validator,
        IEnumerable<IAnimalBehavior> behaviors,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(random);

        validator.ValidateAndThrow(options);

        _behaviors = behaviors.ToDictionary(behavior => behavior.Type);
        _random = random;

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
    /// Gives each surviving animal one turn in a shuffled order.
    public void AdvanceRound()
    {
        var animalsThisRound = State.Animals.ToArray();

        _random.Shuffle(animalsThisRound);

        foreach (var animal in animalsThisRound)
        {
            if (!State.MutableAnimals.Contains(animal))
            {
                continue;
            }

            MoveAnimal(animal);
        }

        State.Round++;
    }

    /// Summary:
    /// Moves an animal up to its speed and stops after eating prey.
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
            }

            animal.Position = nextPosition;

            if (prey is not null)
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