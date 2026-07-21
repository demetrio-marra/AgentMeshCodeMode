using AgentMesh.Application;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Infrastructure.Mem0.Configuration;
using AgentMesh.Infrastructure.Mem0.Services;
using AgentMesh.Infrastructure.QDrant;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Infrastructure.QMD;
using AgentMesh.Infrastructure.QMD.Configuration;
using AgentMesh.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.Workflows;
using AgentMesh.Application.Workflows.Steps;

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


            // Embedding configuration and service registration
            var embeddingConfiguration = new EmbeddingServiceConfiguration();
            configuration.GetSection("Embedding").Bind(embeddingConfiguration);
            services.AddSingleton(embeddingConfiguration);
            services.AddHttpClient<IEmbeddingService, EmbeddingService>();

            services.AddSingleton<IKnowledgeBaseService, QMDKnowledgeBaseService>();
            services.AddSingleton<KnowledgeBaseExecutor>();
            services.AddSingleton<KnowledgeBaseSearchFastExecutor>();
            services.AddSingleton<IKnowledgeBaseSearchExecutor>(sp => sp.GetRequiredService<KnowledgeBaseExecutor>());
            services.AddSingleton<IKnowledgeBaseSearchFastExecutor>(sp => sp.GetRequiredService<KnowledgeBaseSearchFastExecutor>());
            services.AddSingleton<IKnowledgeBaseGetDocsExecutor>(sp => sp.GetRequiredService<KnowledgeBaseExecutor>());

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
            services.AddSingleton<IAgentMemoryRetrieverExecutor>(sp => sp.GetRequiredService<AgentMemoryExecutor>());
            services.AddSingleton<IAgentMemorySaverExecutor>(sp => sp.GetRequiredService<AgentMemoryExecutor>());

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

            services.AddSingleton<IFunctionalAnalystAgent, FunctionalAnalystAgent>();

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

            services.AddSingleton<IDomainExpertAgent, DomainExpertAgent>();

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

            services.AddSingleton<ITechnicalAnalystAgent, TechnicalAnalystAgent>();

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

            services.AddSingleton<IDocumentationAgent, DocumentationAgent>();

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

            services.AddSingleton<ICoderAgent, CoderAgent>();

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

            services.AddSingleton<ICodeFixerAgent, CodeFixerAgent>();

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

            services.AddSingleton<ICodeExecutionFailuresDetectorAgent, JavascriptCodeExecutionFailuresDetectorAgent>();

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

            services.AddSingleton<IRequestCanonicalizationAgent, RequestCanonicalizationAgent>();

            // RequirementsCollector agent config and client
            services
                .AddOptions<RequirementsCollectorAgentConfiguration>()
                .Bind(configuration.GetSection(RequirementsCollectorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<RequirementsCollectorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(RequirementsCollectorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<RequirementsCollectorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IRequirementsCollectorAgent, RequirementsCollectorAgent>();

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

            services.AddSingleton<IPersonalAssistantAgent, PersonalAssistantAgent>();

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

            services.AddSingleton<IRelevantFactsEvaluatorAgent, RelevantFactsEvaluatorAgent>();

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

            services.AddSingleton<IRequestAnalyzerAgent, RequestAnalyzerAgent>();

            // QueryExpander agent config and client
            services
                .AddOptions<QueryExpanderAgentConfiguration>()
                .Bind(configuration.GetSection(QueryExpanderAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<QueryExpanderAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(QueryExpanderAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<QueryExpanderAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IQueryExpanderAgent, QueryExpanderAgent>();

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

            services.AddSingleton<IConversationSummarizerAgent, ConversationSummarizerAgent>();

            // CodeModeWorkflow configuration
            services
                .AddOptions<CodeModeWorkflowConfiguration>()
                .Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

            services.AddSingleton<IJSSandboxExecutor, JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

            services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();

            services.AddSingleton<KnowledgeBaseDocumentsExtractorWorkflowStep>();
            services.AddSingleton<DomainsKnowledgeBaseDocumentsExtractorWorkflowStep>();
            services.AddSingleton<APIKnowledgeBaseDocumentsExtractorWorkflowStep>();
            services.AddSingleton<RequestCanonicalizationWorkflowStep>();
            services.AddSingleton<RequirementsCollectorWorkflowStep>();
            services.AddSingleton<AgentMemoryServiceWorkflowStep>();
            services.AddSingleton<KnowledgeBaseServiceSearchWorkflowStep>();
            services.AddSingleton<DomainsKnowledgeBaseServiceSearchWorkflowStep>();
            services.AddSingleton<APIsKnowledgeBaseServiceSearchWorkflowStep>();
            services.AddSingleton<KnowledgeBaseServiceFastSearchWorkflowStep>();
            services.AddSingleton<DomainsKnowledgeBaseServiceFastSearchWorkflowStep>();
            services.AddSingleton<APIsKnowledgeBaseServiceFastSearchWorkflowStep>();
            services.AddSingleton<FunctionalAnalystWorkflowStep>();
            services.AddSingleton<TechnicalAnalystWorkflowStep>();
            services.AddSingleton<CoderWorkflowStep>();
            services.AddSingleton<CodeFixerForRuntimeErrorsWorkflowStep>();
            services.AddSingleton<JSSandboxWorkflowStep>();
            services.AddSingleton<CodeExecutionFailuresDetectorWorkflowStep>();
            services.AddSingleton<DocumentationWorkflowStep>();
            services.AddSingleton<DomainExpertWorkflowStep>();
            services.AddSingleton<RequestAnalyzerWorkflowStep>();
            services.AddSingleton<QueryExpanderWorkflowStep>();

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
