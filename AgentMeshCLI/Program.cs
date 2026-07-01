using AgentMesh.Application;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Infrastructure.AgentMemory.Configuration;
using AgentMesh.Infrastructure.AgentMemory.Services;
using AgentMesh.Infrastructure.DocumentsCache;
using AgentMesh.Infrastructure.DocumentsCache.Configuration;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Infrastructure.SemanticSearch;
using AgentMesh.Infrastructure.SemanticSearch.Configuration;
using AgentMesh.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.Workflows;

namespace AgentMesh
{
    internal class Program
    {
        static async Task Main(string[] args) // Add async keyword
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

            services.AddSingleton<IKnowledgeBaseService, QMDKnowledgeBaseService>();
            services.AddSingleton<KnowledgeBaseExecutor>();
            services.AddSingleton<IKnowledgeBaseSearchExecutor>(sp => sp.GetRequiredService<KnowledgeBaseExecutor>());
            services.AddSingleton<IKnowledgeBaseGetDocsExecutor>(sp => sp.GetRequiredService<KnowledgeBaseExecutor>());

            // Agent Memory Service configuration
            var agentMemoryConfig = new AgentMemoryServiceConfiguration();
            configuration.GetSection(AgentMemoryServiceConfiguration.SectionName).Bind(agentMemoryConfig);
            services.AddSingleton(agentMemoryConfig);
            services.AddHttpClient<IAgentMemoryService, Mem0AgentMemoryService>();

            // Register Agent Memory Executor - single implementation for both interfaces
            services.AddSingleton<AgentMemoryExecutor>();
            services.AddSingleton<IAgentMemoryRetriever>(sp => sp.GetRequiredService<AgentMemoryExecutor>());
            services.AddSingleton<IAgentMemorySaver>(sp => sp.GetRequiredService<AgentMemoryExecutor>());

            // Documents Cache Service and Executor
            var documentsCacheConfig = new DocumentsCacheServiceConfiguration();
            configuration.GetSection(DocumentsCacheServiceConfiguration.SectionName).Bind(documentsCacheConfig);
            services.AddSingleton(documentsCacheConfig);
            services.AddSingleton<IDocumentsCacheService, DummyDocumentsCacheService>();
            services.AddSingleton<IDocumentsCacheExecutor, DocumentsCacheExecutor>();
            services.AddSingleton<IGetAllCachedSearchesExecutor, GetAllCachedSearchesExecutor>();
            services.AddSingleton<IAgentMemoryCacheSaveExecutor, AgentMemoryCacheSaveExecutor>();
            services.AddSingleton<IKnowledgeBaseCacheSaveExecutor, KnowledgeBaseCacheSaveExecutor>();

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

            // Business Requirements Creator agent config and client
            services
                .AddOptions<BusinessRequirementsCreatorAgentConfiguration>()
                .Bind(configuration.GetSection(BusinessRequirementsCreatorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<BusinessRequirementsCreatorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(BusinessRequirementsCreatorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<BusinessRequirementsCreatorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IBusinessRequirementsCreatorAgent, BusinessRequirementsCreatorAgent>();

            // Business Advisor agent config and client
            services
                .AddOptions<BusinessAdvisorAgentConfiguration>()
                .Bind(configuration.GetSection(BusinessAdvisorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<BusinessAdvisorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(BusinessAdvisorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<BusinessAdvisorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IBusinessAdvisorAgent, BusinessAdvisorAgent>();

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

            // Code Smell Checker
            services.AddSingleton<ICodeSmellDetector, CodeSmellDetector>();

            // Results Presenter agent config and client
            services
                .AddOptions<ResultsPresenterAgentConfiguration>()
                .Bind(configuration.GetSection(ResultsPresenterAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ResultsPresenterAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(ResultsPresenterAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<ResultsPresenterAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IResultsPresenterAgent, ResultsPresenterAgent>();

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

            // CodeStaticAnalyzer agent config and client
            services
                .AddOptions<CodeStaticAnalyzerConfiguration>()
                .Bind(configuration.GetSection(CodeStaticAnalyzerConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeStaticAnalyzerConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(CodeStaticAnalyzerConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<CodeStaticAnalyzerConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<ICodeStaticAnalyzerAgent, CodeStaticAnalyzer>();

            // ContextAnalyzer agent config and client
            services
                .AddOptions<ContextAnalyzerAgentConfiguration>()
                .Bind(configuration.GetSection(ContextAnalyzerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ContextAnalyzerAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(ContextAnalyzerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<ContextAnalyzerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IContextAnalyzerAgent, ContextAnalyzerAgent>();

            // IntentExtractor agent config and client
            services
                .AddOptions<IntentExtractorAgentConfiguration>()
                .Bind(configuration.GetSection(IntentExtractorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<IntentExtractorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(IntentExtractorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<IntentExtractorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IIntentExtractorAgent, IntentExtractorAgent>();

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

            // SearchQueriesConciliator agent config and client
            services
                .AddOptions<SearchQueriesConciliatorAgentConfiguration>()
                .Bind(configuration.GetSection(SearchQueriesConciliatorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<SearchQueriesConciliatorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(SearchQueriesConciliatorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<SearchQueriesConciliatorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<ISearchQueriesConciliatorAgent, SearchQueriesConciliatorAgent>();

            services.AddSingleton<IJSSandboxExecutor, JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

            services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();

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
