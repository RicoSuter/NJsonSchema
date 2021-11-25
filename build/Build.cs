using System;
using System.Linq;
using System.Runtime.InteropServices;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    static bool IsRunningOnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    string TagVersion => GitRepository.Tags.SingleOrDefault(x => x.StartsWith("v"))?[1..];

    string VersionSuffix =>
        string.IsNullOrWhiteSpace(TagVersion)
            ? "preview-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmm")
            : "";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .After(Compile)
        .Executes(() =>
        {
            var framework = "";
            if (!IsRunningOnWindows)
            {
                framework = "net6.0";
            }

            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetFramework(framework)
            );
        });

    Target Pack => _ => _
        .After(Test, Compile)
        .Produces(ArtifactsDirectory / "*.*")
        .Executes(() =>
        {
            if (Configuration != Configuration.Release)
            {
                throw new InvalidOperationException("Cannot pack if compilation hasn't been done in Release mode, use --configuration Release");
            }

            EnsureCleanDirectory(ArtifactsDirectory);

            DotNetPack(s => s
                .SetProcessWorkingDirectory(SourceDirectory)
                .SetAssemblyVersion(TagVersion)
                .SetFileVersion(TagVersion)
                .SetInformationalVersion(TagVersion)
                .SetVersionSuffix(VersionSuffix)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetOutputDirectory(ArtifactsDirectory)
            );
        });
}