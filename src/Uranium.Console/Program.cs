namespace Uranium.Console
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ColoredConsole;
    using Octokit;
    using Octokit.Internal;

    internal static class Program
    {
        public static void Main()
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            var organization = "Particular";

            var credentials = new Credentials(
                Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME"),
                Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD"));

            var credentialStore = new InMemoryCredentialStore(credentials);

            var connection = new Connection(
                new ProductHeaderValue("GitHubIssues"),
                GitHubClient.GitHubApiUrl,
                credentialStore);

            var client = new GitHubClient(connection);

            var groups = File.ReadAllLines("groups.txt")
                .Select(line => line.Split())
                .GroupBy(tokens => tokens[1], tokens => tokens[0])
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var repo in (await client.Repository.GetAllForOrg(organization))
                .Where(repo => !repo.Private)
                .Where(repo => !groups.Any(group => group.Value.Contains(repo.Name)))
                .OrderBy(repo => repo.Name))
            {
                ColorConsole.WriteLine(
                    "* **".White(),
                    string.Format(CultureInfo.InvariantCulture, "Repo '{0}' is not grouped!", repo.Name).Red(),
                    "**".White());
            }

            var groupLoginContributions = new List<Contribution>();
            var staff = await client.Organization.Member.GetAll(organization);
            foreach (var group in groups)
            {
                ColorConsole.WriteLine();
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("# ".White(), group.Key.Yellow());
                ColorConsole.WriteLine("### Repos".White());
                var contributions = new Dictionary<string, double>();
                foreach (var repo in group.Value)
                {
                    ColorConsole.WriteLine("* ".White(), repo.Green());

                    List<GitHubCommit> commits;
                    try
                    {
                        commits = (await client.Repository.Commits.GetAll(organization, repo)).ToList();
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

                    foreach (var commit in commits
                        .Where(commit => commit.Committer != null)
                        .Where(commit => staff.Any(member => member.Login == commit.Committer.Login)))
                    {
                        var age = DateTime.UtcNow - commit.Commit.Committer.Date.UtcDateTime;
                        var value = 1 / Math.Pow(2, 2 * age.TotalDays / 365.25d);

                        double sum;
                        if (!contributions.TryGetValue(commit.Committer.Login, out sum))
                        {
                            contributions.Add(commit.Committer.Login, value);
                        }
                        else
                        {
                            contributions[commit.Committer.Login] = sum + value;
                        }
                    }
                }

                groupLoginContributions.AddRange(contributions
                    .OrderByDescending(contribution => contribution.Value)
                    .Select(contribution =>
                        new Contribution { Group = @group.Key, Login = contribution.Key, Score = contribution.Value }));
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
