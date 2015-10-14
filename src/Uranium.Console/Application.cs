namespace Uranium.Console
{
    using Microsoft.FSharp.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Uranium.Console.Logging;
    using Uranium.Model;
    using Uranium.Model.Octokit;

    internal static class Application
    {
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        public static async Task RunAsync(
            IReadOnlyList<string> organizations, string githubLogin, string githubPassword, bool includePrivateRepositories)
        {
            var committerGroups = organizations.SelectMany(CrappyCommitterGroupService.Get).ToList();

            var client = GitHubClientFactory.Create(typeof(Program).Namespace, githubLogin, githubPassword);

            var repositoryService = new RepositoryService(client);
            var ungroupedRepositories = (await Task.WhenAll(committerGroups
                    .SelectMany(@group => @group.RepositoryList.Select(id => id.Owner))
                    .Concat(organizations)
                    .Distinct()
                    .Select(owner => repositoryService.Get(owner))))
                .SelectMany(_ => _)
                .Where(repository => includePrivateRepositories || !repository.IsPrivate)
                .Where(repository => !committerGroups.Any(group => group.RepositoryList.Contains(repository.Id)))
                .ToList();

            foreach (var repository in ungroupedRepositories.OrderBy(_ => _))
            {
                log.WarnFormat("{@Repository} is not grouped.", repository);
            }

            committerGroups =
                committerGroups.Concat(new[]
                {
                    new CommitterGroup("(Ungrouped)", ListModule.OfSeq(ungroupedRepositories.Select(repository => repository.Id))),
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
