namespace Uranium.Console
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.FSharp.Collections;
    using Serilog;
    using Uranium.Console.Logging;
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .ColoredConsole(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({Name:l}) {Message}{NewLine}{Exception}")
                .WriteTo
                .File(
                    $"{typeof(Program).Assembly.GetName().Name}.log",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({Name:l}) {Message}{NewLine}{Exception}")
                .CreateLogger();

            var log = LogProvider.GetCurrentClassLogger();

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
                .Where(repository => !committerGroups.Any(group => group.RepositoryList.Contains(repository.Id)))
                .ToList();

            foreach (var repository in ungroupedRepositories.OrderBy(_ => _))
            {
                log.WarnFormat("{@Repository} is not grouped.", repository);
            }

            committerGroups =
                committerGroups.Concat(new[]
                {
                    new CommitterGroup("(Ungrouped)",ListModule.OfSeq(ungroupedRepositories.Select(repository => repository.Id))),
                })
                .ToList();

            var contributions = (await Task.WhenAll(
                    ContributionService.Get(committerGroups, new CommitService(client)),
                    ContributionService.Get(committerGroups, new IssueService(client)),
                    ContributionService.Get(committerGroups, new PullRequestService(client))))
                .SelectMany(_ => _)
                .GroupBy(contribution => new { contribution.Group, contribution.Login })
                .Select(group => new Contribution(group.Key.Group, group.Key.Login, group.Sum(contribution => contribution.Score)))
                .ToList();

            TsvContributionsRepository.Save(contributions);
        }
    }
}
