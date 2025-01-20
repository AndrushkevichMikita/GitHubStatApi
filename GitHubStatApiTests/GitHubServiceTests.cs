using GitHubStatApi.Configuration;
using GitHubStatApi.Services;
using GitHubStatApi.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;

namespace GitHubStatApiTests.Tests
{
    public class GitHubServiceTests
    {
        private readonly Mock<GitHubRepoService> _mockService;

        public GitHubServiceTests()
        {
            var mockOptions = new Mock<IOptions<GitHubOptions>>();
            mockOptions.Setup(o => o.Value).Returns(new GitHubOptions
            {
                Repo = "repo",
                Owner = "owner",
                ProductInfoName = "TestProduct",
                ProductGitHubAccessToken = "test-token"
            });

            var loggerMock = new Mock<ILogger<PolicyFactory>>();

            _mockService = new Mock<GitHubRepoService>(mockOptions.Object,
                                                       new PolicyFactory(loggerMock.Object),
                                                       new FileContentAnalyzerService(),
                                                       new GitHubClientFactory(mockOptions.Object))
            { CallBase = true };
        }

        [Fact]
        public async Task GetLetterFrequenciesFromFilesWithExtensions_ShouldRetry_OnApiExceededException()
        {
            // Arrange
            var allowedFileExtensions = new List<string>() { ".js", ".ts" };
            var mockFileContent = "console.log('test');";

            var apiException = new ApiException("mock error", System.Net.HttpStatusCode.ServiceUnavailable);

            var mockContent = new List<RepositoryContent>
            {
                new("test.js", "path/to/test.js", "", 0,  ContentType.File, "path/to/test.js", null,null,null,null,null,null,null),
            };

            _mockService.SetupSequence(c => c.GetRepositoryContents(It.IsAny<string>()))
                        .ThrowsAsync(apiException)
                        .ReturnsAsync(mockContent);

            _mockService.Setup(c => c.GetRepositoryFileRawContent(It.IsAny<string>()))
                        .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(mockFileContent));
            // Act
            var result = await _mockService.Object.GetLetterFrequenciesFromFilesWithExtensions(allowedFileExtensions, CancellationToken.None);

            // Assert
            Assert.Contains(result.Values, c => c != 0);
            Assert.Contains(new KeyValuePair<char, int>('o', 3), result);
        }

        [Fact]
        public async Task GetLetterFrequenciesFromFilesWithExtensions_ShouldThrowException_WhenRetryCountExceeds()
        {
            // Arrange
            var allowedFileExtensions = new List<string>() { ".js", ".ts" };
            var apiException = new ApiException("mock error", System.Net.HttpStatusCode.ServiceUnavailable);

            _mockService.Setup(c => c.GetRepositoryContents(It.IsAny<string>()))
                        .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() =>
                _mockService.Object.GetLetterFrequenciesFromFilesWithExtensions(allowedFileExtensions, CancellationToken.None));
        }

        [Fact]
        public async Task GetLetterFrequenciesFromFilesWithExtensions_ShouldHonorCancellation()
        {
            // Arrange
            var allowedFileExtensions = new List<string>() { ".js", ".ts" };
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _mockService.Object.GetLetterFrequenciesFromFilesWithExtensions(allowedFileExtensions, cancellationTokenSource.Token));
        }
    }
}
