using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Logger;
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

    private static string DateTimeSuffix = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    static bool IsRunningOnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    bool IsTaggedBuild;
    string VersionPrefix;
    string VersionSuffix;

    string DetermineVersionPrefix()
    {
        var versionPrefix = GitRepository.Tags.SingleOrDefault(x => x.StartsWith("v"))?[1..];
        if (!string.IsNullOrWhiteSpace(versionPrefix))
        {
            IsTaggedBuild = true;
            Info($"Tag version {versionPrefix} from Git found, using it as version prefix");
        }
        else
        {
            var propsDocument = XDocument.Parse(TextTasks.ReadAllText(SourceDirectory / "Directory.Build.props"));
            versionPrefix = propsDocument.Element("Project").Element("PropertyGroup").Element("VersionPrefix").Value;
            Info($"Version prefix {versionPrefix} read from Directory.Build.props");
        }

        return versionPrefix;
    }

    protected override void OnBuildInitialized()
    {
        VersionPrefix = DetermineVersionPrefix();

        VersionSuffix = !IsTaggedBuild
            ? $"preview-{DateTime.UtcNow:yyyyMMdd-HHmm}"
            : "";

        if (IsLocalBuild)
        {
            VersionSuffix = $"dev-{DateTime.UtcNow:yyyyMMdd-HHmm}";
        }

        using var _ = Block("BUILD SETUP");
        Info("Configuration:\t" + Configuration);
        Info("Version prefix:\t" + VersionPrefix);
        Info("Version suffix:\t" + VersionSuffix);
        Info("Tagged build:\t" + IsTaggedBuild);
    }

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
                .SetAssemblyVersion(VersionPrefix)
                .SetFileVersion(VersionPrefix)
                .SetInformationalVersion(VersionPrefix)
                .SetVersion(VersionPrefix)
                .SetVersionSuffix(VersionSuffix)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
            );
        });
}