using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Savanna.Console.UI;
using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Service;
using Savanna.Core.Validator;
using Terminal = System.Console;

namespace Savanna.Console;

internal static class Program
{
    /// Summary:
    /// Loads configuration, builds dependencies and starts the application.
    private static async Task Main()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .Build();

            var options = configuration
                .GetRequiredSection(SimulationOptions.SectionName)
                .Get<SimulationOptions>()
                ?? throw new InvalidOperationException(
                    "Simulation configuration is missing.");

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(options);
            services.AddSingleton<Random>(new Random());

            services.AddSingleton<
                IValidator<SimulationOptions>,
                SimulationOptionsValidator>();

            services.AddSingleton<MovementService>();

            services.AddSingleton<IAnimalBehavior, AntelopeBehavior>();
            services.AddSingleton<IAnimalBehavior, LionBehavior>();

            services.AddSingleton<AnimalSpawnService>();
            services.AddSingleton<SimulationService>();

            services.AddSingleton<ConsoleRenderer>();
            services.AddSingleton<ConsoleApplication>();

            using var serviceProvider = services.BuildServiceProvider();

            var application =
                serviceProvider.GetRequiredService<ConsoleApplication>();

            await application.RunAsync();
        }
        catch (ValidationException exception)
        {
            Terminal.WriteLine("Invalid simulation settings:");

            foreach (var error in exception.Errors)
            {
                Terminal.WriteLine($"- {error.ErrorMessage}");
            }

            Environment.ExitCode = 1;
        }
        catch (Exception exception)
        {
            Terminal.WriteLine($"Unable to run Savanna: {exception.Message}");
            Environment.ExitCode = 1;
        }
    }
}