namespace Uranium.Model
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICommitService
    {
        Task<IReadOnlyList<Commit>> Get(string repositoryOwner, string repositoryName);
    }
}
