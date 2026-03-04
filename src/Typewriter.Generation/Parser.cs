using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Typewriter.CodeModel;
using Type = System.Type;

namespace Typewriter.Generation
{
    /// <summary>
    /// Parses a Typewriter template against a single code model context and produces the rendered output.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Parses the given <paramref name="template"/> using the specified <paramref name="context"/> and returns the rendered output.
        /// </summary>
        /// <param name="templatePath">Absolute path to the template file (used for diagnostics).</param>
        /// <param name="sourcePath">Absolute path to the source file being rendered.</param>
        /// <param name="template">The template text to parse.</param>
        /// <param name="extensions">Extension method types available for identifier resolution.</param>
        /// <param name="context">The code model object to render (e.g., a <see cref="CodeModel.File"/>).</param>
        /// <param name="success">Set to <see langword="true"/> if parsing completed without errors.</param>
        /// <param name="errorReporter">Optional callback invoked with a diagnostic message when a parse error occurs.</param>
        /// <returns>The rendered output, or <see langword="null"/> if no template identifiers matched the context.</returns>
        public static string? Parse(string templatePath, string sourcePath, string template, List<Type> extensions, object context, out bool success, Action<string>? errorReporter = null)
        {
            var instance = new Parser(extensions, errorReporter);
            var output = instance.ParseTemplate(templatePath, sourcePath, template, context);
            success = !instance.hasError;

            return instance.matchFound ? output : null;
        }

        private readonly List<Type> extensions;
        private readonly Action<string>? errorReporter;
        private bool matchFound;
        private bool hasError;

        private Parser(List<Type> extensions, Action<string>? errorReporter)
        {
            this.extensions = extensions;
            this.errorReporter = errorReporter;
        }

        private string? ParseTemplate(string templatePath, string sourcePath, string? template, object context)
        {
            if (string.IsNullOrEmpty(template))
            {
                return null;
            }

            var output = new StringBuilder();
            var stream = new Stream(template);

            while (stream.Advance())
            {
                if (ParseDollar(templatePath, sourcePath, stream, context, output))
                {
                    continue;
                }

                output.Append(stream.Current);
            }

            return output.ToString();
        }

        private bool ParseDollar(string templatePath, string sourcePath, Stream stream, object context, StringBuilder output)
        {
            if (stream.Current == '$')
            {
                var identifier = stream.PeekWord(1);

                if (TryGetIdentifier(templatePath, sourcePath, identifier, context, out var value))
                {
                    stream.Advance(identifier!.Length);

                    if (value is IEnumerable<Item> collection)
                    {
                        var filter = ParseBlock(stream, '(', ')');
                        var block = ParseBlock(stream, '[', ']');
                        var separator = ParseBlock(stream, '[', ']');

                        if (filter == null && block == null && separator == null)
                        {
                            var stringValue = value.ToString();

                            if (stringValue != null && !stringValue.Equals(value.GetType().FullName, StringComparison.OrdinalIgnoreCase))
                            {
                                output.Append(stringValue);
                            }
                            else
                            {
                                output.Append("$").Append(identifier);
                            }
                        }
                        else
                        {
                            IEnumerable<Item> items;
                            if (filter != null && filter.StartsWith("$", StringComparison.OrdinalIgnoreCase))
                            {
                                var predicate = filter.Remove(0, 1);
                                if (extensions != null)
                                {
                                    // Lambda filters are always defined in the first extension type
                                    var c = extensions.FirstOrDefault()?.GetMethod(predicate);
                                    if (c != null)
                                    {
                                        try
                                        {
                                            items = collection.Where(x => (bool)c.Invoke(null, new object[] { x })!).ToList();
                                            matchFound = matchFound || items.Any();
                                        }
                                        catch (Exception e)
                                        {
                                            items = Array.Empty<Item>();
                                            hasError = true;

                                            var message = $"Error rendering template. Cannot apply filter to identifier '{identifier}'.";
                                            LogException(e, message, templatePath, sourcePath);
                                        }
                                    }
                                    else
                                    {
                                        items = Array.Empty<Item>();
                                    }
                                }
                                else
                                {
                                    items = Array.Empty<Item>();
                                }
                            }
                            else
                            {
                                items = ItemFilter.Apply(collection, filter!, ref matchFound);
                            }

                            output.Append(string.Join(ParseTemplate(templatePath, sourcePath, separator, context),
                                items.Select(item => ParseTemplate(templatePath, sourcePath, block, item))));
                        }
                    }
                    else if (value is bool boolValue)
                    {
                        var trueBlock = ParseBlock(stream, '[', ']');
                        var falseBlock = ParseBlock(stream, '[', ']');

                        output.Append(ParseTemplate(templatePath, sourcePath, boolValue ? trueBlock : falseBlock, context));
                    }
                    else
                    {
                        var block = ParseBlock(stream, '[', ']');
                        if (value != null)
                        {
                            if (block != null)
                            {
                                output.Append(ParseTemplate(templatePath, sourcePath, block, value));
                            }
                            else
                            {
                                output.Append(value.ToString());
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private static string? ParseBlock(Stream stream, char open, char close)
        {
            if (stream.Peek() == open)
            {
                var block = stream.PeekBlock(2, open, close);

                stream.Advance(block.Length);
                stream.Advance(stream.Peek(2) == close ? 2 : 1);

                return block;
            }

            return null;
        }

        private bool TryGetIdentifier(string templatePath, string sourcePath, string? identifier, object context, out object? value)
        {
            value = null;

            if (identifier == null)
            {
                return false;
            }

            var type = context.GetType();

            try
            {
                var property = type.GetProperty(identifier);
                if (property != null)
                {
                    value = property.GetValue(context);
                    return true;
                }

                var extension = extensions.Select(e => e.GetMethod(identifier, new[] { type })).FirstOrDefault(m => m != null);
                if (extension != null)
                {
                    value = extension.Invoke(null, new[] { context });
                    return true;
                }
            }
            catch (Exception e)
            {
                hasError = true;

                var message = $"Error rendering template. Cannot get identifier '{identifier}'.";
                LogException(e, message, templatePath, sourcePath);
            }

            return false;
        }

        private void LogException(Exception exception, string message, string templatePath, string sourcePath)
        {
            // Skip the target invocation exception, get the real exception instead.
            if (exception is TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            var logMessage = $"{message} Error: {exception.Message}. Template: {templatePath}. Source path: {sourcePath}.{Environment.NewLine}{exception}";

            errorReporter?.Invoke(logMessage);
        }
    }
}
