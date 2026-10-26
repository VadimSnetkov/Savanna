namespace Savanna.Core.Models;

public sealed class SavannaState
{
    internal List<Animal> MutableAnimals { get; } = [];

    public IReadOnlyList<Animal> Animals { get; }

    public int Width { get; }
    public int Height { get; }
    public long Round { get; internal set; }

    /// Summary:
    /// Creates an empty field with the specified dimensions.
    public SavannaState(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        Animals = MutableAnimals.AsReadOnly();
    }
}