namespace Uranium.Model
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICommitterGroupService
    {
        Task<IReadOnlyList<CommitterGroup>> Get(string repositoryOwner);
    }
}
