using System;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

namespace DotnetVersion.Tests
{
    public class Program : IDisposable

    {
        private readonly StringWriter consoleOut;
        private readonly string tempFilePath;

        public Program()
        {
            consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var tempDirectoryPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(DotnetVersion)));
            tempFilePath = Path.Combine(tempDirectoryPath.FullName, $"{Guid.NewGuid()}.xml");
            File.WriteAllLines(tempFilePath, new[]
            {
                @"<Project Sdk=""Microsoft.NET.Sdk"">",
                "<PropertyGroup>",
                "<Version>1.2.3</Version>",
                "</PropertyGroup>",
                "</Project>",
            });
        }

        [Fact(Skip = "No automated way to check version number. Yet.")]
        public void ShowsTheToolVersion()
        {
            DotnetVersion.Program.Main(Args("-V"));

            consoleOut.ToString().ShouldBe("1.2.3" + Environment.NewLine);
        }

        [Fact]
        public void ShowsTheCurrentProjectVersionOnly()
        {
            DotnetVersion.Program.Main(Args("--show"));

            consoleOut.ToString().ShouldBe("Current version: 1.2.3" + Environment.NewLine);
        }

        [Theory]
        [InlineData("1.0.0")]
        [InlineData("1.5.0")]
        [InlineData("2.0.0")]
        public void SetsTheCorrectVersion(string newVersion)
        {
            DotnetVersion.Program.Main(Args("--new-version", newVersion));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                $"New version: {newVersion}",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to {newVersion}{Environment.NewLine}"));
        }

        [Fact]
        public void SetsNewMajorVersion()
        {
            DotnetVersion.Program.Main(Args("--major"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 2.0.0",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 2.0.0{Environment.NewLine}"));
        }

        [Fact]
        public void SetsNewMinorVersion()
        {
            DotnetVersion.Program.Main(Args("--minor"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 1.3.0",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 1.3.0{Environment.NewLine}"));
        }

        [Fact]
        public void SetsNewPatchVersion()
        {
            DotnetVersion.Program.Main(Args("--patch"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 1.2.4",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 1.2.4{Environment.NewLine}"));
        }

        [Fact]
        public void SetsCorrectAlphaVersion()
        {
            DotnetVersion.Program.Main(Args("--major", "--alpha"));
            DotnetVersion.Program.Main(Args("--alpha"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 2.0.0-alpha.1",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                "Successfully set version to 2.0.0-alpha.1",
                "Current version: 2.0.0-alpha.1",
                "New version: 2.0.0-alpha.2",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 2.0.0-alpha.2{Environment.NewLine}"));
        }

        [Fact]
        public void SetsCorrectBetaVersion()
        {
            DotnetVersion.Program.Main(Args("--major", "--beta"));
            DotnetVersion.Program.Main(Args("--beta"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 2.0.0-beta.1",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                "Successfully set version to 2.0.0-beta.1",
                "Current version: 2.0.0-beta.1",
                "New version: 2.0.0-beta.2",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 2.0.0-beta.2{Environment.NewLine}"));
        }

        [Fact]
        public void SetsCorrectReleaseCandidateVersion()
        {
            DotnetVersion.Program.Main(Args("--major", "--rc"));
            DotnetVersion.Program.Main(Args("--rc"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 2.0.0-rc.1",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                "Successfully set version to 2.0.0-rc.1",
                "Current version: 2.0.0-rc.1",
                "New version: 2.0.0-rc.2",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 2.0.0-rc.2{Environment.NewLine}"));
        }

        [Fact]
        public void SetsCorrectFinalVersion()
        {
            DotnetVersion.Program.Main(Args("--major", "--rc"));
            DotnetVersion.Program.Main(Args("--final"));

            consoleOut.ToString().ShouldBe(string.Join(
                Environment.NewLine,
                "Current version: 1.2.3",
                "New version: 2.0.0-rc.1",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                "Successfully set version to 2.0.0-rc.1",
                "Current version: 2.0.0-rc.1",
                "New version: 2.0.0",
                "Not running git integration when project file has been specified, to prevent running git in wrong directory.",
                $"Successfully set version to 2.0.0{Environment.NewLine}"));
        }

        public void Dispose()
        {
            File.Delete(tempFilePath);
        }

        private string[] Args(params string[] args) =>
            args.Concat(new[] {"-p", tempFilePath}).ToArray();
    }
}