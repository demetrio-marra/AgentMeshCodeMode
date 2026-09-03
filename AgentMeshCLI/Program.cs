using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Application.Utils;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AgentMesh
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostApplicationBuilder(args);

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

            foreach (var ewParameterType in DiscoverEWParameterImplementations())
            {
                services.AddSingleton(ewParameterType);
                services.AddSingleton(typeof(IEWParameterConfiguration), sp => (IEWParameterConfiguration)sp.GetRequiredService(ewParameterType));
            }

            foreach (var ewStepType in DiscoverEWStepImplementations())
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

            services.AddSingleton<FunctionalAnalystAgent>();
            services.AddSingleton<DomainExpertAgent>();
            services.AddSingleton<TechnicalAnalystAgent>();
            services.AddSingleton<DocumentationAgent>();
            services.AddSingleton<CoderAgent>();
            services.AddSingleton<PersonalAssistantAgent>();
            services.AddSingleton<RelevantFactsEvaluatorAgent>();
            services.AddSingleton<RequestAnalyzerAgent>();
            services.AddSingleton<CanonicalizerAgent>();
            services.AddSingleton<KnowledgeQueryBuilderForCoderAgent>();
            services.AddSingleton<AgentMemoryQueryExpanderAgent>();
            services.AddSingleton<KnowledgeRerankerAgent>();
            services.AddSingleton<KnowledgeForCoderRerankerAgent>();
            services.AddSingleton<ConversationSummarizerAgent>();

            // CodeModeWorkflow configuration
            services
                .AddOptions<CodeModeWorkflowConfiguration>()
                .Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

            services.AddSingleton<JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

            #endregion

            services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();
            services.AddSingleton<ConversationContext>();

            services
               .AddOptions<UserConfiguration>()
               .Bind(configuration.GetSection(UserConfiguration.SectionName))
               .Services
               .AddSingleton(sp => sp.GetRequiredService<IOptions<UserConfiguration>>().Value);

            services.AddSingleton<AppInstance>();

            services.AddHostedService<UserConsoleInputService>();

            var host = builder.Build();
            await host.RunAsync();
        }

    
        private static IEnumerable<Type> DiscoverEWParameterImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsEWParameterConfiguration)
                .Distinct();
        }

        private static IEnumerable<Type> DiscoverEWStepImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteEWStep)
                .Distinct();
        }

        private static bool IsEWParameterConfiguration(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IEWParameterConfiguration).IsAssignableFrom(type);
        }

        private static bool IsConcreteEWStep(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IEWStep).IsAssignableFrom(type);
        }

        private static IEnumerable<Assembly> GetAllAssemblies()
        {
            var discoveredAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!discoveredAssemblies.ContainsKey(assembly.FullName ?? assembly.GetName().Name ?? string.Empty))
                {
                    discoveredAssemblies[assembly.FullName ?? assembly.GetName().Name ?? string.Empty] = assembly;
                    queue.Enqueue(assembly);
                }
            }

            while (queue.Count > 0)
            {
                var assembly = queue.Dequeue();
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (discoveredAssemblies.ContainsKey(reference.FullName))
                    {
                        continue;
                    }

                    try
                    {
                        var loadedAssembly = Assembly.Load(reference);
                        discoveredAssemblies[reference.FullName] = loadedAssembly;
                        queue.Enqueue(loadedAssembly);
                    }
                    catch
                    {
                        // Ignore assemblies that cannot be loaded.
                    }
                }
            }

            return discoveredAssemblies.Values;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
            catch
            {
                return [];
            }
        }

        private static string ResolveConfigText(string currentValue, string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var fullPath = Path.IsPathRooted(filePath)
                    ? filePath
                    : Path.Combine(AppContext.BaseDirectory, filePath);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {fullPath}");
                }

                return File.ReadAllText(fullPath);
            }

            return currentValue;
        }
    }
}
