namespace Uranium.Console
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ColoredConsole;
    using Uranium.Model;
    using Uranium.Model.Octokit;

    internal static class Program
    {
        public static void Main()
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            var client = GitHubClientFactory.Create(
                typeof(Program).Namespace,
                Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME"),
                Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD"));

            var committerGroups = await new CrappyCommitterGroupService("groups.txt", "Particular").Get("Particular");

            var repositoryService = new RepositoryService(client);
            var repositories = (await Task.WhenAll(committerGroups
                    .SelectMany(@group => @group.RepositoryList.Select(id => id.Owner))
                    .Distinct()
                    .Select(owner => repositoryService.Get(owner))))
                .SelectMany(_ => _)
                .ToList();

            var ungroupedRepositories = repositories
                .Where(repository => !committerGroups.Any(group => group.RepositoryList.Contains(repository.Id)));

            foreach (var repo in ungroupedRepositories.OrderBy(_ => _))
            {
                ColorConsole.WriteLine(
                    "* **".White(), $"Repo '{repo.Id.Owner}/{repo.Id.Name}' is not grouped!".Red(), "**".White());
            }

            var contributions = await ContributionService.Get(committerGroups, new CommitService(client));

            using (var writer = new StreamWriter("matrix.txt", false))
            {
                writer.Write("Login/Group");
                foreach (var group in contributions.Select(contribution => contribution.Group).Distinct())
                {
                    writer.Write("\t" + group);
                }

                writer.WriteLine();

                foreach (var login in contributions.Select(contribution => contribution.Login).Distinct())
                {
                    writer.Write(login);
                    foreach (var group in contributions.Select(contribution => contribution.Group).Distinct())
                    {
                        var contribution =
                            contributions.SingleOrDefault(
                                candidate => candidate.Group == group && candidate.Login == login);

                        writer.Write(
                            "\t" +
                            (contribution?.Score.ToString(CultureInfo.InvariantCulture) ?? "0"));
                    }

                    writer.WriteLine();
                }
            }
        }
    }
}
