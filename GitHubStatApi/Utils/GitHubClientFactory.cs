using GitHubStatApi.Configuration;
using GitHubStatApi.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;

namespace GitHubStatApi.Utils
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly GitHubOptions _gitHubOptions;

        public GitHubClientFactory(IOptions<GitHubOptions> options)
        {
            _gitHubOptions = options.Value;
        }

        public GitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(_gitHubOptions.ProductInfoName));

            if (!string.IsNullOrWhiteSpace(_gitHubOptions.ProductGitHubAccessToken))
            {
                // Authentication with a Personal Access Token, this increases the limit of api requests per hour
                client.Credentials = new Credentials(_gitHubOptions.ProductGitHubAccessToken);
            }

            return client;
        }
    }
}
