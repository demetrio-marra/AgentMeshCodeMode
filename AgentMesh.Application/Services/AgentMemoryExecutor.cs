using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.AgentMemory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class AgentMemoryExecutor(IAgentMemoryService agentMemoryService,
        ILogger<AgentMemoryExecutor> logger,
        UserConfiguration userConfiguration) : IAgentMemoryRetriever, IAgentMemorySaver
    {
        private readonly ILogger<AgentMemoryExecutor> _logger = logger;
        private readonly IAgentMemoryService _agentMemoryService = agentMemoryService;
        private readonly UserConfiguration _userConfiguration = userConfiguration;

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
