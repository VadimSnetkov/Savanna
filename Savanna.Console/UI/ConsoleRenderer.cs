using Savanna.Core.Models;
using Savanna.Core.Service;
using Terminal = System.Console;

namespace Savanna.Console.UI;

public sealed class ConsoleRenderer
{
    private const char EmptyCell = '.';
    private const int CellDisplayWidth = 2;
    private const int MinimumDisplayWidth = 64;
    private readonly AnimalCatalog _catalog;

    /// Summary:
    /// Creates a renderer for all registered species.
    public ConsoleRenderer(AnimalCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
    }

    /// Summary:
    /// Draws registered species, health statistics and the field.
    public void Render(SavannaState state, string message)
    {
        var lines = new List<string>
        {
            $"SAVANNA | Round: {state.Round} | Esc: exit"
        };

        foreach (var entry in _catalog.Animals)
        {
            var animals = state.Animals.Where(animal => animal.Type == entry.Key).ToArray();
            decimal health = animals.Select(animal => animal.Health).DefaultIfEmpty(0m).Average();
            lines.Add(
                $"{entry.Value.SpawnKey}: {entry.Key} ({entry.Value.Symbol}) | Count: {animals.Length} | HP: {health:0.0}");
        }

        string border = "+" + new string('-', state.Width * CellDisplayWidth) + "+";
        var occupied = state.Animals.ToDictionary(animal => animal.Position);
        lines.Add(border);

        for (int row = 0; row < state.Height; row++)
        {
            var cells = new System.Text.StringBuilder("|");

            for (int column = 0; column < state.Width; column++)
            {
                char symbol = occupied.TryGetValue(new Position(row, column), out var animal)
                    ? _catalog.GetOptions(animal.Type).Symbol
                    : EmptyCell;
                cells.Append(symbol).Append(' ');
            }

            lines.Add(cells.Append('|').ToString());
        }

        lines.Add(border);
        string cleanMessage = message.Replace('\r', ' ').Replace('\n', ' ');
        lines.Add(cleanMessage.Length > MinimumDisplayWidth
            ? cleanMessage[..MinimumDisplayWidth]
            : cleanMessage);

        int requiredWidth = Math.Max(MinimumDisplayWidth, lines.Max(line => line.Length));
        int requiredHeight = lines.Count + 1;

        Terminal.Clear();

        if (Terminal.WindowWidth <= requiredWidth || Terminal.WindowHeight < requiredHeight)
        {
            Terminal.Write($"Enlarge console to {requiredWidth + 1}x{requiredHeight}.");
            return;
        }

        Terminal.Write(string.Join(Environment.NewLine, lines));
    }
}
