namespace GitHubStatApi.Interfaces
{
    public interface IGitHubService
    {
        Task<List<string>> GetContentOfTSAndJSFiles(CancellationToken cancellationToken);
    }
}
