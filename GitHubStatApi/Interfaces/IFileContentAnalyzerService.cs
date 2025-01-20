using System.Collections.Concurrent;

namespace GitHubStatApi.Interfaces
{
    public interface IFileContentAnalyzerService
    {
        Dictionary<char, int> CalculateLetterFrequencies(List<string> filesContent);

        ConcurrentDictionary<char, int> CreateLetterFrequencies();

        void CalculateLetterFrequencies(ConcurrentDictionary<char, int> resultDictionary, string fileContent);

        Task<Dictionary<char, int>> CalculateLetterFrequenciesAsync(IAsyncEnumerable<string> filesContent);
    }
}
