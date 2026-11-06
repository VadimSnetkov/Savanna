namespace Savanna.Core.Configuration;

public sealed class AnimalOptions
{
    public char Symbol { get; set; }
    public char SpawnKey { get; set; }
    public bool IsPredator { get; set; }

    public int Speed { get; set; }
    public int VisionRange { get; set; }
    public decimal InitialHealth { get; set; }
}
