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

            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.Sources.Clear();
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Configuration;
            var services = builder.Services;
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

            #region agents/executors region
            // LightRAG service configuration and HTTP client
            var lightRagConfig = new LightRagServiceConfiguration();
            configuration.GetSection(LightRagServiceConfiguration.SectionName).Bind(lightRagConfig);
            services.AddSingleton(lightRagConfig);
            services.AddHttpClient<IKnowledgeService, LightRagKnowledgeService>();

            // Agent Memory Service configuration
            var agentMemoryConfig = new AgentMemoryServiceConfiguration();
            configuration.GetSection(AgentMemoryServiceConfiguration.SectionName).Bind(agentMemoryConfig);
            services.AddSingleton(agentMemoryConfig);
            services.AddHttpClient<IAgentMemoryService, Mem0AgentMemoryService>();

            // Cohere reranker service configuration and HTTP client
            var cohereRerankerConfig = new CohereV1RerankerServiceConfiguration();
            configuration.GetSection(CohereV1RerankerServiceConfiguration.SectionName).Bind(cohereRerankerConfig);
            services.AddSingleton(cohereRerankerConfig);
            services.AddHttpClient<IRerankerService, CohereV1RerankerService>();

            // Register Agent Memory Executor - single implementation for both interfaces
            services.AddSingleton<AgentMemoryExecutor>();

            // Configure JSSandbox options
            services
                .AddOptions<SESJSSandboxConfiguration>()
                .Bind(configuration.GetSection("SESJSSandbox"))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<SESJSSandboxConfiguration>>().Value);

            // Resilience configuration
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

            // CodeModeWorkflow configuration
            services
                .AddOptions<CodeModeWorkflowConfiguration>()
                .Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

            services.AddSingleton<JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

            #endregion

            services
                .AddOptions<UserConfiguration>()
                .Bind(configuration.GetSection(UserConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<UserConfiguration>>().Value);

            services
                .AddOptions<ApiKeyAuthenticationConfiguration>()
                .Bind(configuration.GetSection(ApiKeyAuthenticationConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ApiKeyAuthenticationConfiguration>>().Value);

            if (isInteractive)
            {
                services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();
                services.AddHostedService<UserConsoleInputService>();
            }
            else
            {
                var apiKeyConfiguration = configuration.GetSection(ApiKeyAuthenticationConfiguration.SectionName).Get<ApiKeyAuthenticationConfiguration>() ?? new ApiKeyAuthenticationConfiguration();
                if (string.IsNullOrWhiteSpace(apiKeyConfiguration.ApiKey))
                {
                    throw new InvalidOperationException($"Missing API key configuration: '{ApiKeyAuthenticationConfiguration.SectionName}:ApiKey'.");
                }

                services.AddSingleton<IWorkflowProgressNotifier, DummyWorkflowProgressNotifier>();
                services.AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.SchemeName, _ => { });
                services.AddAuthorization();
                services.AddControllers();
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(options =>
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
            }

            services.AddSingleton<ConversationContext>();
            services.AddSingleton<AppInstance>();

            var app = builder.Build();

            if (!isInteractive)
            {
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            await app.RunAsync();
        }
    }
}
