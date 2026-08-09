using Microsoft.Extensions.DependencyInjection;
using Vacanam.Infrastructure.Configuration;

namespace Vacanam.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVacanamServices(this IServiceCollection services)
    {
        services.AddSingleton<SettingsManager>();
        return services;
    }

    public static IServiceCollection AddVacanamDirectories(this IServiceCollection services)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string baseDir = System.IO.Path.Combine(appData, "Vacanam");

        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(baseDir, "Models", "Whisper"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(baseDir, "Models", "LLM"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(baseDir, "Logs"));

        return services;
    }
}
