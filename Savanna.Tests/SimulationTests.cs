using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Models;
using Savanna.Core.Service;
using Savanna.Core.Validator;
using Xunit;

namespace Savanna.Tests;

public sealed class SimulationTests
{
    /// Summary:
    /// Verifies that spawning fails without replacing an existing animal.
    [Fact]
    public void TryAddAnimal_WhenFieldIsFull_ReturnsFalse()
    {
        var (simulation, spawner) = CreateGame(width: 1, height: 1);

        bool firstAdded = spawner.TryAddAnimal(
            simulation.State,
            AnimalType.Antelope);

        bool secondAdded = spawner.TryAddAnimal(
            simulation.State,
            AnimalType.Lion);

        Assert.True(firstAdded);
        Assert.False(secondAdded);

        var animal = Assert.Single(simulation.State.Animals);
        Assert.Equal(AnimalType.Antelope, animal.Type);
    }

    /// Summary:
    /// Verifies that a lion eats adjacent prey when the prey cannot escape.
    [Fact]
    public void AdvanceRound_WhenLionReachesAntelope_RemovesAntelope()
    {
        var (simulation, spawner) = CreateGame(width: 2, height: 1);

        Assert.True(spawner.TryAddAnimal(
            simulation.State,
            AnimalType.Antelope));

        Assert.True(spawner.TryAddAnimal(
            simulation.State,
            AnimalType.Lion));

        simulation.AdvanceRound();

        var survivor = Assert.Single(simulation.State.Animals);

        Assert.Equal(AnimalType.Lion, survivor.Type);
        Assert.Equal(1L, simulation.State.Round);
    }

    /// Summary:
    /// Verifies that movement respects boundaries, occupancy and speed.
    [Fact]
    public void AdvanceRound_KeepsAnimalsInsideFieldWithoutOverlapping()
    {
        const int animalCount = 8;
        const int roundsToRun = 30;

        var (simulation, spawner) = CreateGame(width: 6, height: 4);

        for (int index = 0; index < animalCount; index++)
        {
            Assert.True(spawner.TryAddAnimal(
                simulation.State,
                AnimalType.Antelope));
        }

        for (int round = 0; round < roundsToRun; round++)
        {
            var previousPositions = simulation.State.Animals
                .ToDictionary(animal => animal, animal => animal.Position);

            simulation.AdvanceRound();

            Assert.All(simulation.State.Animals, animal =>
            {
                Assert.InRange(
                    animal.Position.Row,
                    0,
                    simulation.State.Height - 1);

                Assert.InRange(
                    animal.Position.Column,
                    0,
                    simulation.State.Width - 1);

                int travelledDistance = MovementService.GetDistance(
                    previousPositions[animal],
                    animal.Position);

                Assert.InRange(travelledDistance, 0, animal.Speed);
            });

            Assert.Equal(
                animalCount,
                simulation.State.Animals
                    .Select(animal => animal.Position)
                    .Distinct()
                    .Count());
        }
    }

    /// Summary:
    /// Verifies that an antelope chooses a cell further from a visible lion.
    [Fact]
    public void AntelopeBehavior_WhenLionIsVisible_MovesAway()
    {
        var movementService = new MovementService(new Random(42));
        var behavior = new AntelopeBehavior(movementService);

        var antelope = new Animal(
            AnimalType.Antelope, 1, 5, new Position(2, 2));

        var lion = new Animal(
            AnimalType.Lion, 2, 7, new Position(2, 1));

        Position[] allowedPositions =
        [
            new Position(2, 2),
            new Position(2, 3),
            new Position(1, 2)
        ];

        var nextPosition = behavior.GetNextPosition(
            antelope,
            new[] { antelope, lion },
            allowedPositions);

        Assert.Equal(new Position(2, 3), nextPosition);
    }

    /// Summary:
    /// Verifies that animals at the vision boundary are seen but farther ones are not.
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void FindNearestVisibleAnimal_RespectsVisionRange(
        int targetColumn,
        bool expectedVisible)
    {
        var movementService = new MovementService(new Random(42));

        var antelope = new Animal(
            AnimalType.Antelope, 1, 2, new Position(0, 0));

        var lion = new Animal(
            AnimalType.Lion, 2, 7, new Position(0, targetColumn));

        var result = movementService.FindNearestVisibleAnimal(
            antelope,
            new[] { antelope, lion },
            AnimalType.Lion);

        if (expectedVisible)
        {
            Assert.Same(lion, result);
        }
        else
        {
            Assert.Null(result);
        }
    }

    /// Summary:
    /// Creates a simulation and spawner with repeatable random behaviour.
    private static (
        SimulationService Simulation,
        AnimalSpawnService Spawner) CreateGame(int width, int height)
    {
        var options = new SimulationOptions
        {
            Width = width,
            Height = height,
            RoundDelayMilliseconds = 400,
            Antelope = new AnimalOptions
            {
                Speed = 1,
                VisionRange = 5
            },
            Lion = new AnimalOptions
            {
                Speed = 2,
                VisionRange = 7
            }
        };

        var random = new Random(42);
        var movementService = new MovementService(random);

        IAnimalBehavior[] behaviors =
        [
            new AntelopeBehavior(movementService),
            new LionBehavior(movementService)
        ];

        var simulation = new SimulationService(
            options,
            new SimulationOptionsValidator(),
            behaviors,
            random);

        var spawner = new AnimalSpawnService(options, random);

        return (simulation, spawner);
    }
}