using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Octokit;

namespace Uranium.Model.Octokit
{
    public class RepositoryService : IRepositoryService
    {
        private readonly IGitHubClient client;
        public RepositoryService(IGitHubClient client)
        {
            Guard.AgainstNullArgument("client", client);

            this.client = client;
        }

        public async Task<IReadOnlyList<Repository>> Get(string organisation)
        {
            return (await this.client.Repository.GetAllForOrg(organisation))
                .Select(repository => new Repository(new RepositoryId(organisation,repository.Name),repository.Private))
                .ToArray();
        }
    }
}
