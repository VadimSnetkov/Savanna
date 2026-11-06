namespace Savanna.Core.Models;

public sealed class Animal
{
    public string Type { get; }
    public bool IsPredator { get; }
    public int Speed { get; }
    public int VisionRange { get; }

    public decimal Health { get; internal set; }
    public Position Position { get; internal set; }

    /// Summary:
    /// Creates an animal with its movement settings, position and health.
    public Animal(
        string type,
        int speed,
        int visionRange,
        Position position,
        decimal initialHealth,
        bool isPredator = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speed);
        ArgumentOutOfRangeException.ThrowIfNegative(visionRange);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialHealth);

        Type = type;
        IsPredator = isPredator;
        Speed = speed;
        VisionRange = visionRange;
        Position = position;
        Health = initialHealth;
    }
}
