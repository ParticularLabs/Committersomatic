using Octokit;
using Octokit.Internal;

namespace Uranium.Console
{
    internal class GitHubClientFactory
    {
        public static GitHubClient Create(string application, string login, string password)
        {
            var connection = new Connection(
                new ProductHeaderValue(application),
                GitHubClient.GitHubApiUrl,
                new InMemoryCredentialStore(new Credentials(login, password)));

            return new GitHubClient(connection);
        }
    }
}
