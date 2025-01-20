using System.Collections.Concurrent;

namespace GitHubStatApi.Interfaces
{
    public interface IFileContentAnalyzerService
    {
        ConcurrentDictionary<char, int> InitializeLetterFrequencyDictionary();

        void UpdateLetterFrequencyDictionary(ConcurrentDictionary<char, int> resultDictionary, string fileContent);
    }
}
