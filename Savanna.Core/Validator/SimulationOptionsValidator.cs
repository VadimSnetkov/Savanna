using FluentValidation;
using Savanna.Core.Configuration;

namespace Savanna.Core.Validator;

public sealed class SimulationOptionsValidator
    : AbstractValidator<SimulationOptions>
{
    private const int MaximumFieldSize = 100;

    /// Summary:
    /// Defines valid field, timing and animal settings.
    public SimulationOptionsValidator()
    {
        RuleFor(options => options.PluginDirectory).NotEmpty();

        RuleFor(options => options.AdditionalAnimals)
            .NotNull()
            .Must(animals => animals is null ||
                (!animals.ContainsKey("Antelope") && !animals.ContainsKey("Lion")))
            .WithMessage("AdditionalAnimals cannot replace built-in settings.");

        When(options => options.AdditionalAnimals is not null, () =>
        {
            RuleForEach(options => options.AdditionalAnimals)
                .ChildRules(entry =>
                {
                    entry.RuleFor(item => item.Key).Matches("^[A-Za-z][A-Za-z0-9_]*$");
                    entry.RuleFor(item => item.Value)
                        .NotNull()
                        .SetValidator(new AnimalOptionsValidator());
                });
        });

        RuleFor(options => options.Width)
            .InclusiveBetween(1, MaximumFieldSize);

        RuleFor(options => options.Height)
            .InclusiveBetween(1, MaximumFieldSize);

        RuleFor(options => options.RoundDelayMilliseconds)
            .GreaterThan(0);

        RuleFor(options => options.Antelope)
            .NotNull()
            .SetValidator(new AnimalOptionsValidator());

        RuleFor(options => options.Lion)
            .NotNull()
            .SetValidator(new AnimalOptionsValidator());

        RuleFor(options => options.PredatorEatingHealthGain)
            .GreaterThan(0);
    }
}
