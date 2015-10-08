namespace Uranium.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.FSharp.Collections;
    using Uranium.Model;

    internal class CrappyCommitterGroupService : ICommitterGroupService
    {
        private readonly string filename;
        private readonly string repositoryOwner;

        public CrappyCommitterGroupService(string filename, string repositoryOwner)
        {
            this.filename = filename;
            this.repositoryOwner = repositoryOwner;
        }
        public Task<IReadOnlyList<CommitterGroup>> Get(string repositoryOwner)
        {
            if (!string.Equals(repositoryOwner, this.repositoryOwner, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException($"Cannot get committer groups for \"{repositoryOwner}\".");
            }

            IReadOnlyList<CommitterGroup> result = File.ReadAllLines(this.filename)
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith("//", StringComparison.Ordinal))
                .Select(line => line.Split())
                .GroupBy(tokens => tokens[1], tokens => tokens[0])
                .Select(group => new CommitterGroup(
                    group.Key,
                    ListModule.OfSeq(group.Select(repositoryName => new RepositoryId(repositoryName, repositoryOwner)))))
                .ToList();

            return Task.FromResult(result);
        }
    }
}
