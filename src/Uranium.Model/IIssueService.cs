using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uranium.Model
{
    public interface IIssueService
    {
        Task<IReadOnlyList<Issue>> Get(string repositoryOwner, string repositoryName);
    }
}