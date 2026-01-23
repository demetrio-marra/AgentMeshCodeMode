using AgentMesh.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentMesh.Infrastructure.OpenAIClient
{
    public static class OpenAIClientServiceCollectionExtensions
    {
        public static IServiceCollection AddInferenceProviders(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<OpenAIClientFactoryConfiguration>()
                .Bind(configuration.GetSection(OpenAIClientFactoryConfiguration.SectionName));

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<OpenAIClientFactoryConfiguration>>().Value);
            services.AddSingleton<IOpenAIClientFactory, OpenAIClientFactory>();

            return services;
        }
    }
}
