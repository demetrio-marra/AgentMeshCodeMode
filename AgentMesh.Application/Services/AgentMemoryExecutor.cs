using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class AgentMemoryExecutor : IAgentMemoryExecutor
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


        public async Task AddInteractionAsync(AgentMemoryExecutorAddInteractionInput input)
        {
            _logger.LogDebug("Executing AgentMemoryExecutor - AddInteractionAsync.");
            _logger.LogDebug("AgentMemoryExecutor - AddInteractionAsync Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            await _agentMemoryService.AddChatInteraction(_userConfiguration.AgentId,
                input.MessageByUser,
                input.ResponseByAssistant);

            stopwatch.Stop();

            _logger.LogDebug("AgentMemoryExecutor - AddInteractionAsync completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
        }

        public async Task<AgentMemoryExecutorOutput> SearchMemoryAsync(AgentMemoryExecutorSearchMemoryInput input)
        {
            _logger.LogDebug("Executing AgentMemoryExecutor - SearchMemoryAsync.");
            _logger.LogDebug("AgentMemoryExecutor - SearchMemoryAsync Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var ret = await _agentMemoryService.Query(_userConfiguration.AgentId,
                input.Query);

            stopwatch.Stop();

            var output = new AgentMemoryExecutorOutput
            {
                Items = ret.Select(r => new AgentMemoryItem
                {
                    Confidence = r.Confidence,
                    Memory = r.Memory,
                }).ToList()
            };

            _logger.LogDebug("AgentMemoryExecutor - SearchMemoryAsync completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("AgentMemoryExecutor - SearchMemoryAsync Output: {Output}", System.Text.Json.JsonSerializer.Serialize(output));

            return output;
        }
    }
}
