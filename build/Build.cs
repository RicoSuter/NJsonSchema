using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
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

    [Solution(GenerateProjects = true)] readonly Solution Solution;
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
            Serilog.Log.Information("Tag version {VersionPrefix} from Git found, using it as version prefix", versionPrefix);
        }
        else
        {
            var propsDocument = XDocument.Parse((RootDirectory / "Directory.Build.props").ReadAllText());
            versionPrefix = propsDocument.Element("Project").Element("PropertyGroup").Element("VersionPrefix").Value;
            Serilog.Log.Information("Version prefix {VersionPrefix} read from Directory.Build.props", versionPrefix);
        }

        return versionPrefix;
    }

    protected override void OnBuildInitialized()
    {
        VersionPrefix = DetermineVersionPrefix();

        var versionParts = VersionPrefix.Split('-');
        if (versionParts.Length == 2)
        {
            VersionPrefix = versionParts[0];
            VersionSuffix = versionParts[1];
        }
        else
        {
            VersionSuffix = !IsTaggedBuild
                ? $"preview-{DateTime.UtcNow:yyyyMMdd-HHmm}"
                : "";
        }

        if (IsLocalBuild)
        {
            VersionSuffix = $"dev-{DateTime.UtcNow:yyyyMMdd-HHmm}";
        }

        Serilog.Log.Information("BUILD SETUP");
        Serilog.Log.Information("Configuration:\t {Configuration}" , Configuration);
        Serilog.Log.Information("Version prefix:\t {VersionPrefix}" , VersionPrefix);
        Serilog.Log.Information("Version suffix:\t {VersionSuffix}" , VersionSuffix);
        Serilog.Log.Information("Tagged build:\t {IsTaggedBuild}" , IsTaggedBuild);
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
            );

            if (IsServerBuild)
            {
                NpmTasks.NpmCi(_ => _
                    .SetProcessWorkingDirectory(Solution._2_CodeGeneration.NJsonSchema_CodeGeneration_TypeScript_Tests.Directory)
                );
            }
            else
            {
                NpmTasks.NpmInstall(_ => _
                    .SetProcessWorkingDirectory(Solution._2_CodeGeneration.NJsonSchema_CodeGeneration_TypeScript_Tests.Directory)
                );
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetAssemblyVersion(VersionPrefix)
                .SetFileVersion(VersionPrefix)
                .SetInformationalVersion(VersionPrefix)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetDeterministic(IsServerBuild)
                .SetContinuousIntegrationBuild(IsServerBuild)
                // ensure we don't generate too much output in CI run
                // 0  Turns off emission of all warning messages
                // 1  Displays severe warning messages
                .SetWarningLevel(IsServerBuild ? 0 : 1)
            );
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .When(GitHubActions.Instance is not null, x => x.SetLoggers("GitHubActions"))
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

            var nugetVersion = VersionPrefix;
            if (!string.IsNullOrWhiteSpace(VersionSuffix))
            {
                nugetVersion += "-" + VersionSuffix;
            }

            ArtifactsDirectory.CreateOrCleanDirectory();

            DotNetPack(s => s
                .SetProcessWorkingDirectory(SourceDirectory)
                .SetAssemblyVersion(VersionPrefix)
                .SetFileVersion(VersionPrefix)
                .SetInformationalVersion(VersionPrefix)
                .SetVersion(nugetVersion)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetDeterministic(IsServerBuild)
                .SetContinuousIntegrationBuild(IsServerBuild)
            );
        });
}
