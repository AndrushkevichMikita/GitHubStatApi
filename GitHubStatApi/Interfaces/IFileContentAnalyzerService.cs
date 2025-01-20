namespace GitHubStatApi.Interfaces
{
    public interface IFileContentAnalyzerService
    {
        Dictionary<char, int> CalculateLetterFrequencies(List<string> filesContent);
    }
}
