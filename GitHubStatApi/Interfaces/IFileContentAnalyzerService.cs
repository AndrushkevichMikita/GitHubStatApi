namespace GitHubStatApi.Interfaces
{
    public interface IFileContentAnalyzerService
    {
        Dictionary<char, int> CalculateLetterFrequencies(List<string> filesContent);

        Task<Dictionary<char, int>> CalculateLetterFrequenciesAsync(IAsyncEnumerable<string> filesContent);
    }
}
