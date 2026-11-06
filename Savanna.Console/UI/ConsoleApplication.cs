using System.Diagnostics;
using Savanna.Core.Configuration;
using Savanna.Core.Models;
using Savanna.Core.Service;
using Terminal = System.Console;

namespace Savanna.Console.UI;

public sealed class ConsoleApplication
{
    private const int InputPollMilliseconds = 20;

    private readonly SimulationService _simulation;
    private readonly AnimalSpawnService _spawnService;
    private readonly ConsoleRenderer _renderer;
    private readonly SimulationOptions _options;
    private readonly AnimalCatalog _catalog;

    private string _message = "Press a species key shown above to add an animal.";

    /// Summary:
    /// Creates the console frontend with its required services.
    public ConsoleApplication(
        SimulationService simulation,
        AnimalSpawnService spawnService,
        ConsoleRenderer renderer,
        SimulationOptions options,
        AnimalCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(spawnService);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(catalog);

        _simulation = simulation;
        _spawnService = spawnService;
        _renderer = renderer;
        _options = options;
        _catalog = catalog;
    }

    /// Summary:
    /// Reads keyboard input and advances the simulation at regular intervals.
    public async Task RunAsync()
    {
        bool previousCursorVisibility = OperatingSystem.IsWindows()
            ? Terminal.CursorVisible
            : true;
        Terminal.CursorVisible = false;

        try
        {
            _renderer.Render(_simulation.State, _catalog.LastBehaviorError ?? _message);

            var roundTimer = Stopwatch.StartNew();

            while (true)
            {
                bool shouldRender = false;

                if (Terminal.KeyAvailable)
                {
                    var key = Terminal.ReadKey(intercept: true);

                    if (key.Key == ConsoleKey.Escape)
                    {
                        return;
                    }

                    var species = _catalog.Animals.FirstOrDefault(entry =>
                        char.ToUpperInvariant(entry.Value.SpawnKey) ==
                        char.ToUpperInvariant(key.KeyChar));

                    if (species.Key is not null)
                    {
                        CheckToAddAnimal(species.Key);
                        shouldRender = true;
                    }
                }

                if (roundTimer.ElapsedMilliseconds >=
                    _options.RoundDelayMilliseconds)
                {
                    _simulation.AdvanceRound();
                    roundTimer.Restart();
                    shouldRender = true;
                }

                if (shouldRender)
                {
                    _renderer.Render(_simulation.State, _catalog.LastBehaviorError ?? _message);
                }

                await Task.Delay(InputPollMilliseconds);
            }
        }
        finally
        {
            Terminal.CursorVisible = previousCursorVisibility;
            Terminal.WriteLine();
        }
    }

    /// Summary:
    /// Requests an animal spawn and updates the displayed message.
    private void CheckToAddAnimal(string type)
    {
        bool added = _spawnService.TryAddAnimal(
            _simulation.State,
            type);

        _message = added
            ? $"{type} added."
            : "The field is full.";
    }
}
