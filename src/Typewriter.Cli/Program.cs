using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Loading.MSBuild;

var rootCommand = new RootCommand("typewriter-cli \u2014 standalone Typewriter code generator");

// ---- generate subcommand ----
var generateCommand = new Command("generate", "Generate TypeScript files from .tst templates");

var templatesArg = new Argument<string[]>("templates", "One or more glob patterns for template files (.tst)")
{
    Arity = ArgumentArity.OneOrMore,
};

var solutionOpt      = new Option<string?>("--solution",         "Path to the .sln, .slnx, or project file");
var projectOpt       = new Option<string?>("--project",          "Path to a specific .csproj to scope the run");
var frameworkOpt     = new Option<string?>("--framework",        "Target framework moniker (TFM) to select");
var configurationOpt = new Option<string?>("--configuration",    "MSBuild configuration (e.g. Debug, Release)");
var runtimeOpt       = new Option<string?>("--runtime",          "Runtime identifier (RID) for restore/build");
var restoreOpt       = new Option<bool>   ("--restore",          "Run dotnet restore before loading");
var outputOpt        = new Option<string?>("--output",           "Override default output directory for generated files");
var verbosityOpt     = new Option<string?>("--verbosity",        "Diagnostic verbosity: quiet|minimal|normal|detailed|diagnostic");
var failOnWarningsOpt = new Option<bool>  ("--fail-on-warnings", "Exit with code 1 when warnings are emitted");
var dryRunOpt         = new Option<bool>  ("--dry-run",          "Validate the pipeline without writing output files");

generateCommand.AddArgument(templatesArg);
generateCommand.AddOption(solutionOpt);
generateCommand.AddOption(projectOpt);
generateCommand.AddOption(frameworkOpt);
generateCommand.AddOption(configurationOpt);
generateCommand.AddOption(runtimeOpt);
generateCommand.AddOption(restoreOpt);
generateCommand.AddOption(outputOpt);
generateCommand.AddOption(verbosityOpt);
generateCommand.AddOption(failOnWarningsOpt);
generateCommand.AddOption(dryRunOpt);

generateCommand.SetHandler(async (InvocationContext ctx) =>
{
    var pr = ctx.ParseResult;
    var options = GenerateCommandOptions.Merge(
        config:        null, // typewriter.json loaded by ApplicationRunner (T016)
        templates:     pr.GetValueForArgument(templatesArg),
        solution:      pr.GetValueForOption(solutionOpt),
        project:       pr.GetValueForOption(projectOpt),
        framework:     pr.GetValueForOption(frameworkOpt),
        configuration: pr.GetValueForOption(configurationOpt),
        runtime:       pr.GetValueForOption(runtimeOpt),
        restore:       pr.GetValueForOption(restoreOpt),
        output:        pr.GetValueForOption(outputOpt),
        verbosity:     pr.GetValueForOption(verbosityOpt),
        failOnWarnings: pr.GetValueForOption(failOnWarningsOpt),
        dryRun:        pr.GetValueForOption(dryRunOpt));

    var reporter = new MsBuildDiagnosticReporter();

    // Compose the loading pipeline services.
    var cache = new InvocationCache();
    var locatorService = new MsBuildLocatorService();
    var inputResolver = new InputResolver();
    var restoreService = new RestoreService();
    var solutionFallbackService = new SolutionFallbackService();
    var projectGraphService = new ProjectGraphService(locatorService, solutionFallbackService);
    var roslynWorkspaceService = new RoslynWorkspaceService(cache);

    var outputWriter = new OutputWriter();
    var outputPathPolicy = new OutputPathPolicy();
    var runner = new ApplicationRunner(inputResolver, restoreService, projectGraphService, roslynWorkspaceService, outputWriter, outputPathPolicy, cache);
    ctx.ExitCode = await runner.RunAsync(options, reporter, ctx.GetCancellationToken());
});

rootCommand.AddCommand(generateCommand);

// Set a root handler so System.CommandLine middleware (--version, --help)
// can short-circuit before "Required command was not provided" validation.
rootCommand.SetHandler(() => { });

// ---- parse and invoke ----
// Build the parser with defaults (--help, --version, parse-error reporting).
var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build();

// When invoked with no arguments, show help and exit successfully.
if (args.Length == 0)
{
    await parser.InvokeAsync(["--help"]);
    return 0;
}

// Map parse errors to exit code 2 before System.CommandLine's default (exit 1).
var parseResult = parser.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
        Console.Error.WriteLine(error.Message);
    return 2;
}

return await parser.InvokeAsync(args);
