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

            var organization = "Particular";
            var groups = await new CrappyCommitterGroupService("groups.txt", organization).Get(organization);
            foreach (var repo in (await client.Repository.GetAllForOrg(organization))
                .Where(repo => !repo.Private)
                .Where(repo => !groups.Any(group => group.RepositoryIdList.Any(repoId => repoId.Name == repo.Name)))
                .OrderBy(repo => repo.Name))
            {
                ColorConsole.WriteLine(
                    "* **".White(),
                    string.Format(CultureInfo.InvariantCulture, "Repo '{0}' is not grouped!", repo.Name).Red(),
                    "**".White());
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

                    IReadOnlyList<Model.Commit> commits;
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
                        var age = Period.Between(commit.Committed.LocalDateTime, OffsetDateTime.FromDateTimeOffset(DateTimeOffset.UtcNow).LocalDateTime);
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
