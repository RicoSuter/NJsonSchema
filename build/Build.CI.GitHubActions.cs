using Nuke.Common.CI.GitHubActions;

[GitHubActionsAttribute(
    "pr",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    GitHubActionsImage.MacOsLatest,
    OnPullRequestBranches = new [] { "master" },
    PublishArtifacts = false,
    InvokedTargets = new[] { nameof(Compile), nameof(Test), nameof(Pack) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj", "src/**/package.json" }),
]
[GitHubActionsAttribute(
    "build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    GitHubActionsImage.MacOsLatest,
    OnPushBranches = new [] { "master" },
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(Compile), nameof(Test), nameof(Pack) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj", "src/**/package.json" })
]
public partial class Build
{
}
