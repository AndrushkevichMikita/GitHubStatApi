using GitHubStatApi.Interfaces;
using Octokit;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace GitHubStatApi.Utils
{
    public class PolicyFactory : IPolicyFactory
    {
        private readonly ILogger<PolicyFactory> _logger;

        public PolicyFactory(ILogger<PolicyFactory> logger)
        {
            _logger = logger;
        }

        public async Task<T> ExecuteWithPolicyWrap<T>(AsyncPolicyWrap policyWrap, Func<Task<T>> action, CancellationToken cancellationToken)
        {
            return await policyWrap.ExecuteAsync(async (context, token) =>
            {
                try
                {
                    return await action();
                }
                catch (RateLimitExceededException ex)
                {
                    // Add reset time to the Polly context
                    context["resetTime"] = ex.Reset;
                    throw;
                }
            }, new Context(), cancellationToken);
        }

        public AsyncRetryPolicy CreateRateLimitExceededPolicy()
        {
            return Policy.Handle<ApiException>()
                         .WaitAndRetryAsync(1, (attempt, context) =>
                         {
                             double delay = 1;

                             if (context.TryGetValue("resetTime", out var resetTime))
                                 delay = ((DateTimeOffset)resetTime - DateTimeOffset.UtcNow).TotalSeconds;

                             return TimeSpan.FromSeconds(Math.Max(delay, 1)); // at least 1 second
                         },
                         (exception, timeSpan, retryAttempt, context) =>
                         {
                             _logger.LogInformation($"Retry {retryAttempt} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                         });
        }

        public AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(TimeSpan durationOfBreak, int exceptionsAllowedBeforeBreaking)
        {
            return Policy.Handle<HttpRequestException>()
                         .CircuitBreakerAsync(
                            exceptionsAllowedBeforeBreaking,
                            durationOfBreak,
                            onBreak: (exception, duration) =>
                            {
                                _logger.LogInformation($"Breaking for {duration.TotalSeconds}s due to: {exception.Message}");
                            },
                            onReset: () => { },
                            onHalfOpen: () => { });
        }
    }
}
