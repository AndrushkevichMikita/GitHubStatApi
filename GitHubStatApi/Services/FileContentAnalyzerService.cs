using GitHubStatApi.Interfaces;
using System.Collections.Concurrent;

namespace GitHubStatApi.Services
{
    public class FileContentAnalyzerService : IFileContentAnalyzerService
    {
        public void UpdateLetterFrequencyDictionary(ConcurrentDictionary<char, int> resultDictionary, string fileContent)
        {
            foreach (char c in fileContent.ToLower())
            {
                if (resultDictionary.ContainsKey(c))
                {
                    resultDictionary.AddOrUpdate(c, 1, (key, oldValue) => oldValue + 1);
                }
            }
        }

        public ConcurrentDictionary<char, int> InitializeLetterFrequencyDictionary()
        {
            var frequencies = new ConcurrentDictionary<char, int>(
                 Enumerable.Range('a', 26)
                           .Select(x => new KeyValuePair<char, int>((char)x, 0)));

            return frequencies;
        }
    }
}
