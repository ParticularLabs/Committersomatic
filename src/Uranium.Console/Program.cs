namespace Uranium.Console
{
    using System;
    using System.Collections.Generic;
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

            var groups = await new CrappyCommitterGroupService("groups.txt", "Particular").Get("Particular");

            var repositories = (await Task.WhenAll(groups
                .SelectMany(@group => @group.RepositoryIdList.Select(id => id.Owner))
                .Distinct()
                .Select(org => client.Repository.GetAllForOrg(org))))
                .SelectMany(repo => repo)
                .Select(repo => new Repository(new RepositoryId(repo.Name, repo.Owner.Login), repo.Private));

            foreach (var repo in repositories
                .Where(repo => !repo.IsPrivate)
                .Where(repo => !groups.Any(group => group.RepositoryIdList.Contains(repo.Id)))
                .OrderBy(repo => repo.Id.Owner)
                .ThenBy(repo => repo.Id.Name))
            {
                ColorConsole.WriteLine(
                    "* **".White(), $"Repo '{repo.Id.Owner}/{repo.Id.Name}' is not grouped!".Red(), "**".White());
            }

            var groupLoginContributions = new List<Contribution>();
            var commitService = new CommitService(client);
            foreach (var group in groups)
            {
                ColorConsole.WriteLine();
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("# ".White(), group.Name.Yellow());
                ColorConsole.WriteLine("### Repos".White());
                var contributions = new Dictionary<string, double>();
                foreach (var repo in group.RepositoryIdList)
                {
                    ColorConsole.WriteLine("* ".White(), repo.Name.Green());

                    IReadOnlyList<Commit> commits;
                    try
                    {
                        commits = await commitService.Get(repo.Owner, repo.Name);
                    }
                    catch (Exception ex)
                    {
                        ColorConsole.WriteLine(
                            "*".White(),
                            string.Format(CultureInfo.InvariantCulture, "Failed to get commits for '{0}'.", repo).Red(),
                            " ",
                            ex.Message.Yellow(),
                            "*".White());

                        continue;
                    }

                    foreach (var commit in commits.Where(commit => commit.Committer != null))
                    {
                        var age = Period.Between(
                            commit.Committed.LocalDateTime,
                            OffsetDateTime.FromDateTimeOffset(DateTimeOffset.UtcNow).LocalDateTime);

                        var value = 1 / Math.Pow(2, 2 * age.Days / 365.25d);

                        double sum;
                        if (!contributions.TryGetValue(commit.Committer, out sum))
                        {
                            contributions.Add(commit.Committer, value);
                        }
                        else
                        {
                            contributions[commit.Committer] = sum + value;
                        }
                    }
                }

                groupLoginContributions.AddRange(contributions
                    .OrderByDescending(contribution => contribution.Value)
                    .Select(contribution =>
                        new Contribution { Group = group.Name, Login = contribution.Key, Score = contribution.Value }));
            }

            using (var writer = new StreamWriter("matrix.txt", false))
            {
                writer.Write("Login/Group");
                foreach (var group in groupLoginContributions.Select(contribution => contribution.Group).Distinct())
                {
                    writer.Write("\t" + group);
                }

                writer.WriteLine();

                foreach (var login in groupLoginContributions.Select(contribution => contribution.Login).Distinct())
                {
                    writer.Write(login);
                    foreach (var group in groupLoginContributions.Select(contribution => contribution.Group).Distinct())
                    {
                        var contribution =
                            groupLoginContributions.SingleOrDefault(
                                candidate => candidate.Group == group && candidate.Login == login);

                        writer.Write(
                            "\t" +
                            (contribution?.Score.ToString(CultureInfo.InvariantCulture) ?? "0"));
                    }

                    writer.WriteLine();
                }
            }
        }

        private class Contribution
        {
            public string Group { get; set; }

            public string Login { get; set; }

            public double Score { get; set; }
        }
    }
}
