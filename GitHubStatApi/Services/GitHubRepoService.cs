using GitHubStatApi.Configuration;
using GitHubStatApi.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;
using Polly;
using Polly.Wrap;
using System.Collections.Concurrent;

namespace GitHubStatApi.Services
{
    public class GitHubRepoService : IGitHubRepoService
    {
        private readonly GitHubOptions _gitHubOptions;

        private readonly GitHubClient _client;

        private readonly AsyncPolicyWrap _policyWrap;

        private readonly IPolicyFactory _retryPolicyFactory;

        private readonly IFileContentAnalyzerService _fileContentAnalyzerService;

        private static int _maxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1);

        public GitHubRepoService(
            IOptions<GitHubOptions> config,
            IPolicyFactory retryPolicyFactory,
            IFileContentAnalyzerService fileContentAnalyzerService,
            IGitHubClientFactory gitHubClientFactory)
        {
            _gitHubOptions = config.Value;

            _policyWrap = Policy.WrapAsync(retryPolicyFactory.CreateRateLimitExceededPolicy(),
                                           retryPolicyFactory.CreateCircuitBreakerPolicy(TimeSpan.FromSeconds(10), 2));

            _retryPolicyFactory = retryPolicyFactory;

            _fileContentAnalyzerService = fileContentAnalyzerService;

            _client = gitHubClientFactory.CreateClient();
        }

        public async Task<Dictionary<char, int>> GetLetterFrequenciesFromFilesWithExtensions(
            List<string> allowedFileExtensions,
            CancellationToken cancellationToken)
        {
            var contents = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetRepositoryContents("/"), cancellationToken);

            var frequenciesDictionary = _fileContentAnalyzerService.InitializeLetterFrequencyDictionary();

            await ProcessRepositoryContents(contents, frequenciesDictionary, allowedFileExtensions, cancellationToken);

            return frequenciesDictionary.OrderByDescending(kv => kv.Value)
                                        .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public virtual async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContents(string path)
        {
            return await _client.Repository.Content.GetAllContents(_gitHubOptions.Owner, _gitHubOptions.Repo, path);
        }

        public virtual async Task<byte[]> GetRepositoryFileRawContent(string path)
        {
            return await _client.Repository.Content.GetRawContent(_gitHubOptions.Owner, _gitHubOptions.Repo, path);
        }

        private async Task ProcessRepositoryContents(
           IReadOnlyList<RepositoryContent> contents,
           ConcurrentDictionary<char, int> frequencies,
           List<string> allowedFileExtensions,
           CancellationToken cancellationToken)
        {
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(contents, options, async (content, cToken) =>
            {
                if (content.Type == ContentType.Dir)
                {
                    var subContents = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetRepositoryContents(content.Path), cToken);
                    await ProcessRepositoryContents(subContents, frequencies, allowedFileExtensions, cToken);
                }
                else if (content.Type == ContentType.File && HasAllowedFileExtension(content.Name, allowedFileExtensions))
                {
                    var fileContent = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetRepositoryFileRawContent(content.Path), cToken);
                    if (fileContent?.Length > 0)
                    {
                        var contentAsString = System.Text.Encoding.UTF8.GetString(fileContent) ?? string.Empty;
                        _fileContentAnalyzerService.UpdateLetterFrequencyDictionary(frequencies, contentAsString);
                    }
                }
            });
        }

        private static bool HasAllowedFileExtension(string fileName, List<string> allowedFileExtensions)
        {
            return allowedFileExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
