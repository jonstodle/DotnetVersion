# DotnetVersion

A simple tool to update the version number of your project. If you know of `yarn version`, this is that for .NET.

## Usage

### Basic usage
The simplest way to use the tool is to run the command with no input:

```bash
dotnet version
```

This will ask you to specify the desired version number.

### Specifying version change

To prevent the prompt you can use the `--new-version` option to specify the version number:

```bash
dotnet version --new-version 1.2.3
```

You can let the tool automatically figure out the proper version by specifying which part of the version should be incremented:

```bash
# Given version is 1.2.3

# Make the version 1.2.4
dotnet version --patch

# Make the version 1.3.0
dotnet version --minor

# Make the version 2.0.0
dotnet version --major
```

### Custom project file path

By default, the tool will look for a `csproj` file in the current directory. You can use `--project-file` to specify a custom path:

```bash
# Increment major version number of specified project file
dotnet version --project-file path/to/project/file --major
```

### Git integration

By default the tool will commit immediately after changing the version and tag the commit with the version number. This can be disabled by passing `--no-git`:

```bash
# Increment minor version number, but don't commit it
dotnet version --no-git --minor
```

The message of the commit will be the version number. Use `--message` to override the commit message:

```bash
# Increment minor version number and commit with custom message
dotnet version --minor --message "Bump the minor version"
```

Use `--no-git-tag` to prevent a tag being added to the commit:

```bash
# Increment patch version number, but don't add a tag
dotnet version --patch --no-git-tag
```

To specify a prefix to the version number, use `--git-version-prefix`. This prefix is added to both the commit message (if it's not overridden) and the tag:

```bash
# Increment patch version number and prefix the version with 'v'
dotnet version --patch --git-version-prefix v
```

Help output:

```bash
Update project version

Usage: dotnet-version [options]

Options:
  --new-version         New version
  --major               Auto-increment major version number
  --minor               Auto-increment minor version number
  --patch               Auto-increment patch version number
  -p|--project-file     Path to project file
  --no-git              Do not make any changes in git
  --message             git commit message
  --no-git-tag          Do not generate a git tag
  --git-version-prefix  Prefix before version in git
  -?|-h|--help          Show help information
```
