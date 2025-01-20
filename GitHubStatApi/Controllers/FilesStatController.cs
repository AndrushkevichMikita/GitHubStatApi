using GitHubStatApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GitHubStatApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesStatController : ControllerBase
    {
        private readonly IGitHubService _gitHubService;

        private readonly IFileContentAnalyzerService _fileProcessor;

        public FilesStatController(IGitHubService gitHubService, IFileContentAnalyzerService fileProcessor)
        {
            _gitHubService = gitHubService;
            _fileProcessor = fileProcessor;
        }

        [HttpPost]
        public async Task<IActionResult> GetLetterFrequencies(List<string> allowedFileExtensions)
        {
            if (allowedFileExtensions?.Any() ?? false)
            {
                allowedFileExtensions = new List<string>() { ".js", ".ts" };
            }

            var filesContent = _gitHubService.StreamRepositoryContent(allowedFileExtensions, HttpContext.RequestAborted);

            var frequencies = await _fileProcessor.CalculateLetterFrequenciesAsync(filesContent);

            return Ok(frequencies);
        }
    }
}
