using System.Text;

namespace Typewriter.Generation.Lexing;

/// <summary>
/// Character-by-character stream for template parsing.
/// Ported as-is from upstream <c>Typewriter.TemplateEditor.Lexing.Stream</c>.
/// </summary>
internal class Stream
{
    private readonly int offset;
    private readonly string template;
    private int position = -1;
    private char current = char.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Stream"/> class.
    /// </summary>
    /// <param name="template">The template text to parse.</param>
    /// <param name="offset">Starting offset for position tracking.</param>
    public Stream(string template, int offset = 0)
    {
        this.offset = offset;
        this.template = template ?? string.Empty;
    }

    /// <summary>Gets the current absolute position in the template.</summary>
    public int Position => position + offset;

    /// <summary>Gets the character at the current position.</summary>
    public char Current => current;

    /// <summary>
    /// Advances the stream by <paramref name="offset"/> characters.
    /// </summary>
    /// <param name="offset">Number of characters to advance.</param>
    /// <returns><c>true</c> if the stream has not reached the end.</returns>
    public bool Advance(int offset = 1)
    {
        for (var i = 0; i < offset; i++)
        {
            position++;

            if (position >= template.Length)
            {
                current = char.MinValue;
                return false;
            }

            current = template[position];
        }

        return true;
    }

    /// <summary>
    /// Peeks at a character relative to the current position.
    /// </summary>
    /// <param name="offset">Offset from the current position.</param>
    /// <returns>The character at the offset, or <see cref="char.MinValue"/> if out of bounds.</returns>
    public char Peek(int offset = 1)
    {
        var index = position + offset;

        if (index > -1 && index < template.Length)
        {
            return template[index];
        }

        return char.MinValue;
    }

    /// <summary>
    /// Peeks at the next word (sequence of letters/digits) starting at the given offset.
    /// </summary>
    /// <param name="start">Offset from the current position to start reading.</param>
    /// <returns>The word, or <c>null</c> if no letter is found at the start position.</returns>
    public string? PeekWord(int start = 0)
    {
        if (!char.IsLetter(Peek(start)))
        {
            return null;
        }

        var identifier = new StringBuilder();
        var i = start;
        while (char.IsLetterOrDigit(Peek(i)))
        {
            identifier.Append(Peek(i));
            i++;
        }

        return identifier.ToString();
    }

    /// <summary>
    /// Peeks at the remainder of the current line starting at the given offset.
    /// </summary>
    /// <param name="start">Offset from the current position to start reading.</param>
    /// <returns>The line content including trailing newline.</returns>
    public string PeekLine(int start = 0)
    {
        var line = new StringBuilder();
        var i = start;
        do
        {
            line.Append(Peek(i));
            i++;
        } while (Peek(i) != '\n' && i + position < template.Length);

        line.Append('\n');

        return line.ToString();
    }

    /// <summary>
    /// Peeks at a balanced block of text between matching <paramref name="open"/> and <paramref name="close"/> characters.
    /// </summary>
    /// <param name="start">Offset from the current position to start reading.</param>
    /// <param name="open">The opening delimiter character.</param>
    /// <param name="close">The closing delimiter character.</param>
    /// <returns>The block content between delimiters.</returns>
    public string PeekBlock(int start, char open, char close)
    {
        var i = start;
        var depth = 1;
        var identifier = new StringBuilder();

        while (depth > 0)
        {
            var letter = Peek(i);

            if (letter == char.MinValue)
            {
                break;
            }

            if (IsMatch(i, letter, close))
            {
                depth--;
            }

            if (depth > 0)
            {
                identifier.Append(letter);
                if (IsMatch(i, letter, open))
                {
                    depth++;
                }

                i++;

                if (letter != open && (letter == '"' || letter == '\''))
                {
                    var block = PeekBlock(i, letter, letter);
                    identifier.Append(block);
                    i += block.Length;

                    if (letter == Peek(i))
                    {
                        identifier.Append(letter);
                        i++;
                    }
                }
            }
        }

        return identifier.ToString();
    }

    private bool IsMatch(int index, char letter, char match)
    {
        if (letter == match)
        {
            var isString = match == '"' || match == '\'';
            if (isString)
            {
                if (Peek(index - 1) == '\\' && Peek(index - 2) != '\\')
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Skips whitespace characters from the current position.
    /// </summary>
    /// <returns><c>true</c> if the stream has not reached the end.</returns>
    public bool SkipWhitespace()
    {
        if (position < 0)
        {
            Advance();
        }

        while (char.IsWhiteSpace(Current))
        {
            Advance();
        }

        return position < template.Length;
    }
}
