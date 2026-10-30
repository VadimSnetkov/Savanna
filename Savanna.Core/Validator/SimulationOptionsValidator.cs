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

        RuleFor(options => options.LionEatingHealthGain)
            .GreaterThan(0);
    }
}