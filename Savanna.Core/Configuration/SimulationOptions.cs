namespace Savanna.Core.Configuration;

public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    public string PluginDirectory { get; set; } = "Plugins";
    public Dictionary<string, AnimalOptions> AdditionalAnimals { get; set; } = [];

    public int Width { get; set; }
    public int Height { get; set; }
    public int RoundDelayMilliseconds { get; set; }
    public decimal PredatorEatingHealthGain { get; set; }

    public AnimalOptions Antelope { get; set; } = new();
    public AnimalOptions Lion { get; set; } = new();
}
