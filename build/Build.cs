using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using System;
using System.IO;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Solution]
    readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Output directory for test results.")]
    readonly AbsolutePath TestResultsDirectory = RootDirectory / "TestResults";

    [Parameter("Nuget API Url")]
    readonly string NugetApiUrl = "https://www.nuget.org/api/v2/";

    [Parameter("Nuget API Key for this source")]
    readonly string NugetApiKey = "AZ";

    [Parameter("Build/test only projects affected by changes between --affected-from and --affected-to.")]
    bool AffectedOnly;

    [Parameter("A branch or commit to compare against --affected-to when using --affected-only.")]
    readonly string AffectedFrom;

    [Parameter("A branch or commit to compare against --affected-from when using --affected-only.")]
    readonly string AffectedTo;

    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath TestsDirectory => RootDirectory / "tests";
    static AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .When(AffectedOnly, ss => ss.SetProjectFile(AffectedProjectPath)));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .After(AffectedProjects)
        .Executes(() =>
        {
            CheckAffectedProjects(nameof(Build));
            DotNetBuild(s => s
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution)
                .AddProperty("TreatWarningsAsErrors", true)
                .When(AffectedOnly, ss => ss.SetProjectFile(AffectedProjectPath))
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution)
                .AddLoggers("trx")
                .SetResultsDirectory(TestResultsDirectory)
                .When(AffectedOnly, ss => ss.SetProjectFile(AffectedProjectPath))
                .EnableNoBuild());
        });

    Target Pack => _ => _
        .After(Test)
        .Produces(OutputDirectory / "*.nupkg")
        .Executes(() =>
        {
            SourceDirectory.GlobFiles("**/*.csproj").ForEach(projectPath =>
                DotNetPack(s => s
                    .SetConfiguration(Configuration)
                    .SetProject(projectPath)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetNoDependencies(true)
                    .SetOutputDirectory(OutputDirectory)
                    .When(IsDebug, ss => ss.SetVersionSuffix(Environment.MachineName))));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            string source = NugetApiUrl;
            string targetPath = "*.nupkg";
            if (IsDebug)
            {
                source = @"C:\LocalNuget";
                targetPath = $"*{Environment.MachineName}.nupkg";
                ((AbsolutePath)source).CreateDirectory();
            }

            DotNetNuGetPush(s => s
                .SetSource(source)
                .SetApiKey(NugetApiKey)
                .SetSkipDuplicate(true)
                .SetTargetPath(OutputDirectory / targetPath));
        });

    const string AffectedProjectName = "affected";
    const string AffectedProjectFullName = AffectedProjectName + ".proj";
    static AbsolutePath AffectedProjectPath => RootDirectory / AffectedProjectFullName;

    Target AffectedProjects => _ => _
        .Executes(OnAffectedProjects);

    void OnAffectedProjects()
    {
        ArgumentStringHandler args = new();
        args.AppendParam("--solution-path", Solution);
        args.AppendParam("--output-dir", RootDirectory);
        args.AppendParam("--output-name", AffectedProjectName);
        args.AppendParam("--verbose");
        args.AppendParamIfNotNull("--from", AffectedFrom);
        args.AppendParamIfNotNull("--to", AffectedTo);
        try
        {
            DotnetAffected(args, Solution.Directory);
            Log.Information("");
            Log.Information("Generated affected project file: {AffectedProjectPath}", AffectedProjectPath);
            Log.Information(File.ReadAllText(AffectedProjectFullName));
        }
        catch (ProcessException ex)
        {
            if (ex.ExitCode == NoAffectedProjectsFoundExitCode)
            {
                Log.Information("{DotnetAffectedToolName} did not find any affected projects."
                                        + " Empty {AffectedProjectFullName} file will be created",
                    DotnetAffectedToolName, AffectedProjectFullName);
                File.WriteAllText(AffectedProjectPath, "<Project Sdk=\"Microsoft.Build.Traversal/3.0.3\">\r\n</Project>");
            }
            else
            {
                throw;
            }
        }
    }

    void CheckAffectedProjects(string targetName)
    {
        if (AffectedOnly && !ExecutionPlan.Any(t => t.Name == nameof(AffectedProjects)))
        {
            Log.Information(
                "Parameter '--affected-only' was specified, but target 'affected-projects' is not in execution plan.");
            Log.Information(
                "The {TargetName} target will fail, if project file {AffectedProjectFullName} does not exist.",
                targetName, AffectedProjectFullName);
        }
    }

    bool IsDebug => Configuration == Configuration.Debug;
}
