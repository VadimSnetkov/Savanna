using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Models;
using Savanna.Core.Service;
using Savanna.Core.Validator;
using Savanna.Tests.Helpers;
using Xunit;

namespace Savanna.Tests;

public sealed class LifeCycleTests
{
    /// Summary:
    /// Verifies the cost of each cell travelled, including faster movement.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AdvanceRound_DeductsHalfHealthPerCell(int speed)
    {
        var (simulation, spawner, _) = CreateGame(
            2, 1, antelopeSpeed: speed);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 0)));

        simulation.AdvanceRound();

        var animal = Assert.Single(simulation.State.Animals);
        Assert.Equal(20m - speed * 0.5m, animal.Health);
    }

    /// Summary:
    /// Verifies that blocked animals do not pay a movement cost.
    [Fact]
    public void AdvanceRound_WhenBlocked_DoesNotReduceHealth()
    {
        var (simulation, spawner, _) = CreateGame(1, 1);

        Assert.True(spawner.TryAddAnimal(
            simulation.State,
            AnimalType.Antelope));

        simulation.AdvanceRound();

        Assert.Equal(20m, Assert.Single(simulation.State.Animals).Health);
    }

    /// Summary:
    /// Verifies that an animal is removed as soon as its health is exhausted.
    [Fact]
    public void AdvanceRound_WhenHealthRunsOut_RemovesAnimal()
    {
        var (simulation, spawner, behavior) = CreateGame(
            4, 1,
            initialHealth: 0.5m,
            antelopeSpeed: 3,
            holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 0)));

        var animal = Assert.Single(simulation.State.Animals);

        behavior.ChoosePosition = (current, _) =>
            new Position(current.Position.Row, current.Position.Column + 1);

        simulation.AdvanceRound();

        Assert.Empty(simulation.State.Animals);
        Assert.Equal(0m, animal.Health);
        Assert.Equal(new Position(0, 1), animal.Position);
    }

    /// Summary:
    /// Verifies that eating rewards health and can save a lion on its last move.
    [Theory]
    [InlineData(20.0)]
    [InlineData(0.5)]
    public void AdvanceRound_WhenLionEats_AddsHealth(double startingHealth)
    {
        decimal initialHealth = (decimal)startingHealth;

        var (simulation, spawner, _) = CreateGame(
            2, 1, initialHealth: initialHealth);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Lion,
            new Position(0, 0)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 1)));

        simulation.AdvanceRound();

        var lion = Assert.Single(simulation.State.Animals);

        Assert.Equal(AnimalType.Lion, lion.Type);
        Assert.Equal(initialHealth + 10m - 0.5m, lion.Health);
    }

    /// Summary:
    /// Verifies births on the third round and a fresh counter after each birth.
    [Theory]
    [InlineData(AnimalType.Antelope)]
    [InlineData(AnimalType.Lion)]
    public void AdvanceRound_AfterThreeNearbyRounds_CreatesOffspring(
        AnimalType type)
    {
        var (simulation, spawner, _) = CreateGame(
            5, 3, holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, type, new Position(1, 1)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, type, new Position(1, 2)));

        var parents = simulation.State.Animals.ToArray();

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        Assert.Equal(2, simulation.State.Animals.Count);

        simulation.AdvanceRound();

        var child = Assert.Single(
            simulation.State.Animals.Except(parents));

        Assert.Equal(type, child.Type);
        Assert.Equal(20m, child.Health);

        Assert.Contains(parents, parent =>
            MovementService.GetDistance(parent.Position, child.Position) == 1);

        Assert.Equal(
            3,
            simulation.State.Animals
                .Select(animal => animal.Position)
                .Distinct()
                .Count());

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        Assert.Equal(3, simulation.State.Animals.Count);
    }

    /// Summary:
    /// Verifies that a round apart resets the pair's consecutive-round counter.
    [Fact]
    public void AdvanceRound_WhenParentsSeparate_RestartsBirthCounter()
    {
        var (simulation, spawner, behavior) = CreateGame(
            4, 1, holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 0)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 1)));

        var movingParent = simulation.State.Animals[1];

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        behavior.ChoosePosition = (animal, _) =>
            animal == movingParent ? new Position(0, 2) : animal.Position;

        simulation.AdvanceRound();

        behavior.ChoosePosition = (animal, _) =>
            animal == movingParent ? new Position(0, 1) : animal.Position;

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        Assert.Equal(2, simulation.State.Animals.Count);

        simulation.AdvanceRound();

        Assert.Equal(3, simulation.State.Animals.Count);
    }

    /// Summary:
    /// Verifies that newborns first move in the round after their birth.
    [Fact]
    public void AdvanceRound_NewbornStartsMovingNextRound()
    {
        var (simulation, spawner, behavior) = CreateGame(
            5, 3, holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(1, 1)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(1, 2)));

        var parents = simulation.State.Animals.ToArray();

        behavior.ChoosePosition = (animal, allowed) =>
            parents.Contains(animal)
                ? animal.Position
                : allowed.First(position => position != animal.Position);

        simulation.AdvanceRound();
        simulation.AdvanceRound();
        simulation.AdvanceRound();

        var child = Assert.Single(
            simulation.State.Animals.Except(parents));

        Assert.Equal(20m, child.Health);

        simulation.AdvanceRound();

        Assert.Equal(19.5m, child.Health);
    }

    /// Summary:
    /// Verifies that a dead parent cannot complete a pending birth.
    [Fact]
    public void AdvanceRound_WhenParentDies_DoesNotCreateOffspring()
    {
        var (simulation, spawner, _) = CreateGame(
            3, 1,
            initialHealth: 0.5m,
            holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 0)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 1)));

        var dyingParent = simulation.State.Animals[1];

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        new HealthService(CreateOptions(3, 1, 0.5m, 1))
            .ApplyMovementCost(dyingParent);

        simulation.AdvanceRound();

        Assert.Single(simulation.State.Animals);
        Assert.DoesNotContain(dyingParent, simulation.State.Animals);
    }

    /// Summary:
    /// Verifies that a new partner does not inherit another pair's progress.
    [Fact]
    public void AdvanceRound_WhenPartnerChanges_StartsNewCounter()
    {
        var (simulation, spawner, behavior) = CreateGame(
            5, 1, holdPositions: true);

        foreach (int column in new[] { 0, 1, 4 })
        {
            Assert.True(spawner.TryAddAnimalAt(
                simulation.State,
                AnimalType.Antelope,
                new Position(0, column)));
        }

        var movingParent = simulation.State.Animals[1];

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        behavior.ChoosePosition = (animal, _) =>
            animal == movingParent ? new Position(0, 2) : animal.Position;

        simulation.AdvanceRound();

        behavior.ChoosePosition = (animal, _) =>
            animal == movingParent ? new Position(0, 3) : animal.Position;

        simulation.AdvanceRound();
        simulation.AdvanceRound();

        Assert.Equal(3, simulation.State.Animals.Count);

        simulation.AdvanceRound();

        Assert.Equal(4, simulation.State.Animals.Count);
    }

    /// Summary:
    /// Verifies that neighbouring animals of different types cannot reproduce.
    [Fact]
    public void AdvanceRound_WithDifferentSpecies_DoesNotCreateOffspring()
    {
        var (simulation, spawner, _) = CreateGame(
            3, 1, holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 0)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Lion,
            new Position(0, 1)));

        for (int round = 0; round < 4; round++)
        {
            simulation.AdvanceRound();
        }

        Assert.Equal(2, simulation.State.Animals.Count);
    }

    /// Summary:
    /// Verifies that a full field does not allow offspring to overlap animals.
    [Fact]
    public void AdvanceRound_WhenBirthHasNoSpace_DoesNotCreateOffspring()
    {
        var (simulation, spawner, _) = CreateGame(
            2, 1, holdPositions: true);

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 0)));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State,
            AnimalType.Antelope,
            new Position(0, 1)));

        for (int round = 0; round < 4; round++)
        {
            simulation.AdvanceRound();
        }

        Assert.Equal(2, simulation.State.Animals.Count);

        Assert.Equal(
            2,
            simulation.State.Animals
                .Select(animal => animal.Position)
                .Distinct()
                .Count());
    }

    /// Summary:
    /// Verifies invalid initial health and eating rewards are rejected.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SimulationOptionsValidator_RejectsInvalidHealth(int health)
    {
        var options = CreateOptions(3, 3, health, 1);
        options.LionEatingHealthGain = health;

        var result = new SimulationOptionsValidator().Validate(options);

        Assert.False(result.IsValid);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "Antelope.InitialHealth");

        Assert.Contains(result.Errors,
            error => error.PropertyName == "Lion.InitialHealth");

        Assert.Contains(result.Errors,
            error => error.PropertyName == "LionEatingHealthGain");
    }

    /// Summary:
    /// Creates deterministic services with normal or controlled movement.
    private static (
        SimulationService Simulation,
        AnimalSpawnService Spawner,
        TestAnimalBehavior AntelopeBehavior) CreateGame(
        int width,
        int height,
        decimal initialHealth = 20m,
        int antelopeSpeed = 1,
        bool holdPositions = false)
    {
        var options = CreateOptions(
            width, height, initialHealth, antelopeSpeed);

        var random = new Random(42);
        var movement = new MovementService(random);
        var spawner = new AnimalSpawnService(options, random);
        var antelopeBehavior = new TestAnimalBehavior(AnimalType.Antelope);

        IAnimalBehavior[] behaviors = holdPositions
            ? [antelopeBehavior, new TestAnimalBehavior(AnimalType.Lion)]
            : [new AntelopeBehavior(movement), new LionBehavior(movement)];

        var simulation = new SimulationService(
            options,
            new SimulationOptionsValidator(),
            behaviors,
            random,
            new HealthService(options),
            new BirthService(spawner, random));

        return (simulation, spawner, antelopeBehavior);
    }

    /// Summary:
    /// Creates valid settings for the requested test scenario.
    private static SimulationOptions CreateOptions(
        int width,
        int height,
        decimal initialHealth,
        int antelopeSpeed)
    {
        return new SimulationOptions
        {
            Width = width,
            Height = height,
            RoundDelayMilliseconds = 400,
            LionEatingHealthGain = 10m,
            Antelope = new AnimalOptions
            {
                Speed = antelopeSpeed,
                VisionRange = 5,
                InitialHealth = initialHealth
            },
            Lion = new AnimalOptions
            {
                Speed = 2,
                VisionRange = 7,
                InitialHealth = initialHealth
            }
        };
    }
}