using GitHubStatApi.Configuration;
using GitHubStatApi.Services;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;

namespace GitHubStatApiTests.Tests
{
    public class GitHubServiceTests
    {
        private readonly Mock<IOptions<GitHubOptions>> _mockOptions;

        private readonly Mock<GitHubService> _mockService;

        private readonly GitHubOptions _gitHubOptions;

        public GitHubServiceTests()
        {
            _gitHubOptions = new GitHubOptions
            {
                Repo = "repo",
                Owner = "owner",
                ProductInfoName = "TestProduct",
                ProductGitHubAccessToken = "test-token"
            };

            _mockOptions = new Mock<IOptions<GitHubOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(_gitHubOptions);

            _mockService = new Mock<GitHubService>(_mockOptions.Object) { CallBase = true };
        }

        [Fact]
        public async Task GetContentOfTSAndJSFiles_ShouldRetry_OnApiExceededException()
        {
            // Arrange
            var allowedFileExtensions = new List<string>() { ".js", ".ts" };
            var mockFileContent = "console.log('test');";

            var apiException = new ApiException("mock error", System.Net.HttpStatusCode.ServiceUnavailable);

            var mockContent = new List<RepositoryContent>
            {
                new("test.js", "path/to/test.js", "", 0,  ContentType.File, "path/to/test.js", null,null,null,null,null,null,null),
            };

            _mockService.SetupSequence(c => c.GetAllContents(It.IsAny<string>()))
                        .ThrowsAsync(apiException)
                        .ReturnsAsync(mockContent);

            _mockService.Setup(c => c.GetFileRawContent(It.IsAny<string>()))
                        .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(mockFileContent));
            // Act
            var result = await _mockService.Object.GetContentOfTSAndJSFiles(allowedFileExtensions, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal(mockFileContent, result.First());
        }

        [Fact]
        public async Task GetContentOfTSAndJSFiles_ShouldThrowException_WhenRetryCountExceeds()
        {
            // Arrange
            var allowedFileExtensions = new List<string>() { ".js", ".ts" };
            var apiException = new ApiException("mock error", System.Net.HttpStatusCode.ServiceUnavailable);

            _mockService.Setup(c => c.GetAllContents(It.IsAny<string>()))
                        .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() =>
                _mockService.Object.GetContentOfTSAndJSFiles(allowedFileExtensions, CancellationToken.None));
        }

        [Fact]
        public async Task GetContentOfTSAndJSFiles_ShouldHonorCancellation()
        {
            // Arrange
            var allowedFileExtensions = new List<string>() { ".js", ".ts" };
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _mockService.Object.GetContentOfTSAndJSFiles(allowedFileExtensions, cancellationTokenSource.Token));
        }
    }
}
