using Octokit;

namespace GitHubStatApi.Interfaces
{
    public interface IGitHubClientFactory
    {
        GitHubClient CreateClient();
    }
}
