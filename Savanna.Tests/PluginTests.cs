using System.Runtime.Loader;
using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Models;
using Savanna.Core.Service;
using Savanna.Core.Validator;
using Savanna.Tests.Helpers;

namespace Savanna.Tests;

public sealed class PluginTests
{
    private const string ZebraType = "Zebra";

    /// Summary:
    /// Verifies a DLL is loaded in its own context through the shared interface.
    [Fact]
    public void Load_ValidPlugin_ReturnsCompatibleBehavior()
    {
        var result = LoadZebra();
        var behavior = Assert.Single(result.Behaviors);

        Assert.Empty(result.Warnings);
        Assert.Equal(ZebraType, behavior.Type);
        Assert.NotSame(
            AssemblyLoadContext.Default,
            AssemblyLoadContext.GetLoadContext(behavior.GetType().Assembly));
    }

    /// Summary:
    /// Verifies a missing plugin folder is allowed.
    [Fact]
    public void Load_MissingFolder_ReturnsNoPlugins()
    {
        var result = new PluginLoader().Load(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.Empty(result.Behaviors);
        Assert.Empty(result.Warnings);
    }

    /// Summary:
    /// Verifies that corrupt DLL files are reported instead of crashing startup.
    [Fact]
    public void Load_InvalidDll_ReturnsWarning()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        try
        {
            File.WriteAllText(
                Path.Combine(folder, "Savanna.Animals.Broken.dll"),
                "This is not an assembly.");

            var result = new PluginLoader().Load(folder);

            Assert.Empty(result.Behaviors);
            Assert.Single(result.Warnings);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// Summary:
    /// Verifies that a lion can catch and eat an imported species.
    [Fact]
    public void AdvanceRound_LionEatsImportedZebra()
    {
        var (simulation, spawner) = CreateGame(2, 1);
        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, AnimalType.Lion, new Position(0, 0)));
        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, ZebraType, new Position(0, 1)));

        simulation.AdvanceRound();

        var survivor = Assert.Single(simulation.State.Animals);
        Assert.Equal(AnimalType.Lion, survivor.Type);
        Assert.Equal(29.5m, survivor.Health);
    }

    /// Summary:
    /// Verifies that an imported animal pays movement costs and dies at zero.
    [Fact]
    public void AdvanceRound_ImportedZebraLosesHealthAndDies()
    {
        var (simulation, spawner) = CreateGame(2, 1, zebraHealth: 1m);
        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, ZebraType, new Position(0, 0)));

        simulation.AdvanceRound();
        Assert.Equal(0.5m, Assert.Single(simulation.State.Animals).Health);

        simulation.AdvanceRound();
        Assert.Empty(simulation.State.Animals);
    }

    /// Summary:
    /// Verifies imported animals reproduce using the existing birth service.
    [Fact]
    public void AdvanceRound_ImportedZebrasGiveBirth()
    {
        var (simulation, spawner) = CreateGame(2, 2);
        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, ZebraType, new Position(0, 0)));
        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, ZebraType, new Position(1, 1)));
        var parents = simulation.State.Animals.ToArray();

        simulation.AdvanceRound();
        simulation.AdvanceRound();
        Assert.Equal(2, simulation.State.Animals.Count);
        simulation.AdvanceRound();

        var child = Assert.Single(simulation.State.Animals.Except(parents));
        Assert.Equal(ZebraType, child.Type);
        Assert.Equal(20m, child.Health);
        Assert.Equal(3, simulation.State.Animals.Select(a => a.Position).Distinct().Count());
    }

    /// Summary:
    /// Verifies a faulty behaviour cannot apply a forbidden movement.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdvanceRound_FaultyBehaviorStaysAndReportsError(bool throws)
    {
        var options = CreateOptions(2, 1, 20m);
        var behavior = new TestAnimalBehavior(AnimalType.Antelope)
        {
            ChoosePosition = (_, _) => throws
                ? throw new InvalidOperationException("Broken plugin.")
                : new Position(99, 99)
        };
        var catalog = new AnimalCatalog(options, new[] { behavior });
        var random = new Random(42);
        var spawner = new AnimalSpawnService(catalog, random);
        var simulation = new SimulationService(
            options, new SimulationOptionsValidator(), catalog, random,
            new HealthService(options), new BirthService(spawner, random));

        Assert.True(spawner.TryAddAnimalAt(
            simulation.State, AnimalType.Antelope, new Position(0, 0)));

        simulation.AdvanceRound();

        var animal = Assert.Single(simulation.State.Animals);
        Assert.Equal(new Position(0, 0), animal.Position);
        Assert.Equal(20m, animal.Health);
        Assert.False(string.IsNullOrWhiteSpace(catalog.LastBehaviorError));
        Assert.Equal(1L, simulation.State.Round);
    }

    /// Summary:
    /// Verifies a plugin cannot replace an existing species or reuse its key.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnimalCatalog_RejectsDuplicateSpeciesOrKey(bool duplicateKey)
    {
        var options = CreateOptions(2, 2, 20m);
        string pluginType = duplicateKey ? ZebraType : AnimalType.Antelope;
        if (duplicateKey)
        {
            options.AdditionalAnimals[ZebraType].SpawnKey = 'a';
        }

        var catalog = new AnimalCatalog(options, new IAnimalBehavior[]
        {
            new TestAnimalBehavior(AnimalType.Antelope),
            new TestAnimalBehavior(pluginType)
        });

        Assert.Single(catalog.Animals);
        Assert.Contains(catalog.Warnings, warning => warning.Contains("duplicate"));
    }

    /// Summary:
    /// Verifies a behaviour without matching configuration is not registered.
    [Fact]
    public void AnimalCatalog_RejectsUnconfiguredSpecies()
    {
        var catalog = new AnimalCatalog(
            CreateOptions(2, 2, 20m),
            new[] { new TestAnimalBehavior("Unknown") });

        Assert.Empty(catalog.Animals);
        Assert.Throws<ArgumentException>(() => catalog.GetOptions("Unknown"));
    }

    /// Summary:
    /// Loads the separately built plugin fixture from the test output.
    private static PluginLoadResult LoadZebra()
    {
        return new PluginLoader().Load(
            Path.Combine(AppContext.BaseDirectory, "PluginFixtures"));
    }

    /// Summary:
    /// Creates an engine using built-in behaviours and the actual imported DLL.
    private static (
        SimulationService Simulation,
        AnimalSpawnService Spawner) CreateGame(
        int width, int height, decimal zebraHealth = 20m)
    {
        var options = CreateOptions(width, height, zebraHealth);
        var random = new Random(42);
        var movement = new MovementService(random);
        var plugins = LoadZebra();
        Assert.Single(plugins.Behaviors);

        var behaviors = new List<IAnimalBehavior>
        {
            new AntelopeBehavior(movement),
            new LionBehavior(movement)
        };
        behaviors.AddRange(plugins.Behaviors);

        var catalog = new AnimalCatalog(options, behaviors);
        Assert.Empty(catalog.Warnings);
        var spawner = new AnimalSpawnService(catalog, random);
        var simulation = new SimulationService(
            options, new SimulationOptionsValidator(), catalog, random,
            new HealthService(options), new BirthService(spawner, random));

        return (simulation, spawner);
    }

    /// Summary:
    /// Creates settings for built-in species and a configured Zebra plugin.
    private static SimulationOptions CreateOptions(
        int width, int height, decimal zebraHealth)
    {
        return new SimulationOptions
        {
            Width = width,
            Height = height,
            RoundDelayMilliseconds = 400,
            PredatorEatingHealthGain = 10m,
            Antelope = new AnimalOptions
            {
                Symbol = 'a', SpawnKey = 'A',
                Speed = 1, VisionRange = 5, InitialHealth = 20m
            },
            Lion = new AnimalOptions
            {
                Symbol = 'L', SpawnKey = 'L', IsPredator = true,
                Speed = 2, VisionRange = 7, InitialHealth = 20m
            },
            AdditionalAnimals = new Dictionary<string, AnimalOptions>
            {
                [ZebraType] = new AnimalOptions
                {
                    Symbol = 'Z', SpawnKey = 'Z',
                    Speed = 1, VisionRange = 6, InitialHealth = zebraHealth
                }
            }
        };
    }
}
