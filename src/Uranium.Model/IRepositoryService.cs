namespace Uranium.Model
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IRepositoryService
    {
        Task<IReadOnlyList<Repository>> Get(string owner);
    }
}
