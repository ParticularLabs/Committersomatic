namespace Uranium.Console
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Uranium.Model;

    internal static class TsvContributionsRepository
    {
        public static void Save(IReadOnlyList<Contribution> contributions)
        {
            using (var writer = new StreamWriter("contributions.txt", false))
            {
                writer.Write("Login/Group");
                foreach (var group in contributions.Select(contribution => contribution.Group).Distinct())
                {
                    writer.Write("\t" + @group);
                }

                writer.WriteLine();

                foreach (var login in contributions.Select(contribution => contribution.Login).Distinct())
                {
                    writer.Write(login);
                    foreach (var group in contributions.Select(contribution => contribution.Group).Distinct())
                    {
                        var contribution =
                            contributions.SingleOrDefault(
                                candidate => candidate.Group == @group && candidate.Login == login);

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
