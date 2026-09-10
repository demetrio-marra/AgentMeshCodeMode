using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Application.Utils;
using AgentMesh.Authentication;
using AgentMesh.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.Cohere;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.LightRag.Services;
using AgentMesh.Infrastructure.LightRag.Configuration;
using AgentMesh.Infrastructure.Mem0;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace AgentMesh
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var isInteractive = args.Any(a => string.Equals(a, "--interactive", StringComparison.OrdinalIgnoreCase));

            if (isInteractive)
            {
                var builder = Host.CreateApplicationBuilder(args);
                ConfigureConfiguration(builder.Configuration, builder.Environment.EnvironmentName);
                RegisterCommonServices(builder.Services, builder.Configuration);

                builder.Services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();
                builder.Services.AddHostedService<UserConsoleInputService>();

                var host = builder.Build();
                await host.RunAsync();
                return;
            }

            var webBuilder = WebApplication.CreateBuilder(args);
            ConfigureConfiguration(webBuilder.Configuration, webBuilder.Environment.EnvironmentName);
            RegisterCommonServices(webBuilder.Services, webBuilder.Configuration);

            var apiKeyConfiguration = webBuilder.Configuration
                .GetSection(ApiKeyAuthenticationConfiguration.SectionName)
                .Get<ApiKeyAuthenticationConfiguration>() ?? new ApiKeyAuthenticationConfiguration();

            if (string.IsNullOrWhiteSpace(apiKeyConfiguration.ApiKey))
            {
                throw new InvalidOperationException($"Missing API key configuration: '{ApiKeyAuthenticationConfiguration.SectionName}:ApiKey'.");
            }

            webBuilder.Services
                .AddOptions<ApiKeyAuthenticationConfiguration>()
                .Bind(webBuilder.Configuration.GetSection(ApiKeyAuthenticationConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ApiKeyAuthenticationConfiguration>>().Value);

            webBuilder.Services.AddSingleton<IWorkflowProgressNotifier, DummyWorkflowProgressNotifier>();
            webBuilder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.SchemeName, _ => { });
            webBuilder.Services.AddAuthorization();
            webBuilder.Services.AddControllers();
            webBuilder.Services.AddEndpointsApiExplorer();
            webBuilder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.SchemeName, new OpenApiSecurityScheme
                {
                    Name = apiKeyConfiguration.HeaderName,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Provide the API key to access protected endpoints."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = ApiKeyAuthenticationDefaults.SchemeName
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = webBuilder.Build();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.UseSwagger();
            app.UseSwaggerUI();

            await app.RunAsync();
        }

        private static void ConfigureConfiguration(ConfigurationManager configuration, string environmentName)
        {
            configuration.Sources.Clear();
            configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

        private static void RegisterCommonServices(IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = new AppSettingsConfigurationDto();
            configuration.Bind(appSettings);

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
            });

            services.AddKeyedSingleton<IEWParameterSerializer, DisplayValuesEWParameterSerializer>("DisplayParametersSerializer");
            services.AddKeyedSingleton<IEWParameterSerializer, DefaultEWParameterSerializer>("DefaultParametersSerializer");
            services.AddKeyedSingleton<IEWParameterSerializer, OmittedValueEWParameterSerializer>("OmittedValueParametersSerializer");
            services.AddSingleton<IOpenAIClientFactory, OpenAIClientFactory>();

            foreach (var ewParameterType in AssemblyDiscoveryHelper.DiscoverEWParameterImplementations())
            {
                services.AddSingleton(ewParameterType);
                services.AddSingleton(typeof(IEWParameterConfiguration), sp => (IEWParameterConfiguration)sp.GetRequiredService(ewParameterType));
            }

            foreach (var ewStepType in AssemblyDiscoveryHelper.DiscoverEWStepImplementations())
            {
                services.AddSingleton(ewStepType);
            }

            services.AddSingleton<IEnumerable<AgentFlatConfigurationRecord>>(AgentConfigurationReadHelper.ReadAgentConfigurations(appSettings, AppContext.BaseDirectory).ToArray());

            services.AddSingleton<IAgentInputSerializer, DefaultAgentInputSerializer>();

            services.AddScoped<IParameterStore, ParameterStore>();
            services.AddScoped<IChatRequestPipeline, ChatRequestPipeline>();
            services.AddScoped<ISummarizationPipeline, SummarizationPipeline>();

            var lightRagConfig = new LightRagServiceConfiguration();
            configuration.GetSection(LightRagServiceConfiguration.SectionName).Bind(lightRagConfig);
            services.AddSingleton(lightRagConfig);
            services.AddHttpClient<IKnowledgeService, LightRagKnowledgeService>();

            var agentMemoryConfig = new AgentMemoryServiceConfiguration();
            configuration.GetSection(AgentMemoryServiceConfiguration.SectionName).Bind(agentMemoryConfig);
            services.AddSingleton(agentMemoryConfig);
            services.AddHttpClient<IAgentMemoryService, Mem0AgentMemoryService>();

            var cohereRerankerConfig = new CohereV1RerankerServiceConfiguration();
            configuration.GetSection(CohereV1RerankerServiceConfiguration.SectionName).Bind(cohereRerankerConfig);
            services.AddSingleton(cohereRerankerConfig);
            services.AddHttpClient<IRerankerService, CohereV1RerankerService>();

            services.AddSingleton<AgentMemoryExecutor>();

            services
                .AddOptions<SESJSSandboxConfiguration>()
                .Bind(configuration.GetSection("SESJSSandbox"))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<SESJSSandboxConfiguration>>().Value);

            services
                .AddOptions<ResilienceConfiguration>()
                .Bind(configuration.GetSection(ResilienceConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ResilienceConfiguration>>().Value);

            services
              .AddOptions<ConversationSummarizationConfiguration>()
              .Bind(configuration.GetSection(ConversationSummarizationConfiguration.SectionName))
              .Services
              .AddSingleton(sp => sp.GetRequiredService<IOptions<ConversationSummarizationConfiguration>>().Value);

            services.AddSingleton<Resilience>();

            foreach (var ewAgentType in AssemblyDiscoveryHelper.DiscoverEWAgentImplementations())
            {
                services.AddSingleton(ewAgentType);
                services.AddSingleton(typeof(IEWAgent), sp => (IEWAgent)sp.GetRequiredService(ewAgentType));
            }

            services
                .AddOptions<CodeModeWorkflowConfiguration>()
                .Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

            services.AddSingleton<JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

            services
                .AddOptions<UserConfiguration>()
                .Bind(configuration.GetSection(UserConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<UserConfiguration>>().Value);

            services.AddSingleton<ConversationContext>();
            services.AddSingleton<AppInstance>();
        }
    }
}
