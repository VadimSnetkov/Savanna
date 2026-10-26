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

    private string _message = "Press A or L to add animals.";

    /// Summary:
    /// Creates the console frontend with its required services.
    public ConsoleApplication(
        SimulationService simulation,
        AnimalSpawnService spawnService,
        ConsoleRenderer renderer,
        SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(spawnService);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(options);

        _simulation = simulation;
        _spawnService = spawnService;
        _renderer = renderer;
        _options = options;
    }

    /// Summary:
    /// Reads keyboard input and advances the simulation at regular intervals.
    public async Task RunAsync()
    {
        bool previousCursorVisibility = Terminal.CursorVisible;
        Terminal.CursorVisible = false;

        try
        {
            _renderer.Render(_simulation.State, _message);

            var roundTimer = Stopwatch.StartNew();

            while (true)
            {
                bool shouldRender = false;

                if (Terminal.KeyAvailable)
                {
                    var key = Terminal.ReadKey(intercept: true).Key;

                    switch (key)
                    {
                        case ConsoleKey.Escape:
                            return;

                        case ConsoleKey.A:
                            AddAnimal(AnimalType.Antelope);
                            shouldRender = true;
                            break;

                        case ConsoleKey.L:
                            AddAnimal(AnimalType.Lion);
                            shouldRender = true;
                            break;
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
                    _renderer.Render(_simulation.State, _message);
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
    private void AddAnimal(AnimalType type)
    {
        bool added = _spawnService.TryAddAnimal(
            _simulation.State,
            type);

        _message = added
            ? $"{type} added."
            : "The field is full.";
    }
}