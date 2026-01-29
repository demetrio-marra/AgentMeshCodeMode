using AgentMesh.Models;
using Microsoft.Extensions.Logging;
using Polly;

namespace AgentMesh.Application
{
    public class Resilience
    {
        public static Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string agentName, ILogger? logger = null)
        {
            var policy = Policy
                .Handle<BadStructuredResponseException>()
                .Or<EmptyAgentResponseException>()
                .Or<Exception>(ex => ex.GetType().Name == "ClientResultException" && ex.Message.Contains("Tool choice is none, but model called a tool"))
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        logger?.LogWarning(exception, "Retry {RetryCount} for agent {AgentName} due to error: {ErrorMessage}", retryCount, agentName, exception.Message);
                    });

            return policy.ExecuteAsync(action);
        }
    }
}
