using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Models.AgentMemory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class AgentMemoryExecutor(IAgentMemoryService agentMemoryService,
        ILogger<AgentMemoryExecutor> logger,
        UserConfiguration userConfiguration) 
    {
        private readonly ILogger<AgentMemoryExecutor> _logger = logger;
        private readonly IAgentMemoryService _agentMemoryService = agentMemoryService;
        private readonly UserConfiguration _userConfiguration = userConfiguration;

        public async Task SaveAsync(AgentMemorySaverConversationInput input)
        {
            _logger.LogDebug("Executing AgentMemorySaver - SaveConversationHistoryAsync.");
            _logger.LogDebug("AgentMemorySaver - SaveConversationHistoryAsync Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            await _agentMemoryService.AddConversationHistory(_userConfiguration.AgentId, input.ConversationHistory);

            stopwatch.Stop();

            _logger.LogDebug("AgentMemorySaver - SaveConversationHistoryAsync completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
        }

        public async Task<AgentMemoryRetrieverOutput> GetAsync(AgentMemoryRetrieverInput input)
        {
            _logger.LogDebug("Executing AgentMemoryRetriever - ExecuteAsync.");
            _logger.LogDebug("AgentMemoryRetriever - ExecuteAsync Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var ret = await _agentMemoryService.Query(_userConfiguration.AgentId,
                input.Query);

            stopwatch.Stop();

            var output = new AgentMemoryRetrieverOutput
            {
                Items = [.. ret.Select(r => new AgentMemoryQueryResultItem
                {
                    Confidence = r.Confidence,
                    Memory = r.Memory,
                })]
            };

            _logger.LogDebug("AgentMemoryRetriever - ExecuteAsync completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("AgentMemoryRetriever - ExecuteAsync Output: {Output}", System.Text.Json.JsonSerializer.Serialize(output));

            return output;
        }
    }
}
