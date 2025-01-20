using GitHubStatApi.Configuration;
using GitHubStatApi.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;
using Polly;
using Polly.Wrap;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GitHubStatApi.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly GitHubOptions _gitHubOptions;

        private readonly GitHubClient _client;

        private readonly AsyncPolicyWrap _policyWrap;

        private readonly IPolicyFactory _retryPolicyFactory;

        public GitHubService(
            IOptions<GitHubOptions> config,
            IPolicyFactory retryPolicyFactory,
            IGitHubClientFactory gitHubClientFactory)
        {
            _gitHubOptions = config.Value;

            _policyWrap = Policy.WrapAsync(retryPolicyFactory.CreateRateLimitExceededPolicy(),
                                           retryPolicyFactory.CreateCircuitBreakerPolicy(TimeSpan.FromSeconds(10), 2));

            _retryPolicyFactory = retryPolicyFactory;

            _client = gitHubClientFactory.CreateClient();
        }

        public async Task<List<string>> GetContentOfTSAndJSFiles(List<string> allowedFileExtensions, CancellationToken cancellationToken)
        {
            var contents = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetAllContents("/"), cancellationToken);

            var files = new ConcurrentBag<string>();
            await Process(contents, files, allowedFileExtensions, cancellationToken);

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

        public async IAsyncEnumerable<string> StreamRepositoryContent(
            IEnumerable<string> allowedFileExtensions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var fileContent in StreamDirectoryContent("/", allowedFileExtensions.ToList(), cancellationToken))
            {
                yield return fileContent;
            }
        }

        private async IAsyncEnumerable<string> StreamDirectoryContent(
            string path,
            List<string> allowedFileExtensions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var contents = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetAllContents(path), cancellationToken);

            foreach (var content in contents)
            {
                if (content.Type == ContentType.Dir)
                {
                    // Recursively stream contents of subdirectories
                    await foreach (var subContent in StreamDirectoryContent(content.Path, allowedFileExtensions, cancellationToken))
                    {
                        yield return subContent;
                    }
                }
                else if (content.Type == ContentType.File && IsAllowedExtension(content.Name, allowedFileExtensions))
                {
                    // Stream file content
                    var fileContent = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetFileRawContent(content.Path), cancellationToken);
                    if (fileContent?.Length > 0)
                        yield return System.Text.Encoding.UTF8.GetString(fileContent);
                }
            }
        }

        private async Task Process(
            IReadOnlyList<RepositoryContent> contents,
            ConcurrentBag<string> files,
            List<string> allowedFileExtensions,
            CancellationToken cancellationToken)
        {
            await Parallel.ForEachAsync(contents, cancellationToken, async (content, cToken) =>
            {
                if (content.Type == ContentType.Dir)
                {
                    var subContents = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetAllContents(content.Path), cToken);
                    await Process(subContents, files, allowedFileExtensions, cToken);
                }
                else if (content.Type == ContentType.File && IsAllowedExtension(content.Name, allowedFileExtensions))
                {
                    var fileContent = await _retryPolicyFactory.ExecuteWithPolicyWrap(_policyWrap, () => GetFileRawContent(content.Path), cToken);
                    if (fileContent?.Length > 0)
                        files.Add(System.Text.Encoding.UTF8.GetString(fileContent));
                }
            });
        }

        private bool IsAllowedExtension(string fileName, List<string> allowedFileExtensions)
        {
            return allowedFileExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
