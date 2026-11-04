namespace Savanna.Core.Models;

public sealed class Animal
{
    public AnimalType Type { get; }
    public int Speed { get; }
    public int VisionRange { get; }

    public decimal Health { get; internal set; }
    public Position Position { get; internal set; }

    /// Summary:
    /// Creates an animal with its movement settings, position and health.
    public Animal(
        AnimalType type,
        int speed,
        int visionRange,
        Position position,
        decimal initialHealth)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speed);
        ArgumentOutOfRangeException.ThrowIfNegative(visionRange);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialHealth);

        Type = type;
        Speed = speed;
        VisionRange = visionRange;
        Position = position;
        Health = initialHealth;
    }
}