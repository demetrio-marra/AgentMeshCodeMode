using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class AgentMemoryExecutor : IAgentMemoryRetriever, IAgentMemorySaver
    {
        private readonly ILogger<AgentMemoryExecutor> _logger;
        private readonly IAgentMemoryService _agentMemoryService;
        private readonly UserConfiguration _userConfiguration;

        public AgentMemoryExecutor(IAgentMemoryService agentMemoryService,
            ILogger<AgentMemoryExecutor> logger,
            UserConfiguration userConfiguration)
        {
            _agentMemoryService = agentMemoryService;
            _logger = logger;
            _userConfiguration = userConfiguration;
        }


        async Task IAgentMemorySaver.ExecuteAsync(AgentMemorySaverInput input)
        {
            _logger.LogDebug("Executing AgentMemorySaver - ExecuteAsync.");
            _logger.LogDebug("AgentMemorySaver - ExecuteAsync Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            await _agentMemoryService.AddChatInteraction(_userConfiguration.AgentId,
                input.MessageByUser,
                input.ResponseByAssistant);

            stopwatch.Stop();

            _logger.LogDebug("AgentMemorySaver - ExecuteAsync completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
        }

        async Task<AgentMemoryRetrieverOutput> IAgentMemoryRetriever.ExecuteAsync(AgentMemoryRetrieverInput input)
        {
            _logger.LogDebug("Executing AgentMemoryRetriever - ExecuteAsync.");
            _logger.LogDebug("AgentMemoryRetriever - ExecuteAsync Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var ret = await _agentMemoryService.Query(_userConfiguration.AgentId,
                input.Query);

            stopwatch.Stop();

            var output = new AgentMemoryRetrieverOutput
            {
                Items = ret.Select(r => new AgentMemoryItem
                {
                    Confidence = r.Confidence,
                    Memory = r.Memory,
                }).ToList()
            };

            _logger.LogDebug("AgentMemoryRetriever - ExecuteAsync completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("AgentMemoryRetriever - ExecuteAsync Output: {Output}", System.Text.Json.JsonSerializer.Serialize(output));

            return output;
        }
    }
}
