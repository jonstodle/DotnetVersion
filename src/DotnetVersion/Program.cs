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
    class Program
    {
        private static void Main(string[] args) =>
            CommandLineApplication.Execute<Program>(args);

        // ReSharper disable UnassignedGetOnlyAutoProperty
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
            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            var projectFile = !string.IsNullOrWhiteSpace(ProjectFilePath)
                ? new FileInfo(ProjectFilePath)
                : currentDirectory.EnumerateFiles("*.csproj").FirstOrDefault();
            if (projectFile?.Exists == false)
                throw new CliException(1, "Unable to find a project file.");

            var xDocument = XDocument.Load(projectFile.OpenRead());
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
            else if (Major)
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
                    build: "",
                    prerelease: "");
            }
            else if (Patch)
            {
                version = currentVersion.Change(
                    patch: currentVersion.Patch + 1,
                    build: "",
                    prerelease: "");
            }

            if (version is null)
            {
                var inputVersion = Prompt.GetString("New version:");
                version = ParseVersion(inputVersion);
            }
            else
                WriteLine($"New version: {version}");

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
                versionElement.Value = version.ToString();

            File.WriteAllText(projectFile.FullName, xDocument.ToString());

            if (!NoGit)
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
    }
}