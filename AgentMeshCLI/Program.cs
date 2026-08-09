using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.CostsAnalysis;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Workflows.ParameterSerializers;
using AgentMesh.Application.Utils;
using AgentMesh.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.Mem0;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Infrastructure.QMD;
using AgentMesh.Infrastructure.QMD.Services;
using AgentMesh.Models.Workflows;
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

            services.AddSingleton<IEWParameterSerializer, DisplayValuesEWParameterSerializer>();

            foreach (var ewParameterType in DiscoverEWParameterImplementations())
            {
                services.AddScoped(ewParameterType);
                services.AddScoped(typeof(IEWParameter), sp => (IEWParameter)sp.GetRequiredService(ewParameterType));
            }

            foreach (var ewStepType in DiscoverEWStepImplementations())
            {
                services.AddScoped(ewStepType);
            }

            services.AddScoped<EWParametersProvider>();
            services.AddSingleton<IEnumerable<AgentFlatConfigurationRecord>>(AgentConfigurationReadHelper.ReadAgentConfigurations(appSettings, AppContext.BaseDirectory).ToArray());
            // insert here
            services.AddScoped<IEWStepSelector, EWStepSelector>();
            services.AddScoped<EWPipeline>();

            #region agents/executors region
            // Embedding configuration and service registration
            var embeddingConfiguration = new EmbeddingServiceConfiguration();
            configuration.GetSection("Embedding").Bind(embeddingConfiguration);
            services.AddSingleton(embeddingConfiguration);
            services.AddHttpClient<IEmbeddingService, EmbeddingService>();

            services.AddSingleton<IKnowledgeBaseService, QMDKnowledgeBaseService>();
            services.AddSingleton<KnowledgeBaseExecutor>();

            //// Queries cache service configuration
            //var queriesCacheServiceConfig = new QDrantQueriesCacheServiceConfiguration();
            //configuration.GetSection("QDrantQueriesCacheService").Bind(queriesCacheServiceConfig);
            //services.AddSingleton(queriesCacheServiceConfig);
            //services.AddSingleton<IQueriesCacheService, QDrantQueriesCacheService>();

            // Agent Memory Service configuration
            var agentMemoryConfig = new AgentMemoryServiceConfiguration();
            configuration.GetSection(AgentMemoryServiceConfiguration.SectionName).Bind(agentMemoryConfig);
            services.AddSingleton(agentMemoryConfig);
            services.AddHttpClient<IAgentMemoryService, Mem0AgentMemoryService>();

            // Register Agent Memory Executor - single implementation for both interfaces
            services.AddSingleton<AgentMemoryExecutor>();

            // QMD MCP server proxy configuration and HTTP client
            var qmdHttpProxyConfig = new QMDHttpProxyConfiguration();
            configuration.GetSection(QMDHttpProxyConfiguration.SectionName).Bind(qmdHttpProxyConfig);
            services.AddSingleton(qmdHttpProxyConfig);
            services.AddHttpClient<QMDHttpProxy>();

            // Configure JSSandbox options
            services
                .AddOptions<SESJSSandboxConfiguration>()
                .Bind(configuration.GetSection("SESJSSandbox"))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<SESJSSandboxConfiguration>>().Value);

            services.AddInferenceProviders(configuration);

            // Resilience configuration
            services
                .AddOptions<ResilienceConfiguration>()
                .Bind(configuration.GetSection(ResilienceConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ResilienceConfiguration>>().Value);

            services.AddSingleton<Resilience>();

            services.AddSingleton<FunctionalAnalystAgent>();
            services.AddSingleton<DomainExpertAgent>();
            services.AddSingleton<TechnicalAnalystAgent>();
            services.AddSingleton<DocumentationAgent>();
            services.AddSingleton<CoderAgent>();
            services.AddSingleton<RequestCanonicalizationAgent>();
            services.AddSingleton<PersonalAssistantAgent>();
            services.AddSingleton<RelevantFactsEvaluatorAgent>();
            services.AddSingleton<RequestAnalyzerAgent>();
            services.AddSingleton<KnowledgeBaseQueryExpanderAgent>();
            services.AddSingleton<AgentMemoryQueryExpanderAgent>();
            services.AddSingleton<RerankerAgent>();

            // conversation summarizer agent config and client
            services
                .AddOptions<ConversationSummarizerAgentConfiguration>()
                .Bind(configuration.GetSection(ConversationSummarizerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ConversationSummarizerAgentConfiguration>>().Value);

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

            services.AddHostedService<UserConsoleInputService>();

            var host = builder.Build();
            await host.RunAsync();
        }

    
        private static IEnumerable<Type> DiscoverEWParameterImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteEWParameter)
                .Distinct();
        }

        private static IEnumerable<Type> DiscoverEWStepImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteEWStep)
                .Distinct();
        }

        private static bool IsConcreteEWParameter(Type type)
        {
            if (!type.IsClass || type.IsAbstract)
            {
                return false;
            }

            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                if (currentBaseType.IsGenericType
                    && currentBaseType.GetGenericTypeDefinition() == typeof(EWParameter<>))
                {
                    return true;
                }

                currentBaseType = currentBaseType.BaseType;
            }

            return false;
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
