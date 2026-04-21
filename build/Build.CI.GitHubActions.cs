using System;
using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;

[CustomGitHubActions(
    "pr",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    OnPullRequestBranches = ["master", "main"],
    OnPullRequestIncludePaths = ["**/*.*"],
    OnPullRequestExcludePaths = ["**/*.md"],
    PublishArtifacts = false,
    InvokedTargets = [nameof(Compile), nameof(Test), nameof(Pack)],
    CacheKeyFiles = ["global.json", "src/**/*.csproj", "src/**/package.json"],
    ConcurrencyCancelInProgress = true)
]
[CustomGitHubActions(
    "build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["master", "main"],
    OnPushTags = ["v*.*.*", "v*.*.*-*"],
    OnPushIncludePaths = ["**/*.*"],
    OnPushExcludePaths = ["**/*.md"],
    PublishArtifacts = true,
    PublishCondition = "runner.os == 'Windows'",
    InvokedTargets = [nameof(Compile), nameof(Test), nameof(Pack), nameof(Publish)],
    ImportSecrets = ["NUGET_API_KEY", "MYGET_API_KEY"],
    CacheKeyFiles = ["global.json", "src/**/*.csproj", "src/**/package.json"])
]
public partial class Build;

class CustomGitHubActionsAttribute : GitHubActionsAttribute
{
    public CustomGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var job = base.GetJobs(image, relevantTargets);

        var newSteps = new List<GitHubActionsStep>(job.Steps);

        // only need to list the ones that are missing from default image
        newSteps.Insert(0, new GitHubActionsSetupDotNetStep(["10.0"]));

        var onWindows = image.ToString().StartsWith("windows", StringComparison.OrdinalIgnoreCase);
        if (onWindows)
        {
            newSteps.Insert(0, new GitHubActionsUseGnuTarStep());
            newSteps.Insert(0, new GitHubActionsConfigureLongPathsStep());
        }

        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsSetupDotNetStep : GitHubActionsStep
{
    public GitHubActionsSetupDotNetStep(string[] versions)
    {
        Versions = versions;
    }

    string[] Versions { get; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/setup-dotnet@v5");

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("dotnet-version: |");
                using (writer.Indent())
                {
                    foreach (var version in Versions)
                    {
                        writer.WriteLine(version);
                    }
                }
            }
        }
    }
}

class GitHubActionsUseGnuTarStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- if: ${{ runner.os == 'Windows' }}");
        using (writer.Indent())
        {
            writer.WriteLine("name: 'Use GNU tar'");
            writer.WriteLine("shell: cmd");
            writer.WriteLine("run: |");
            using (writer.Indent())
            {
                writer.WriteLine("echo \"Adding GNU tar to PATH\"");
                writer.WriteLine("echo C:\\Program Files\\Git\\usr\\bin>>\"%GITHUB_PATH%\"");
            }
        }
    }
}

class GitHubActionsConfigureLongPathsStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: 'Allow long file path'");
        using (writer.Indent())
        {
            writer.WriteLine("run: git config --system core.longpaths true");
        }
    }
}