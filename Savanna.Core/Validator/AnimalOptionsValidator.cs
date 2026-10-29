using FluentValidation;
using Savanna.Core.Configuration;

namespace Savanna.Core.Validator;

public sealed class AnimalOptionsValidator : AbstractValidator<AnimalOptions>
{
    /// Summary:
    /// Defines valid movement settings for an animal.
    public AnimalOptionsValidator()
    {
        RuleFor(options => options.Speed)
            .GreaterThan(0);

        RuleFor(options => options.VisionRange)
            .GreaterThanOrEqualTo(0);
    }
}