using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uranium.Model
{
    public interface IPullRequestService
    {
        Task<IReadOnlyList<PullRequest>> Get(string repositoryOwner, string repositoryName);
    }
}