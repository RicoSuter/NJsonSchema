using Nuke.Common.CI.GitHubActions;

[GitHubActionsAttribute(
    "pr",
    GitHubActionsImage.WindowsLatest,
    //GitHubActionsImage.UbuntuLatest,
    //GitHubActionsImage.MacOsLatest,
    OnPullRequestBranches = new[] { "master", "main" },
    OnPullRequestIncludePaths = new[] { "**/*.*" },
    OnPullRequestExcludePaths = new[] { "**/*.md" },
    PublishArtifacts = false,
    InvokedTargets = new[] { nameof(Compile), nameof(Test), nameof(Pack) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj", "src/**/package.json" }),
]
[GitHubActionsAttribute(
    "build",
    GitHubActionsImage.WindowsLatest,
    //GitHubActionsImage.UbuntuLatest,
    //GitHubActionsImage.MacOsLatest,
    OnPushBranches = new[] { "master", "main" },
    OnPushTags = new[] { "v*.*.*", "v*.*.*-*" },
    OnPushIncludePaths = new[] { "**/*.*" },
    OnPushExcludePaths = new[] { "**/*.md" },
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(Compile), nameof(Test), nameof(Pack), nameof(Publish) },
    ImportSecrets = new [] { "NUGET_API_KEY", "MYGET_API_KEY" },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj", "src/**/package.json" })
]
public partial class Build
{
}
