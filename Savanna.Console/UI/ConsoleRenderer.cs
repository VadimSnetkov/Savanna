using System.Text;
using Savanna.Core.Models;
using Terminal = System.Console;

namespace Savanna.Console.UI;

public sealed class ConsoleRenderer
{
    private const char EmptyCell = '.';
    private const char AntelopeSymbol = 'A';
    private const char LionSymbol = 'L';

    private const int CellDisplayWidth = 2;
    private const int BorderWidth = 2;
    private const int MinimumDisplayWidth = 64;
    private const int ExtraDisplayRows = 6;

    /// Summary:
    /// Draws the field, animal counts and keyboard instructions.
    public void Render(SavannaState state, string message)
    {
        int requiredWidth = Math.Max(
            state.Width * CellDisplayWidth + BorderWidth,
            MinimumDisplayWidth);

        int requiredHeight = state.Height + ExtraDisplayRows;

        Terminal.Clear();

        if (Terminal.WindowWidth <= requiredWidth ||
            Terminal.WindowHeight < requiredHeight)
        {
            Terminal.Write(
                $"Enlarge console to {requiredWidth + 1}x{requiredHeight}.");
            return;
        }

        var cells = new char[state.Height, state.Width];

        for (int row = 0; row < state.Height; row++)
        {
            for (int column = 0; column < state.Width; column++)
            {
                cells[row, column] = EmptyCell;
            }
        }

        foreach (var animal in state.Animals)
        {
            cells[animal.Position.Row, animal.Position.Column] =
                animal.Type switch
                {
                    AnimalType.Antelope => AntelopeSymbol,
                    AnimalType.Lion => LionSymbol,
                    _ => '?'
                };
        }

        int antelopeCount = state.Animals.Count(animal =>
            animal.Type == AnimalType.Antelope);

        int lionCount = state.Animals.Count(animal =>
            animal.Type == AnimalType.Lion);

        string border =
            "+" + new string('-', state.Width * CellDisplayWidth) + "+";

        var output = new StringBuilder();

        output.AppendLine(
            "SAVANNA | A: add antelope | L: add lion | Esc: exit");

        output.AppendLine(
            $"Round: {state.Round} | Antelopes: {antelopeCount} | Lions: {lionCount}");

        output.AppendLine(border);

        for (int row = 0; row < state.Height; row++)
        {
            output.Append('|');

            for (int column = 0; column < state.Width; column++)
            {
                output.Append(cells[row, column]);
                output.Append(' ');
            }

            output.AppendLine("|");
        }

        output.AppendLine(border);
        output.Append(message);

        Terminal.Write(output.ToString());
    }
}