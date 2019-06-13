using System;

namespace DotnetVersion
{
    public class CliException : Exception
    {
        public int ExitCode { get; }

        public CliException(int exitCode, string message): base(message)
        {
            ExitCode = exitCode;
        }
    }
}