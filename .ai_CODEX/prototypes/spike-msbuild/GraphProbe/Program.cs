using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;

var baseDir = AppContext.BaseDirectory;
var probeRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));

var msbuildRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "sdk", "10.0.103");
var sdksPath = Path.Combine(msbuildRoot, "Sdks");

Environment.SetEnvironmentVariable("MSBuildSDKsPath", sdksPath);
Environment.SetEnvironmentVariable("MSBuildExtensionsPath", msbuildRoot);
Environment.SetEnvironmentVariable("MSBuildExtensionsPath32", msbuildRoot);
Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(msbuildRoot, "MSBuild.dll"));

var baseProperties = new Dictionary<string, string>
{
    ["MSBuildSDKsPath"] = sdksPath,
    ["MSBuildExtensionsPath"] = msbuildRoot,
    ["MSBuildExtensionsPath32"] = msbuildRoot
};

var paths = new[]
{
    "SpikeLib/SpikeLib.csproj",
    "SpikeSolutionLegacy.sln",
    "SpikeSolution.slnx",
    "SpikeSolutionLegacy.slnx",
    "MultiLib/MultiLib.csproj"
};

foreach (var relative in paths)
{
    DumpGraph(relative, baseProperties);
}

DumpGraph("MultiLib/MultiLib.csproj [TargetFramework=net9.0]", Merge(baseProperties, "TargetFramework", "net9.0"));
DumpGraph("MultiLib/MultiLib.csproj [TargetFramework=net10.0]", Merge(baseProperties, "TargetFramework", "net10.0"));

void DumpGraph(string label, IDictionary<string, string> properties)
{
    var bracketIndex = label.IndexOf(" [", StringComparison.Ordinal);
    var relative = bracketIndex >= 0 ? label.Substring(0, bracketIndex) : label;
    var full = Path.GetFullPath(Path.Combine(probeRoot, relative));

    try
    {
        var graph = new ProjectGraph(full, properties, ProjectCollection.GlobalProjectCollection);
        Console.WriteLine($"OK: {label} -> nodes={graph.ProjectNodes.Count}");

        foreach (var node in graph.ProjectNodes)
        {
            var projectName = Path.GetFileName(node.ProjectInstance.FullPath);
            var tfm = node.ProjectInstance.GetPropertyValue("TargetFramework");
            var tfms = node.ProjectInstance.GetPropertyValue("TargetFrameworks");
            Console.WriteLine($"  node: {projectName}; TargetFramework='{tfm}'; TargetFrameworks='{tfms}'");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL: {label} -> {ex.GetType().Name}: {ex.Message}");
    }
}

Dictionary<string, string> Merge(IDictionary<string, string> source, string key, string value)
{
    var dict = source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    dict[key] = value;
    return dict;
}
