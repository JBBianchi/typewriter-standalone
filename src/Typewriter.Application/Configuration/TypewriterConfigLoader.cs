using System.Text.Json;

namespace Typewriter.Application.Configuration;

/// <summary>
/// Locates and deserializes the nearest <c>typewriter.json</c> by walking upward
/// from a starting directory until the file is found or a <c>.git</c> boundary is reached.
/// </summary>
public static class TypewriterConfigLoader
{
    private const string ConfigFileName = "typewriter.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Walks upward from <paramref name="startDirectory"/> looking for <c>typewriter.json</c>.
    /// Stops at the first file found or when a <c>.git</c> directory is encountered (repo root).
    /// </summary>
    /// <param name="startDirectory">Directory to begin the upward search from.</param>
    /// <returns>Deserialized <see cref="TypewriterConfig"/>, or <c>null</c> if not found.</returns>
    public static TypewriterConfig? Load(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current is not null)
        {
            var configFile = Path.Combine(current.FullName, ConfigFileName);
            if (File.Exists(configFile))
            {
                return Deserialize(configFile);
            }

            // Stop at repo root boundary.
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                break;
            }

            current = current.Parent;
        }

        return null;
    }

    private static TypewriterConfig? Deserialize(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<TypewriterConfig>(json, _jsonOptions);
    }
}
