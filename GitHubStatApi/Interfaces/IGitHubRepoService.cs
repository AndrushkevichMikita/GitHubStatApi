namespace GitHubStatApi.Interfaces
{
    public interface IGitHubRepoService
    {
        Task<Dictionary<char, int>> GetLetterFrequenciesFromFilesWithExtensions(List<string> allowedFileExtensions, CancellationToken cancellationToken);
    }
}
