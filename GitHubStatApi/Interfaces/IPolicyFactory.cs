using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace GitHubStatApi.Interfaces
{
    public interface IPolicyFactory
    {
        Task<T> ExecuteWithPolicyWrap<T>(AsyncPolicyWrap policyWrap, Func<Task<T>> action, CancellationToken cancellationToken);

        AsyncRetryPolicy CreateRateLimitExceededPolicy();

        AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(TimeSpan durationOfBreak, int exceptionsAllowedBeforeBreaking);
    }
}
