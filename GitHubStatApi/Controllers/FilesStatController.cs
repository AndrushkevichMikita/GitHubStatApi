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

        [HttpGet]
        public async Task<IActionResult> GetLetterFrequencies()
        {
            var filesContent = await _gitHubService.GetContentOfTSAndJSFiles(HttpContext.RequestAborted);
            var frequencies = _fileProcessor.CalculateLetterFrequencies(filesContent);

            return Ok(frequencies);
        }
    }
}
