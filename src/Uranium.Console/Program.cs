namespace Uranium.Console
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ColoredConsole;
    using NodaTime;
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

            var commitService = new CommitService(client);
            var contributions = (await Task.WhenAll(committerGroups.SelectMany(group => group.RepositoryList.Select(id =>
                {
                    ColorConsole.WriteLine($"Getting commits for \"#{id.Owner}/#{id.Name}\"...".Green());
                    return commitService.Get(id.Owner, id.Name).ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            ColorConsole.WriteLine($"Failed to getting commits for \"#{id.Owner}/#{id.Name}\". #{task.Exception.InnerException.Message}".Red());
                            return Enumerable.Empty<Commit>();
                        }

                        Console.WriteLine($"Got commits for \"#{id.Owner}/#{id.Name}\"".Green());
                        return task.Result;
                    });
                }))))
                .SelectMany(_ => _)
                .Select(commit => new { Login = commit.Committer, commit.Repository, Score = Score(commit) })
                .GroupBy(contribution => new { Group = committerGroups.First(group => group.RepositoryList.Contains(contribution.Repository)).Name, contribution.Login })
                .Select(g => new Contribution { Group = g.Key.Group, Login = g.Key.Login, Score = g.Sum(contribution => contribution.Score) })
                .ToList();

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

        private static double Score(Commit commit)
        {
            var age = Period.Between(
                commit.Committed.LocalDateTime,
                OffsetDateTime.FromDateTimeOffset(DateTimeOffset.UtcNow).LocalDateTime);

            return 1 / Math.Pow(2, 2 * age.Days / 365.25d);
        }

        private class Contribution
        {
            public string Group { get; set; }

            public string Login { get; set; }

            public double Score { get; set; }
        }
    }
}
