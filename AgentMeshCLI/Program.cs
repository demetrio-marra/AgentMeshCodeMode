using AgentMesh.Application.Configuration;
using AgentMesh.Application.Services;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Infrastructure.QMD;
using AgentMesh.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Utils;
using AgentMesh.Infrastructure.Mem0;
using AgentMesh.Infrastructure.QMD.Services;
using AgentMesh.Application.Services.Workflows;
using AgentMesh.Application.Services.Workflows.Steps;
using AgentMesh.Models.Workflows;
using System.Reflection;

namespace AgentMesh
{
    internal class Program
    {
        static async Task Main()
        {
            // Build configuration
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                              ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup Dependency Injection
            var services = new ServiceCollection();

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            services.AddSingleton<IEWParameterSerializer, DefaultEWParameterSerializer>();

            foreach (var ewParameterType in DiscoverEWParameterImplementations())
            {
                services.AddSingleton(ewParameterType);
                services.AddSingleton(typeof(IEWParameter), sp => (IEWParameter)sp.GetRequiredService(ewParameterType));
            }

            services.AddSingleton<EWParametersProvider>();
            services.AddSingleton<IEWStepSelector, EWStepSelector>();
            services.AddTransient<EWPipeline>();


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

            // Load LLMs configuration
            services
                .AddOptions<LLMsConfiguration>()
                .Bind(configuration.GetSection(LLMsConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<LLMsConfiguration>>().Value);

            // FunctionalAnalyst agent config and client
            services
                .AddOptions<FunctionalAnalystAgentConfiguration>()
                .Bind(configuration.GetSection(FunctionalAnalystAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<FunctionalAnalystAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(FunctionalAnalystAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<FunctionalAnalystAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<FunctionalAnalystAgent>();

            // DomainExpert agent config and client
            services
                .AddOptions<DomainExpertAgentConfiguration>()
                .Bind(configuration.GetSection(DomainExpertAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<DomainExpertAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(DomainExpertAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<DomainExpertAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<DomainExpertAgent>();

            // TechnicalAnalyst agent config and client
            services
                .AddOptions<TechnicalAnalystAgentConfiguration>()
                .Bind(configuration.GetSection(TechnicalAnalystAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<TechnicalAnalystAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(TechnicalAnalystAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<TechnicalAnalystAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<TechnicalAnalystAgent>();

            // Documentation agent config and client
            services
                .AddOptions<DocumentationAgentConfiguration>()
                .Bind(configuration.GetSection(DocumentationAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<DocumentationAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(DocumentationAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<DocumentationAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<DocumentationAgent>();

            // Coder agent config and client
            services
                .AddOptions<CoderAgentConfiguration>()
                .Bind(configuration.GetSection(CoderAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CoderAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(CoderAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<CoderAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<CoderAgent>();

            // CodeFixer agent config and client
            services
                .AddOptions<CodeFixerAgentConfiguration>()
                .Bind(configuration.GetSection(CodeFixerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeFixerAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(CodeFixerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<CodeFixerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<CodeFixerAgent>();

            // CodeExecutionFailuresDetector agent config and client
            services
                .AddOptions<CodeExecutionFailuresDetectorAgentConfiguration>()
                .Bind(configuration.GetSection(CodeExecutionFailuresDetectorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeExecutionFailuresDetectorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<CodeExecutionFailuresDetectorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<JavascriptCodeExecutionFailuresDetectorAgent>();

            // RequestCanonicalization agent config and client
            services
                .AddOptions<RequestCanonicalizationAgentConfiguration>()
                .Bind(configuration.GetSection(RequestCanonicalizationAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<RequestCanonicalizationAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(RequestCanonicalizationAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<RequestCanonicalizationAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<RequestCanonicalizationAgent>();

            // PersonalAssistant agent config and client
            services
                .AddOptions<PersonalAssistantAgentConfiguration>()
                .Bind(configuration.GetSection(PersonalAssistantAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<PersonalAssistantAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(PersonalAssistantAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<PersonalAssistantAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<PersonalAssistantAgent>();

            // RelevantFactsEvaluator agent config and client
            services
                .AddOptions<RelevantFactsEvaluatorAgentConfiguration>()
                .Bind(configuration.GetSection(RelevantFactsEvaluatorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<RelevantFactsEvaluatorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(RelevantFactsEvaluatorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<RelevantFactsEvaluatorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<RelevantFactsEvaluatorAgent>();

            // RequestAnalyzer agent config and client
            services
                .AddOptions<RequestAnalyzerAgentConfiguration>()
                .Bind(configuration.GetSection(RequestAnalyzerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<RequestAnalyzerAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(RequestAnalyzerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<RequestAnalyzerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<RequestAnalyzerAgent>();

            // KnowledgeBaseQueryExpander agent config and client
            services
                .AddOptions<KnowledgeBaseQueryExpanderAgentConfiguration>()
                .Bind(configuration.GetSection(KnowledgeBaseQueryExpanderAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<KnowledgeBaseQueryExpanderAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(KnowledgeBaseQueryExpanderAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<KnowledgeBaseQueryExpanderAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<KnowledgeBaseQueryExpanderAgent>();

            // AgentMemoryQueryExpander agent config and client
            services
                .AddOptions<AgentMemoryQueryExpanderAgentConfiguration>()
                .Bind(configuration.GetSection(AgentMemoryQueryExpanderAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<AgentMemoryQueryExpanderAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(AgentMemoryQueryExpanderAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<AgentMemoryQueryExpanderAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<AgentMemoryQueryExpanderAgent>();

            // Reranker agent config and client
            services
                .AddOptions<RerankerAgentConfiguration>()
                .Bind(configuration.GetSection(RerankerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<RerankerAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(RerankerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<RerankerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

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

            services.AddKeyedSingleton<IOpenAIClient>(ConversationSummarizerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<ConversationSummarizerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<ConversationSummarizerAgent>();

            // CodeModeWorkflow configuration
            services
                .AddOptions<CodeModeWorkflowConfiguration>()
                .Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

            services.AddSingleton<JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

            services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();

            services.AddSingleton<KnowledgeBaseDocumentsExtractorWorkflowStep>();
            services.AddSingleton<DomainsKnowledgeBaseDocumentsExtractorWorkflowStep>();
            services.AddSingleton<APIKnowledgeBaseDocumentsExtractorWorkflowStep>();
            services.AddSingleton<RequestCanonicalizationWorkflowStep>();
            services.AddSingleton<AgentMemoryServiceWorkflowStep>();
            services.AddSingleton<AgentMemoryQueryExpanderWorkflowStep>();
            services.AddSingleton<KnowledgeBaseServiceSearchWorkflowStep>();
            services.AddSingleton<DomainsKnowledgeBaseServiceSearchWorkflowStep>();
            services.AddSingleton<APIsKnowledgeBaseServiceSearchWorkflowStep>();
            services.AddSingleton<FunctionalAnalystWorkflowStep>();
            services.AddSingleton<TechnicalAnalystWorkflowStep>();
            services.AddSingleton<CoderWorkflowStep>();
            services.AddSingleton<CodeFixerForRuntimeErrorsWorkflowStep>();
            services.AddSingleton<JSSandboxWorkflowStep>();
            services.AddSingleton<CodeExecutionFailuresDetectorWorkflowStep>();
            services.AddSingleton<DocumentationWorkflowStep>();
            services.AddSingleton<DomainExpertWorkflowStep>();
            services.AddSingleton<RequestAnalyzerWorkflowStep>();
            services.AddSingleton<KnowledgeBaseQueryExpanderWorkflowStep>();
            services.AddSingleton<RerankerWorkflowStep>();

            services.AddSingleton<IWorkflow, CodeModeWorkflow>();
            services.AddSingleton<UserConsoleInputService>();

            services
               .AddOptions<UserConfiguration>()
               .Bind(configuration.GetSection(UserConfiguration.SectionName))
               .Services
               .AddSingleton(sp => sp.GetRequiredService<IOptions<UserConfiguration>>().Value);

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create and run the service
            var userConsoleInputService = serviceProvider.GetRequiredService<UserConsoleInputService>();
            await userConsoleInputService.Run();
        }

        private static IEnumerable<Type> DiscoverEWParameterImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteEWParameter)
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

        private static LLMConfiguration ResolveLLMConfiguration(string llmKey, LLMsConfiguration llmsConfiguration)
        {
            if (!llmsConfiguration.TryGetValue(llmKey, out var llmConfig))
            {
                throw new InvalidOperationException($"LLM configuration not found for key: {llmKey}");
            }

            return llmConfig;
        }
    }
}
