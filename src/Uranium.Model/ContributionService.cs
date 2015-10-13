﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace Uranium.Model
{
    public static class ContributionService
    {
        public static async Task<IReadOnlyList<Contribution>> Get(IReadOnlyCollection<CommitterGroup> committerGroups, ICommitService commitService)
        {
            return (await Task.WhenAll(committerGroups.SelectMany(group => @group.RepositoryList.Select(id =>
            {
                Console.WriteLine($"Getting commits for \"#{id.Owner}/#{id.Name}\"...");
                return commitService.Get(id.Owner, id.Name).ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Console.WriteLine($"Failed to getting commits for \"#{id.Owner}/#{id.Name}\". #{task.Exception.InnerException.Message}");
                        return Enumerable.Empty<Commit>();
                    }

                    Console.WriteLine($"Got commits for \"#{id.Owner}/#{id.Name}\"");
                    return task.Result;
                });
            }))))
                .SelectMany(_ => _)
                .Select(commit => new { Login = commit.Committer, commit.Repository, Score = Score(commit) })
                .GroupBy(contribution => new { Group = committerGroups.First(group => @group.RepositoryList.Contains(contribution.Repository)).Name, contribution.Login })
                .Select(g => new Contribution(g.Key.Group, g.Key.Login, g.Sum(contribution => contribution.Score)))
                .ToList();
        }

        private static double Score(Commit commit)
        {
            var age = Period.Between(
                commit.Committed.LocalDateTime,
                OffsetDateTime.FromDateTimeOffset(DateTimeOffset.UtcNow).LocalDateTime);

            return 1 / Math.Pow(2, 2 * age.Days / 365.25d);
        }
    }
}