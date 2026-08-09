using Microsoft.Extensions.DependencyInjection;
using Vacanam.Core.Interfaces;
using Vacanam.LLM.Model;
using Vacanam.LLM.Processing;

namespace Vacanam.LLM.DependencyInjection;

public static class LlmServiceCollectionExtensions
{
    public static IServiceCollection AddLocalLlmServices(this IServiceCollection services)
    {
        services.AddSingleton<LlmModelManager>();
        services.AddSingleton<ITextProcessor, LlmTextProcessor>();
        return services;
    }
}
