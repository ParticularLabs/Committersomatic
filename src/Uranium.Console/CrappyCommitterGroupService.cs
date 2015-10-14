namespace Uranium.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.FSharp.Collections;
    using Uranium.Model;

    internal static class CrappyCommitterGroupService
    {
        public static IReadOnlyList<CommitterGroup> Get(string repositoryOwner)
        {
            var path = $"groups-{repositoryOwner}.txt";
            return File.Exists(path)
                ? File.ReadAllLines(path)
                    .Select(line => line.Trim())
                    .Where(line => !line.StartsWith("//", StringComparison.Ordinal))
                    .Select(line => line.Split())
                    .GroupBy(tokens => tokens[1], tokens => tokens[0])
                    .Select(group => new CommitterGroup(
                        @group.Key,
                        ListModule.OfSeq(@group.Select(repositoryName => new RepositoryId(repositoryOwner, repositoryName)))))
                    .ToList()
                : new List<CommitterGroup>();
        }
    }
}
