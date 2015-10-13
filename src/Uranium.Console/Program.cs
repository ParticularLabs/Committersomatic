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

            Application.RunAsync(
                    "Particular",
                    Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME"),
                    Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD"))
                .Wait();
        }
    }
}
