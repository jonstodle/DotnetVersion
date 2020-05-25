using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using Semver;
using static System.Console;

namespace DotnetVersion
{
    [Command("dotnet-version", Description = "Update project version")]
    public class Program
    {
        public static void Main(string[] args) =>
            CommandLineApplication.Execute<Program>(args);

        private const string AlphaString = "alpha";
        private const string BetaString = "beta";
        private const string ReleaseCandidateString = "rc";

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("-V|--version", Description = "Show version of the tool")]
        public bool ShowVersion { get; }

        [Option("--show", Description = "Only show the current version number")]
        public bool Show { get; }

        [Option("--new-version", Description = "New version")]
        public string NewVersion { get; }

        [Option("--major", Description = "Auto-increment major version number")]
        public bool Major { get; }

        [Option("--minor", Description = "Auto-increment minor version number")]
        public bool Minor { get; }

        [Option("--patch", Description = "Auto-increment patch version number")]
        public bool Patch { get; }

        [Option("--alpha", Description = "Auto-increment alpha version number")]
        public bool Alpha { get; }

        [Option("--beta", Description = "Auto-increment beta version number")]
        public bool Beta { get; }

        [Option("--rc", Description = "Auto-increment release candidate version number")]
        public bool ReleaseCandidate { get; }

        [Option("--final", Description = "Remove prerelease version number")]
        public bool Final { get; }

        [Option("-p|--project-file", Description = "Path to project file")]
        public string ProjectFilePath { get; }

        [Option("--no-git", Description = "Do not make any changes in git")]
        public bool NoGit { get; }

        [Option("--message", Description = "git commit message")]
        public string CommitMessage { get; }

        [Option("--no-git-tag", Description = "Do not generate a git tag")]
        public bool NoGitTag { get; }

        [Option("--git-version-prefix", Description = "Prefix before version in git")]
        public string GitVersionPrefix { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        // ReSharper disable once UnusedMember.Local
        private void OnExecute()
        {
            try
            {
                Run();
            }
            catch (CliException e)
            {
                Error.WriteLine(e.Message);
                Environment.Exit(e.ExitCode);
            }
        }

        private void Run()
        {
            if (ShowVersion)
            {
                if (SemVersion.TryParse(
                    FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion,
                    out var toolVersion))
                    WriteLine(toolVersion);
                else
                    WriteLine(typeof(Program).Assembly.GetName().Version.ToString(3));
                return;
            }

            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            var projectFile = !string.IsNullOrWhiteSpace(ProjectFilePath)
                ? new FileInfo(ProjectFilePath)
                : currentDirectory.EnumerateFiles("*.csproj").FirstOrDefault();

            if (projectFile is null ||
                projectFile.Exists == false)
                throw new CliException(1, $"Unable to find a project file in directory '{currentDirectory}'.");

            var projectFileFullName = projectFile.FullName;

            var xDocument = XDocument.Load(projectFileFullName);
            var versionElement = xDocument.Root?.Descendants("Version").FirstOrDefault();
            var currentVersion = ParseVersion(versionElement?.Value ?? "0.0.0");

            WriteLine($"Current version: {currentVersion}");

            if (Show)
                return;

            SemVersion version = null;

            if (!string.IsNullOrWhiteSpace(NewVersion))
            {
                version = ParseVersion(NewVersion);
            }
            else
            {
                if (Major)
                {
                    version = currentVersion.Change(
                        currentVersion.Major + 1,
                        0,
                        0,
                        "",
                        "");
                }
                else if (Minor)
                {
                    version = currentVersion.Change(
                        minor: currentVersion.Minor + 1,
                        patch: 0,
                        prerelease: "",
                        build: "");
                }
                else if (Patch)
                {
                    version = currentVersion.Change(
                        patch: currentVersion.Patch + 1,
                        prerelease: "",
                        build: "");
                }

                if (Final)
                {
                    version = (version ?? currentVersion).Change(
                        prerelease: string.Empty);
                }

                if (ReleaseCandidate)
                {
                    if (Final)
                        throw new CliException(1, "Can't increment release candidate number of the final version.");

                    version = (version ?? currentVersion).Change(
                        prerelease: CreatePreReleaseString(
                            ReleaseCandidateString,
                            version is null ? currentVersion.Prerelease : ""),
                        build: "");
                }
                else if (Beta)
                {
                    if (Final)
                        throw new CliException(1, "Can't increment beta number of the final version.");

                    if (version is null &&
                        currentVersion.Prerelease.StartsWith(ReleaseCandidateString,
                            StringComparison.OrdinalIgnoreCase))
                        throw new CliException(1,
                            "Can't increment beta version number of a release candidate version number.");

                    version = (version ?? currentVersion).Change(
                        prerelease: CreatePreReleaseString(BetaString,
                            version is null ? currentVersion.Prerelease : ""),
                        build: "");
                }
                else if (Alpha)
                {
                    if (Final)
                        throw new CliException(1, "Can't increment alpha number of the final version.");

                    if (version is null &&
                        currentVersion.Prerelease.StartsWith(ReleaseCandidateString,
                            StringComparison.OrdinalIgnoreCase))
                        throw new CliException(1,
                            "Can't increment alpha version number of a release candidate version number.");

                    if (version is null &&
                        currentVersion.Prerelease.StartsWith(BetaString, StringComparison.OrdinalIgnoreCase))
                        throw new CliException(1, "Can't increment alpha version number of a beta version number.");

                    version = (version ?? currentVersion).Change(
                        prerelease: CreatePreReleaseString(AlphaString,
                            version is null ? currentVersion.Prerelease : ""),
                        build: "");
                }
            }

            if (version is null)
            {
                var inputVersion = Prompt.GetString("New version:");
                version = ParseVersion(inputVersion);
            }
            else
            {
                WriteLine($"New version: {version}");
            }

            if (versionElement is null)
            {
                var propertyGroupElement = xDocument.Root?.Descendants("PropertyGroup").FirstOrDefault();
                if (propertyGroupElement is null)
                {
                    propertyGroupElement = new XElement("PropertyGroup");
                    xDocument.Root?.Add(propertyGroupElement);
                }

                propertyGroupElement.Add(new XElement("Version", version));
            }
            else
            {
                versionElement.Value = version.ToString();
            }

            File.WriteAllText(projectFileFullName, xDocument.ToString());

            if (!NoGit)
            {
                if (string.IsNullOrWhiteSpace(ProjectFilePath))
                {
                    try
                    {
                        var tag = $"{GitVersionPrefix}{version}";
                        var message = !string.IsNullOrWhiteSpace(CommitMessage)
                            ? CommitMessage
                            : tag;

                        Process.Start(new ProcessStartInfo("git", $"commit -am \"{message}\"")
                        {
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                        })?.WaitForExit();

                        if (!NoGitTag)
                        {
                            // Hack to make sure the wrong commit is tagged
                            Thread.Sleep(200);
                            Process.Start(new ProcessStartInfo("git", $"tag {tag}")
                            {
                                RedirectStandardError = true,
                                RedirectStandardOutput = true,
                            })?.WaitForExit();
                        }
                    }
                    catch
                    {
                        /* Ignored */
                    }
                }
                else
                {
                    WriteLine(
                        "Not running git integration when project file has been specified, to prevent running git in wrong directory.");
                }
            }

            WriteLine($"Successfully set version to {version}");
        }

        private SemVersion ParseVersion(string version)
        {
            try
            {
                return SemVersion.Parse(version);
            }
            catch
            {
                throw new CliException(1, $"Unable to parse version '{version}'.");
            }
        }

        private string CreatePreReleaseString(string preReleaseName, string preReleaseVersion)
        {
            var version = 0;
            if (preReleaseVersion.StartsWith(preReleaseName, StringComparison.OrdinalIgnoreCase))
            {
                var versionString = String.Join("", preReleaseVersion
                    .Reverse()
                    .TakeWhile(char.IsDigit)
                    .Reverse());
                int.TryParse(versionString, out version);
            }

            return $"{preReleaseName}.{version + 1}";
        }
    }
}