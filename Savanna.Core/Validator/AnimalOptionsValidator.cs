using FluentValidation;
using Savanna.Core.Configuration;

namespace Savanna.Core.Validator;

public sealed class AnimalOptionsValidator : AbstractValidator<AnimalOptions>
{
    /// Summary:
    /// Defines valid movement settings for an animal.
    public AnimalOptionsValidator()
    {
        RuleFor(options => options.Symbol)
            .Must(char.IsAsciiLetterOrDigit)
            .WithMessage("Symbol must be an ASCII letter or digit.");

        RuleFor(options => options.SpawnKey)
            .Must(char.IsAsciiLetterOrDigit)
            .WithMessage("SpawnKey must be an ASCII letter or digit.");

        RuleFor(options => options.Speed)
            .GreaterThan(0);

        RuleFor(options => options.VisionRange)
            .GreaterThanOrEqualTo(0);
        RuleFor(options => options.InitialHealth)
            .GreaterThan(0);
    }
}
