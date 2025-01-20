using GitHubStatApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GitHubStatApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GitHubRepoController : ControllerBase
    {
        private readonly IGitHubRepoService _gitHubService;

        public GitHubRepoController(IGitHubRepoService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        [HttpPost]
        public async Task<IActionResult> GetLetterFrequenciesFromFilesWithExtensions(List<string> allowedFileExtensions)
        {
            if (!allowedFileExtensions?.Any() ?? true)
            {
                allowedFileExtensions = new List<string>() { ".js", ".ts" };
            }

            var frequencies = await _gitHubService.GetLetterFrequenciesFromFilesWithExtensions(allowedFileExtensions!, HttpContext.RequestAborted);
            return Ok(frequencies);
        }
    }
}
