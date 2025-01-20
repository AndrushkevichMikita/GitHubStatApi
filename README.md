# GitHubStatApi

**GitHubStatApi** is a .NET Core Web API application that interacts with the GitHub API to fetch repository content and analyze file data. The application demonstrates efficient handling of external API calls using **Polly for resilience**, including retry and circuit breaker policies, while supporting parallelism and asynchronous operations for optimal performance.

## Features

- **GitHub API Integration**:
  - Fetches repository contents, including directories and files.
  - Supports filtering by file extensions (e.g., `.js`, `.ts`).
  - 
- **Cancellation Support**:
  - Implemented cancellation tokens across the application to allow for graceful termination of operations, ensuring better control over long-running tasks and responsiveness to user actions.

- **Resilience Patterns**:

  - **Retry Policy**: In addition to handling transient failures, the retry policy monitors and handles the exhaustion of GitHub's request limit (5000 requests per authenticated account), ensuring that operations fail gracefully when the limit is reached.

  - **Circuit Breaker**: Stops requests temporarily when failures exceed a threshold to prevent overloading.

- **File Content Analysis**:
  - Calculates letter frequencies in the repository files.

- **Parallel Processing**:
  - Processes files and directories concurrently while limiting the number of simultaneous operations.

## Technologies Used

- **.NET 6**
- **ASP.NET Core** for the Web API.
- **Polly** for resilience policies.
- **Octokit** for GitHub API interaction.
- **Dependency Injection** for better modularity and testability.
- **xUnit** for unit testing.
- **Moq** for mocking dependencies in tests.

## Getting Started

### Prerequisites

- **.NET 6 SDK**: [Install here](https://dotnet.microsoft.com/download/dotnet/6.0).
- **GitHub Personal Access Token**:
  - Required to authenticate with the GitHub API for increased rate limits.
  
You can securely store the `ProductGitHubAccessToken` in your user secrets instead of including it in `appsettings.json`.
To set up user secrets, run the following command in GitHubStatApi\GitHubStatApi subfolder:

```bash
dotnet user-secrets set "GitHubOptions:ProductGitHubAccessToken" "your-personal-access-token"
```

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/AndrushkevichMikita/GitHubStatApi.git
   cd GitHubStatApi
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Configure the application:
   - Update the `appsettings.json` file with your GitHub configuration:
     ```json
     {
       "GitHubOptions": {
         "Owner": "your-repo-owner",
         "Repo": "your-repo-name",
         "ProductInfoName": "GitHubStatApi",
         "ProductGitHubAccessToken": "your-personal-access-token"
       }
     }
     ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. Access the API at `https://localhost:7216/swagger/index.html`.

## Testing

Run unit tests using `xUnit`:
```bash
dotnet test
```

## Key Classes and Responsibilities

- **`GitHubRepoService`**:
  - Handles interaction with the GitHub API for fetching repository contents.
- **`FileContentAnalyzerService`**:
  - Processes and analyzes file content.
- **`PolicyFactory`**:
  - Provides resilience policies like retries and circuit breakers for handling external API failures.

## Future Improvements

- **Modular Architecture**:
  - Consider adopting a modular architecture if the solution grows to improve maintainability and scalability.
- **Archives Support**:
  - Use GitHub's tarball or zipball archive endpoints to efficiently retrieve and process repository data in bulk.
- **Extended File Analysis**:
  - Support additional statistics (e.g., word counts, file sizes).
- **Docker Support**:
  - Add Docker configuration for containerized deployment.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.

