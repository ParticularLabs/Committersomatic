namespace Uranium.Console
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.FSharp.Collections;
    using Uranium.Console.Logging;
    using Uranium.Model;
    using Uranium.Model.Octokit;

    internal static class Application
    {
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        public static async Task RunAsync(
            string organization, string githubLogin, string githubPassword, bool includePrivateRepositories)
        {
            var committerGroups = await CrappyCommitterGroupService.Get(organization);

            var client = GitHubClientFactory.Create(typeof(Program).Namespace, githubLogin, githubPassword);
            var repositoryService = new RepositoryService(client);
            var repositories = (await Task.WhenAll(committerGroups
                    .SelectMany(@group => @group.RepositoryList.Select(id => id.Owner))
                    .Distinct()
                    .Select(owner => repositoryService.Get(owner))))
                .SelectMany(_ => _)
                .Where(repository => includePrivateRepositories || !repository.IsPrivate)
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
