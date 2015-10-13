using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NodaTime;
using Octokit;

namespace Uranium.Model.Octokit
{
    public class IssueService : IIssueService
    {
        private readonly IGitHubClient client;
        public IssueService(IGitHubClient client)
        {
            Guard.AgainstNullArgument(nameof(client), client);

            this.client = client;
        }

        public async Task<IReadOnlyList<Issue>> Get(string repositoryOwner, string repositoryName)
        {
            return (await this.client.Issue.GetAllForRepository(repositoryOwner, repositoryName))
                .Select(issue => new Issue(
                    new RepositoryId(repositoryOwner, repositoryName),
                    OffsetDateTime.FromDateTimeOffset(issue.CreatedAt),
                    issue.User.Login))
                .ToList();
        }
    }
}
