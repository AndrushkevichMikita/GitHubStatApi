using System.Runtime.CompilerServices;

namespace GitHubStatApi.Interfaces
{
    public interface IGitHubService
    {
        IAsyncEnumerable<string> StreamRepositoryContent(
            IEnumerable<string> allowedFileExtensions,
            [EnumeratorCancellation] CancellationToken cancellationToken);

        Task<List<string>> GetContentOfTSAndJSFiles(List<string> allowedFileExtensions, CancellationToken cancellationToken);
    }
}
