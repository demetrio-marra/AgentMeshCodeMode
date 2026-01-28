using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Models;
using AgentMesh.Services;
using AgentMesh.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            // Configure JSSandbox options
            services
                .AddOptions<SESJSSandboxConfiguration>()
                .Bind(configuration.GetSection("SESJSSandbox"))
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<SESJSSandboxConfiguration>>().Value);

            services.AddInferenceProviders(configuration);

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
                    options.ApiDocumentation = ResolveConfigText(options.ApiDocumentation, options.ApiDocumentationFile);
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
                    options.ApiDocumentation = ResolveConfigText(options.ApiDocumentation, options.ApiDocumentationFile);
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

            // Coder agent config and client
            services
                .AddOptions<CoderAgentConfiguration>()
                .Bind(configuration.GetSection(CoderAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                    options.ApiReference = ResolveConfigText(options.ApiReference, options.ApiReferenceFile);
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

            services.AddSingleton<ICodeStaticAnalyzer, CodeStaticAnalyzer>();

            // CodeFixer agent config and client
            services
                .AddOptions<TranslatorAgentConfiguration>()
                .Bind(configuration.GetSection(TranslatorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<TranslatorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(TranslatorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<TranslatorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<ITranslatorAgent, TranslatorAgent>();

            // ContextAggregator agent config and client
            services
                .AddOptions<ContextAggregatorAgentConfiguration>()
                .Bind(configuration.GetSection(ContextAggregatorAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ContextAggregatorAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(ContextAggregatorAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<ContextAggregatorAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IContextAggregatorAgent, ContextAggregatorAgent>();

            // ChatManager agent config and client
            services
                .AddOptions<ChatManagerAgentConfiguration>()
                .Bind(configuration.GetSection(ChatManagerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ChatManagerAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(ChatManagerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<ChatManagerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            // ContextManager agent config and client
            services
                .AddOptions<ContextManagerAgentConfiguration>()
                .Bind(configuration.GetSection(ContextManagerAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<ContextManagerAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(ContextManagerAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<ContextManagerAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IContextManagerAgent, ContextManagerAgent>();

            // Router agent config and client
            services
                .AddOptions<RouterAgentConfiguration>()
                .Bind(configuration.GetSection(RouterAgentConfiguration.SectionName))
                .PostConfigure(options =>
                {
                    options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
                })
                .Services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<RouterAgentConfiguration>>().Value);

            services.AddKeyedSingleton<IOpenAIClient>(RouterAgentConfiguration.AgentName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IOpenAIClientFactory>();
                var config = sp.GetRequiredService<RouterAgentConfiguration>();
                var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
                var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
                var systemPrompt = config.SystemPrompt;
                return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
            });

            services.AddSingleton<IRouterAgent, RouterAgent>();

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

            services.AddSingleton<IJSSandboxExecutor, JSSandboxExecutor>();
            services.AddSingleton<IJSSandbox, SESJSSandbox>();

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
