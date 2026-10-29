namespace Savanna.Core.Models;

public readonly record struct Position
{
    public int Row { get; }
    public int Column { get; }

    /// Summary:
    /// Creates a position on the field.
    public Position(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);

        Row = row;
        Column = column;
    }
}