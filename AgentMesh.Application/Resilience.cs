using AgentMesh.Application.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net;

namespace AgentMesh.Application
{
    public class Resilience
    {
        public static Task<T> AgentRunWithRetryAsync<T>(Func<Task<T>> action, string agentName, ILogger? logger = null)
        {
            var policy = Policy
                .Handle<BadStructuredResponseException>()
                .Or<EmptyAgentResponseException>()
                .Or<Exception>(ex => ex.GetType().Name == "ClientResultException" && ex.Message.Contains("Tool choice is none, but model called a tool"))
                .Or<Exception>(ex => ex.GetType().Name == "ClientResultException" && ex.Message.Contains("Service unavailable"))
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        logger?.LogWarning(exception, "Retry {RetryCount} for agent {AgentName} due to error: {ErrorMessage}", retryCount, agentName, exception.Message);
                    });

            return policy.ExecuteAsync(action);
        }


        /// <summary>
        /// Retry policy for transient HTTP failures: network errors, client-side timeouts,
        /// and 5xx/408/429 responses. Mirrors AgentRunWithRetryAsync: exponential backoff + jitter,
        /// capped retries, structured logging on each attempt.
        /// </summary>
        public static Task<HttpResponseMessage> SendWithRetryAsync(
            Func<Task<HttpResponseMessage>> action,
            string operationName,
            ILogger? logger = null,
            int retryCount = 3)
        {
            var jitterer = new Random();

            var policy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()   // client-side timeout (HttpClient.Timeout firing)
                .Or<TimeoutException>()        // e.g. Polly TimeoutPolicy, if you chain one in front of this
                .OrResult<HttpResponseMessage>(response =>
                    (int)response.StatusCode >= 500 ||
                    response.StatusCode == HttpStatusCode.RequestTimeout ||
                    response.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: (attempt, outcome, context) =>
                    {
                        // Honor Retry-After when the server sends one (typical on 429/503)
                        var retryAfter = outcome.Result?.Headers.RetryAfter?.Delta;
                        if (retryAfter.HasValue)
                            return retryAfter.Value;

                        var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s...
                        var jitter = TimeSpan.FromMilliseconds(jitterer.Next(0, 500));
                        return backoff + jitter;
                    },
                    onRetryAsync: (outcome, timeSpan, attempt, context) =>
                    {
                        var reason = outcome.Exception?.Message
                            ?? $"HTTP {(int)outcome.Result.StatusCode} {outcome.Result.StatusCode}";

                        logger?.LogWarning(
                            outcome.Exception,
                            "Retry {RetryCount} for {OperationName} after {DelaySeconds:F1}s due to: {Reason}",
                            attempt, operationName, timeSpan.TotalSeconds, reason);

                        return Task.CompletedTask;
                    });

            return policy.ExecuteAsync(action);
        }
    }
}
