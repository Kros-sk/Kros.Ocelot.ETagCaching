using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
            TestResultsDirectory.CreateOrCleanDirectory();
            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution)
                .AddLoggers("trx")
                .SetResultsDirectory(TestResultsDirectory)
                .When(AffectedOnly, ss => ss.SetProjectFile(AffectedProjectPath))
                .EnableNoBuild());
        });

    Target PrintTestResults => _ => _
        .TriggeredBy(Test)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            ProcessTestResults(TestResultsDirectory);
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
            var source = NugetApiUrl;
            var targetPath = "*.nupkg";
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

    void ProcessTestResults(string directory)
    {
        var trxFiles = Directory.GetFiles(directory, "*.trx");
        foreach (var file in trxFiles)
        {
            var xDocument = XDocument.Load(file);
            var failedTestResults = xDocument.Descendants()
                .Where(x => x.Name.LocalName == "UnitTestResult" && x.Attribute("outcome").Value != "Passed")
                .Select(result => new
                {
                    FullTestName = result.Attribute("testName").Value,
                    Outcome = result.Attribute("outcome").Value,
                    ErrorMessage = result.Descendants()
                        .FirstOrDefault(x => x.Name.LocalName == "Message")?
                        .Value[..Math.Min(100, result.Descendants()
                            .FirstOrDefault(x => x.Name.LocalName == "Message")?.Value.Length ?? 0)] + "..."
                })
                .ToList();

            var groupedByTestClass = failedTestResults
                .GroupBy(test => test.FullTestName[..test.FullTestName.LastIndexOf('.')])
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var testClass in groupedByTestClass)
            {
                AnsiConsole.MarkupLine($"\n📂 [blue][underline]{testClass.Key}[/][/]");

                foreach (var test in testClass.Value)
                {
                    var testName = test.FullTestName[(test.FullTestName.LastIndexOf('.') + 1)..];
                    AnsiConsole.MarkupLine($"🧪 [red]{testName} ✖[/] [grey]{test.ErrorMessage}[/]");
                }
            }

            var passedTests = xDocument.Descendants()
                .Count(x => x.Name.LocalName == "UnitTestResult" && x.Attribute("outcome").Value == "Passed");
            var failedTests = failedTestResults.Count;
            var totalTests = passedTests + failedTests;

            AnsiConsole.MarkupLine($"\n📊 [blue]Test results[/]");
            var barChart = new BreakdownChart()
                .Width(60).AddItem("Passed", passedTests, Color.Green).AddItem("Failed", failedTests, Color.Red);

            AnsiConsole.Write(barChart);
        }
    }
}
