using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NodaTime;
using Octokit;

namespace Uranium.Model.Octokit
{
    public class PullRequestService : IPullRequestService
    {
        private readonly IGitHubClient client;
        public PullRequestService(IGitHubClient client)
        {
            Guard.AgainstNullArgument(nameof(client), client);

            this.client = client;
        }

        public async Task<IReadOnlyList<PullRequest>> Get(string repositoryOwner, string repositoryName)
        {
            return (await this.client.PullRequest.GetAllForRepository(repositoryOwner, repositoryName))
                .Select(pullRequest => new PullRequest(
                    new RepositoryId(repositoryOwner, repositoryName),
                    OffsetDateTime.FromDateTimeOffset(pullRequest.CreatedAt),
                    pullRequest.User.Login))
                .ToList();
        }
    }
}
