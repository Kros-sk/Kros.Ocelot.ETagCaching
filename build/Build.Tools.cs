using Nuke.Common.Tooling;

partial class Build
{
    const string DotnetAffectedToolName = "dotnet-affected";
    const int NoAffectedProjectsFoundExitCode = 166; // dotnet-affected returns this when there are no projects affected.

    [PathVariable(DotnetAffectedToolName)]
    readonly Tool DotnetAffected;

    [PathVariable("az")]
    readonly Tool Az;

    [PathVariable("docker")]
    readonly Tool Docker;

    [NuGetPackage("Tyrannoport", "Tyrannoport.dll")]
    readonly Tool Tyrannoport;
}
