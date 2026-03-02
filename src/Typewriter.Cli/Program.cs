using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Typewriter.Application;
using Typewriter.Cli;

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

generateCommand.SetHandler(async (InvocationContext ctx) =>
{
    var pr = ctx.ParseResult;
    var options = new GenerateCommandOptions
    {
        Templates      = pr.GetValueForArgument(templatesArg),
        Solution       = pr.GetValueForOption(solutionOpt),
        Project        = pr.GetValueForOption(projectOpt),
        Framework      = pr.GetValueForOption(frameworkOpt),
        Configuration  = pr.GetValueForOption(configurationOpt),
        Runtime        = pr.GetValueForOption(runtimeOpt),
        Restore        = pr.GetValueForOption(restoreOpt),
        Output         = pr.GetValueForOption(outputOpt),
        Verbosity      = pr.GetValueForOption(verbosityOpt),
        FailOnWarnings = pr.GetValueForOption(failOnWarningsOpt),
    };

    var reporter = new ConsoleDiagnosticReporter();
    var runner = new ApplicationRunner();
    ctx.ExitCode = await runner.RunAsync(options, reporter, ctx.GetCancellationToken());
});

rootCommand.AddCommand(generateCommand);

// ---- parse and invoke ----
// Build the parser with defaults (--help, --version, parse-error reporting).
var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build();

// Map parse errors to exit code 2 before System.CommandLine's default (exit 1).
var parseResult = parser.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
        Console.Error.WriteLine(error.Message);
    return 2;
}

return await parser.InvokeAsync(args);
