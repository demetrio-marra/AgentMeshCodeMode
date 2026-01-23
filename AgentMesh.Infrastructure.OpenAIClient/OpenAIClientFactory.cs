using AgentMesh.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentMesh.Infrastructure.OpenAIClient
{
    public class OpenAIClientFactory : IOpenAIClientFactory
    {
        private readonly OpenAIClientFactoryConfiguration _configuration;

        public OpenAIClientFactory(OpenAIClientFactoryConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IOpenAIClient CreateOpenAIClient(string model, string provider, string temperature, string systemPrompt)
        {
            var apikey = _configuration[provider].ApiKey;
            var endpoint = _configuration[provider].Endpoint;
            return new OpenAIClient(model, apikey, endpoint, temperature, systemPrompt);
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void AddOpenAIClientFactory(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<OpenAIClientFactoryConfiguration>()
                    .Bind(configuration.GetSection(OpenAIClientFactoryConfiguration.SectionName));
            services.AddSingleton<IOpenAIClientFactory, OpenAIClientFactory>();
        }
    }
}
