namespace Savanna.Core.Models;

public sealed class Animal
{
    public AnimalType Type { get; }
    public int Speed { get; }
    public int VisionRange { get; }

    public Position Position { get; internal set; }

    /// Summary:
    /// Creates an animal with its movement settings and starting position.
    public Animal(
        AnimalType type,
        int speed,
        int visionRange,
        Position position)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speed);
        ArgumentOutOfRangeException.ThrowIfNegative(visionRange);

        Type = type;
        Speed = speed;
        VisionRange = visionRange;
        Position = position;
    }
}