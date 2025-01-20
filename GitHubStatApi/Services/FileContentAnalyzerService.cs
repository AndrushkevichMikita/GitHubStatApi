using GitHubStatApi.Interfaces;
using System.Collections.Concurrent;

namespace GitHubStatApi.Services
{
    public class FileContentAnalyzerService : IFileContentAnalyzerService
    {
        public Dictionary<char, int> CalculateLetterFrequencies(List<string> filesContent)
        {
            var frequencies = new ConcurrentDictionary<char, int>(
                Enumerable.Range('a', 26)
                          .Select(x => new KeyValuePair<char, int>((char)x, 0)));

            Parallel.ForEach(filesContent, (fileContent, _) =>
            {
                foreach (char c in fileContent.ToLower())
                {
                    if (frequencies.ContainsKey(c))
                    {
                        frequencies.AddOrUpdate(c, 1, (key, oldValue) => oldValue + 1);
                    }
                }
            });

            return frequencies.OrderByDescending(kv => kv.Value)
                              .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
