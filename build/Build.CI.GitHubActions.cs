using Nuke.Common.CI.GitHubActions;

[GitHubActions(
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
[GitHubActions(
    "build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["master", "main"],
    OnPushTags = ["v*.*.*", "v*.*.*-*"],
    OnPushIncludePaths = ["**/*.*"],
    OnPushExcludePaths = ["**/*.md"],
    PublishArtifacts = true,
    InvokedTargets = [nameof(Compile), nameof(Test), nameof(Pack), nameof(Publish)],
    ImportSecrets = ["NUGET_API_KEY", "MYGET_API_KEY"],
    CacheKeyFiles = ["global.json", "src/**/*.csproj", "src/**/package.json"])
]
public partial class Build;
