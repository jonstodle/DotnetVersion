using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;

using static System.Console;

namespace DotnetVersion
{
    class Program
    {
        private static void Main(string[] args) =>
            CommandLineApplication.Execute<Program>(args);
        
        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("--new-version", Description = "New version (must be SemVer compliant)")]
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
            var currentVersion = versionElement?.Value ?? "0.0.0";

            var (major, minor, patch) = ParseVersion(currentVersion);

            if (!string.IsNullOrWhiteSpace(NewVersion))
            {
                (major, minor, patch) = ParseVersion(NewVersion);
            }
            else if (Major)
            {
                major++;
                minor = 0;
                patch = 0;
            }
            else if (Minor)
            {
                minor++;
                patch = 0;
            }
            else if (Patch)
            {
                patch++;
            }
            else
            {
                var version = Prompt.GetString("Version number:");
                (major, minor, patch) = ParseVersion(version);
            }

            var newVersion = string.Join(".", major, minor, patch);
            
            WriteLine($"Current version: {currentVersion}");
            WriteLine($"New version: {newVersion}");

            if (versionElement is null)
            {
                var propertyGroupElement = xDocument.Root?.Descendants("PropertyGroup").FirstOrDefault();
                if (propertyGroupElement is null)
                {
                    propertyGroupElement = new XElement("PropertyGroup");
                    xDocument.Root?.Add(propertyGroupElement);
                }
                
                propertyGroupElement.Add(new XElement("Version", newVersion));
            }
            else
                versionElement.Value = newVersion;
            
            File.WriteAllText(projectFile.FullName, xDocument.ToString());

            if (!NoGit)
                try
                {
                    var tag = $"{GitVersionPrefix}{newVersion}";
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
                catch { /* Ignored */ }
            
            WriteLine($"Successfully set version to {newVersion}");
        }

        private (int major, int minor, int patch) ParseVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new CliException(1, "Version string can not be empty.");

            var versionValues = version.Split('.');
            if (versionValues.Length == 0)
                throw new CliException(1, "Version must have at least one (1) value.");

            var major = 0;
            var minor = 0;
            var patch = 0;

            if (versionValues.Length > 0)
                if (!int.TryParse(versionValues[0], out major))
                    throw new CliException(1, "Major version number could not be parsed.");
            if (versionValues.Length > 1)
                if (!int.TryParse(versionValues[1], out minor))
                    throw new CliException(1, "Minor version number could not be parsed.");
            if (versionValues.Length > 2)
                if (!int.TryParse(versionValues[2], out patch))
                    throw new CliException(1, "Patch version number could not be parsed.");

            return (major, minor, patch);
        }
    }
}