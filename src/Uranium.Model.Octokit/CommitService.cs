using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NodaTime;
using Octokit;

namespace Uranium.Model.Octokit
{
    public class CommitService : ICommitService
    {
        private readonly IGitHubClient client;
        public CommitService(IGitHubClient client)
        {
            Guard.AgainstNullArgument("client", client);

            this.client = client;
        }

        public async Task<IReadOnlyList<Commit>> Get(string repositoryOwner, string repositoryName)
        {
            return (await this.client.Repository.Commits.GetAll(repositoryOwner, repositoryName))
                .Select(commit => new Commit(
                    new RepositoryId(repositoryOwner, repositoryName),
                    OffsetDateTime.FromDateTimeOffset(commit.Commit.Committer.Date),
                    commit.Committer.Login,
                    OffsetDateTime.FromDateTimeOffset(commit.Commit.Author.Date),
                    commit.Author.Login))
                .ToArray();
        }
    }
}
