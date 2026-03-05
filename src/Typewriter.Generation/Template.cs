using Typewriter.CodeModel.Configuration;
using Typewriter.Generation.Output;
using Typewriter.Metadata;
using File = Typewriter.CodeModel.File;
using Path = System.IO.Path;
using Type = System.Type;

namespace Typewriter.Generation;

/// <summary>
/// Represents a compiled Typewriter template, providing methods to render code model
/// files and write the generated output.
/// </summary>
/// <remarks>
/// Adapted from upstream <c>Typewriter.Generation.Template</c> with VS coupling removed:
/// <list type="bullet">
///   <item><c>EnvDTE.ProjectItem</c> replaced with <c>string</c> file paths.</item>
///   <item>Project mutation calls (<c>ProjectItem.Open</c>, <c>Window.Activate</c>,
///         <c>AddFromFile</c>, <c>projectItem.Document.Save</c>) removed.</item>
///   <item>Source-control checkout removed (CLI does not manage VS source control).</item>
///   <item>Output writing delegated to <see cref="IOutputWriter"/>.</item>
///   <item>Collision avoidance delegated to <see cref="IOutputPathPolicy"/>.</item>
///   <item>Windows registry long-path check removed (cross-platform).</item>
/// </list>
/// </remarks>
public class Template
{
    private readonly List<Type> _customExtensions = new();
    private readonly string _templatePath;
    private readonly string _solutionFullName;
    private readonly IOutputPathPolicy _outputPathPolicy;
    private readonly IOutputWriter _outputWriter;
    private readonly Compiler _compiler;
    private readonly Action<string>? _errorReporter;
    private Lazy<string?> _template;
    private Lazy<SettingsImpl> _configuration;
    private bool _templateCompileException;
    private bool _templateCompiled;

    /// <summary>Tracks output-path → source-path assignments for collision avoidance within a session.</summary>
    private readonly Dictionary<string, string> _outputMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the template settings.</summary>
    public Settings Settings => _configuration.Value;

    /// <summary>Gets the absolute path to the template file.</summary>
    public string TemplatePath => _templatePath;

    /// <summary>Gets a value indicating whether the template has been successfully compiled.</summary>
    public bool IsCompiled => _templateCompiled;

    /// <summary>Gets a value indicating whether template compilation threw an exception.</summary>
    public bool HasCompileException => _templateCompileException;

    /// <summary>
    /// Initializes a new instance of the <see cref="Template"/> class.
    /// </summary>
    /// <param name="templatePath">Absolute path to the <c>.tst</c> template file.</param>
    /// <param name="solutionFullName">Absolute path to the solution or project file.</param>
    /// <param name="outputPathPolicy">Policy for resolving output paths with collision avoidance.</param>
    /// <param name="outputWriter">Writer for persisting generated output to disk.</param>
    /// <param name="compiler">
    /// The <see cref="Compiler"/> instance used for template compilation, with per-invocation
    /// caching via <see cref="Performance.InvocationCache"/>.
    /// </param>
    /// <param name="errorReporter">Optional callback invoked with diagnostic messages on errors.</param>
    public Template(
        string templatePath,
        string solutionFullName,
        IOutputPathPolicy outputPathPolicy,
        IOutputWriter outputWriter,
        Compiler compiler,
        Action<string>? errorReporter = null)
    {
        _templatePath = templatePath;
        _solutionFullName = solutionFullName;
        _outputPathPolicy = outputPathPolicy;
        _outputWriter = outputWriter;
        _compiler = compiler;
        _errorReporter = errorReporter;
        _template = LazyTemplate();
        _configuration = LazyConfiguration();
    }

    /// <summary>
    /// Renders a single <see cref="File"/> using this template.
    /// </summary>
    /// <param name="file">The code model file to render.</param>
    /// <param name="success">Set to <see langword="true"/> when rendering completes without errors.</param>
    /// <returns>The rendered output, or <see langword="null"/> when no template identifiers matched.</returns>
    public string? Render(File file, out bool success)
    {
        try
        {
            var templateCode = _template.Value;
            if (templateCode == null)
            {
                success = false;
                return null;
            }

            return Parser.Parse(_templatePath, file.FullName, templateCode, _customExtensions, file, out success, _errorReporter);
        }
        catch (Exception ex)
        {
            _errorReporter?.Invoke($"{ex.Message} Template: {_templatePath}");
            success = false;
            return null;
        }
    }

    /// <summary>
    /// Renders multiple <see cref="File"/> instances into a single output (single-file mode).
    /// </summary>
    /// <param name="files">The code model files to render.</param>
    /// <param name="success">Set to <see langword="true"/> when rendering completes without errors.</param>
    /// <returns>The rendered output, or <see langword="null"/> when no template identifiers matched.</returns>
    public string? Render(File[] files, out bool success)
    {
        try
        {
            var templateCode = _template.Value;
            if (templateCode == null)
            {
                success = false;
                return null;
            }

            return SingleFileParser.Parse(_templatePath, files, templateCode, _customExtensions, out success, _errorReporter);
        }
        catch (Exception ex)
        {
            _errorReporter?.Invoke($"{ex.Message} Template: {_templatePath}");
            success = false;
            return null;
        }
    }

    /// <summary>
    /// Renders a single file and writes the output to disk.
    /// When rendering produces <see langword="null"/> output, the existing output file (if any) is deleted.
    /// </summary>
    /// <param name="file">The code model file to render.</param>
    /// <returns><see langword="true"/> when the render and write completed successfully.</returns>
    public bool RenderFile(File file)
    {
        var output = Render(file, out var success);

        if (success)
        {
            if (output == null)
            {
                DeleteFile(file.FullName);
            }
            else
            {
                SaveFile(file, output, ref success);
            }
        }

        return success;
    }

    /// <summary>
    /// Renders multiple files into a single output file and writes it to disk (single-file mode).
    /// </summary>
    /// <param name="files">The code model files to render.</param>
    /// <returns><see langword="true"/> when the render and write completed successfully.</returns>
    public bool RenderFile(File[] files)
    {
        var output = Render(files, out var success);

        if (success && output != null)
        {
            var outputDir = GetOutputDirectory();
            var singleFileName = _configuration.Value.SingleFileName;

            if (!string.IsNullOrEmpty(singleFileName))
            {
                var outputPath = Path.Combine(outputDir, singleFileName);
                _outputWriter.WriteAsync(outputPath, output, _configuration.Value.Utf8BomGeneration, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
        }

        return success;
    }

    /// <summary>
    /// Deletes the output file associated with the given source file path.
    /// </summary>
    /// <param name="sourcePath">Absolute path of the source <c>.cs</c> file whose output should be deleted.</param>
    public void DeleteFile(string sourcePath)
    {
        var entry = _outputMap
            .FirstOrDefault(kvp => string.Equals(kvp.Value, sourcePath, StringComparison.OrdinalIgnoreCase));

        if (entry.Key != null && System.IO.File.Exists(entry.Key))
        {
            System.IO.File.Delete(entry.Key);
            _outputMap.Remove(entry.Key);
        }
    }

    /// <summary>
    /// Renames the output file when the corresponding source file is renamed.
    /// </summary>
    /// <param name="file">The code model file at the new path.</param>
    /// <param name="oldPath">The previous absolute path of the source file.</param>
    /// <param name="newPath">The new absolute path of the source file.</param>
    public void RenameFile(File file, string oldPath, string newPath)
    {
        var entry = _outputMap
            .FirstOrDefault(kvp => string.Equals(kvp.Value, oldPath, StringComparison.OrdinalIgnoreCase));

        if (entry.Key == null)
        {
            return;
        }

        // If only the source file content changed (same filename), just update the map.
        if (Path.GetFileName(oldPath)?.Equals(Path.GetFileName(newPath), StringComparison.OrdinalIgnoreCase) ?? false)
        {
            _outputMap[entry.Key] = newPath;
            return;
        }

        var newOutputPath = GetOutputPath(file);

        if (!string.Equals(entry.Key, newOutputPath, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(entry.Key))
        {
            System.IO.File.Move(entry.Key, newOutputPath);
            _outputMap.Remove(entry.Key);
            _outputMap[newOutputPath] = newPath;
        }
    }

    /// <summary>
    /// Resets the template, forcing recompilation and reconfiguration on the next render.
    /// </summary>
    public void Reload()
    {
        _template = LazyTemplate();
        _configuration = LazyConfiguration();
    }

    // ---- Lazy initialisation ----

    private Lazy<SettingsImpl> LazyConfiguration()
    {
        return new Lazy<SettingsImpl>(() =>
        {
            var settings = new SettingsImpl(Path.GetFullPath(_templatePath), _solutionFullName);

            if (!_template.IsValueCreated)
            {
                // Force template init so _customExtensions is populated.
                _ = _template.Value;
            }

            var templateClass = _customExtensions.FirstOrDefault();
            if (templateClass?.GetConstructor([typeof(Settings)]) != null)
            {
                Activator.CreateInstance(templateClass, settings);
            }

            return settings;
        });
    }

    private Lazy<string?> LazyTemplate()
    {
        _templateCompiled = false;
        _templateCompileException = false;

        return new Lazy<string?>(() =>
        {
            var code = System.IO.File.ReadAllText(_templatePath);
            try
            {
                var result = TemplateCodeParser.Parse(_templatePath, code, _customExtensions, _compiler);
                _templateCompiled = true;
                return result;
            }
            catch (Exception)
            {
                _templateCompileException = true;
                throw;
            }
        });
    }

    // ---- Output path resolution ----

    private void SaveFile(File file, string output, ref bool success)
    {
        var outputPath = GetOutputPath(file);

        if (string.Equals(file.FullName, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            _errorReporter?.Invoke("Output filename cannot match source filename.");
            success = false;
            return;
        }

        // IOutputWriter handles skip-if-unchanged, directory creation, and BOM.
        _outputWriter.WriteAsync(outputPath, output, _configuration.Value.Utf8BomGeneration, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resolves a unique output path for the given file, handling collisions when multiple
    /// source files produce identically-named outputs.
    /// </summary>
    /// <remarks>
    /// When settings provide a custom <see cref="SettingsImpl.OutputFilenameFactory"/> or
    /// <see cref="Settings.OutputDirectory"/>, the path is computed from those settings with
    /// inline collision suffixing. Otherwise the resolution is delegated entirely to
    /// <see cref="IOutputPathPolicy"/>.
    /// </remarks>
    private string GetOutputPath(File file)
    {
        var sourcePath = file.FullName;
        var hasCustomPath = !string.IsNullOrEmpty(_configuration.Value.OutputDirectory)
                            || _configuration.Value.OutputFilenameFactory != null;

        for (var collisionIndex = 0; collisionIndex < 1000; collisionIndex++)
        {
            string outputPath;

            if (hasCustomPath)
            {
                var directory = GetOutputDirectory();
                var filename = GetOutputFilename(file, sourcePath);

                if (collisionIndex > 0)
                {
                    filename = ApplyCollisionSuffix(filename, collisionIndex);
                }

                outputPath = Path.Combine(directory, filename);
            }
            else
            {
                // Delegate to IOutputPathPolicy for default path + collision suffix.
                outputPath = _outputPathPolicy.Resolve(_templatePath, sourcePath, collisionIndex);
            }

            if (!_outputMap.TryGetValue(outputPath, out var mappedSource))
            {
                _outputMap[outputPath] = sourcePath;
                return outputPath;
            }

            if (string.Equals(mappedSource, sourcePath, StringComparison.OrdinalIgnoreCase))
            {
                return outputPath;
            }
        }

        throw new InvalidOperationException($"Cannot resolve unique output path for '{sourcePath}'.");
    }

    /// <summary>
    /// Appends a collision suffix (<c>_1</c>, <c>_2</c>, …) before the file extension,
    /// matching the convention used by <see cref="IOutputPathPolicy"/>.
    /// Handles <c>.d.ts</c> compound extensions.
    /// </summary>
    private static string ApplyCollisionSuffix(string filename, int collisionIndex)
    {
        int dotIndex;
        if (filename.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase))
        {
            dotIndex = filename.Length - 5;
        }
        else
        {
            dotIndex = filename.LastIndexOf('.');
        }

        if (dotIndex > 0)
        {
            var baseName = filename[..dotIndex];
            var extension = filename[dotIndex..];
            return $"{baseName}_{collisionIndex}{extension}";
        }

        return $"{filename}_{collisionIndex}";
    }

    private string GetOutputDirectory()
    {
        var directory = _configuration.Value.OutputDirectory;
        if (string.IsNullOrEmpty(directory))
        {
            return Path.GetDirectoryName(_templatePath) ?? string.Empty;
        }

        var templateDirectory = Path.GetDirectoryName(_templatePath);

        if (!string.IsNullOrEmpty(templateDirectory) && !Path.IsPathRooted(directory))
        {
            directory = Path.Combine(templateDirectory, directory);
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return directory;
    }

    private string GetOutputFilename(File file, string sourcePath)
    {
        var sourceFilename = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = GetOutputExtension();

        try
        {
            if (_configuration.Value.OutputFilenameFactory != null)
            {
                var filename = _configuration.Value.OutputFilenameFactory(file);

                filename = filename
                    .Replace("<", "-")
                    .Replace(">", "-")
                    .Replace(":", "-")
                    .Replace("\"", "-")
                    .Replace("|", "-")
                    .Replace("?", "-")
                    .Replace("*", "-");

                if (!filename.Contains('.'))
                {
                    filename += extension;
                }

                return filename;
            }
        }
        catch (Exception exception)
        {
            _errorReporter?.Invoke($"Can't get output filename for '{sourcePath}' ({exception.Message})");
        }

        return sourceFilename + extension;
    }

    private string GetOutputExtension()
    {
        var extension = _configuration.Value.OutputExtension;

        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".ts";
        }

        return "." + extension.Trim('.');
    }
}
