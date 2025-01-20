using GitHubStatApi.Configuration;
using GitHubStatApi.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;
using Polly;
using Polly.Retry;
using System.Collections.Concurrent;

namespace GitHubStatApi.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly GitHubOptions _gitHubOptions;

        private readonly GitHubClient _client;

        private readonly AsyncRetryPolicy _retryPolicy;

        public GitHubService(IOptions<GitHubOptions> config)
        {
            _gitHubOptions = config.Value;

            _client ??= new GitHubClient(new ProductHeaderValue(_gitHubOptions.ProductInfoName));

            if (!string.IsNullOrWhiteSpace(_gitHubOptions.ProductGitHubAccessToken))
            {
                // Authentication with a Personal Access Token, this increases the limit of api requests per hour
                _client.Credentials = new Credentials(_gitHubOptions.ProductGitHubAccessToken);
            }

            _retryPolicy = Policy.Handle<ApiException>(ex => ex is RateLimitExceededException || ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                                 .WaitAndRetryAsync(retryCount: 1,
                                                    sleepDurationProvider: (attempt, context) =>
                                                    {
                                                        double delay = 1;

                                                        if (context.TryGetValue("resetTime", out var resetTime))
                                                            delay = ((DateTimeOffset)resetTime - DateTimeOffset.UtcNow).TotalSeconds;

                                                        return TimeSpan.FromSeconds(Math.Max(delay, 1)); // at least 1 second
                                                    },
                                                    onRetry: async (exception, timeSpan, retryAttempt, context) =>
                                                    {
                                                        Console.WriteLine($"Retry {retryAttempt} after {timeSpan.TotalSeconds}s due to {exception.Message}");
                                                    });
        }

        public async Task<List<string>> GetContentOfTSAndJSFiles(CancellationToken cancellationToken)
        {
            var contents = await ExecuteWithRetry(() => GetAllContents("/"), cancellationToken);

            var files = new ConcurrentBag<string>();
            await Process(contents, files, cancellationToken);

            return files.ToList();
        }

        public virtual async Task<IReadOnlyList<RepositoryContent>> GetAllContents(string path)
        {
            return await _client.Repository.Content.GetAllContents(_gitHubOptions.Owner, _gitHubOptions.Repo, path);
        }

        public virtual async Task<byte[]> GetFileRawContent(string path)
        {
            return await _client.Repository.Content.GetRawContent(_gitHubOptions.Owner, _gitHubOptions.Repo, path);
        }

        private async Task Process(
            IReadOnlyList<RepositoryContent> contents,
            ConcurrentBag<string> files,
            CancellationToken cancellationToken)
        {
            await Parallel.ForEachAsync(contents, cancellationToken, async (content, cToken) =>
            {
                if (content.Type == ContentType.Dir)
                {
                    var subContents = await ExecuteWithRetry(() => GetAllContents(content.Path), cToken);
                    await Process(subContents, files, cToken);
                }
                else if (content.Type == ContentType.File && (content.Name.EndsWith(".js") || content.Name.EndsWith(".ts")))
                {
                    var fileContent = await ExecuteWithRetry(() => GetFileRawContent(content.Path), cToken);
                    files.Add(System.Text.Encoding.UTF8.GetString(fileContent));
                }
            });
        }

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async (context, token) =>
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
    }
}
