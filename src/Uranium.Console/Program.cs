namespace Uranium.Console
{
    using System;
    using Serilog;

    internal static class Program
    {
        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .ColoredConsole(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({Name:l}) {Message}{NewLine}{Exception}")
                .WriteTo
                .File(
                    $"{typeof(Program).Assembly.GetName().Name}.log",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({Name:l}) {Message}{NewLine}{Exception}")
                .CreateLogger();

            var organizations = new[] { "Particular", "ParticularLabs" };
            var githubLogin = Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME");
            var githubPassword = Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD");
            var includePrivateRepositories = false;

            Application.RunAsync(organizations, githubLogin, githubPassword, includePrivateRepositories).Wait();
        }
    }
}
