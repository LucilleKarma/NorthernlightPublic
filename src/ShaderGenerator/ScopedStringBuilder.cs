using System.Text;

namespace ShaderGenerator;

public sealed class ScopedStringBuilder
{
    public readonly ref struct BraceScope : IDisposable
    {
        private readonly ScopedStringBuilder stringBuilder;

        private readonly bool includeBraces;

        private readonly bool includeSemicolon;

        public BraceScope(ScopedStringBuilder stringBuilder, bool includeBraces = true, bool includeSemicolon = false)
        {
            this.stringBuilder = stringBuilder;
            this.includeBraces = includeBraces;
            this.includeSemicolon = includeSemicolon;

            if (includeBraces)
                stringBuilder.Write("{");
            stringBuilder.IndentNesting++;
        }

        public void Dispose()
        {
            stringBuilder.IndentNesting--;
            if (includeBraces)
                stringBuilder.Write("}", newline: !includeSemicolon);
            if (includeSemicolon)
                stringBuilder.Builder.Append(';');
        }
    }

    /// <summary>
    ///     The current amount of indent nesting active.
    /// </summary>
    public int IndentNesting
    {
        get;
        set;
    }

    /// <summary>
    ///     The underlying string builder.
    /// </summary>
    public StringBuilder Builder
    {
        get;
    } = new StringBuilder();

    /// <summary>
    ///     The text indentation used for
    ///     lines.
    /// </summary>
    public const string Indent = "    ";

    /// <summary>
    ///     Writes a given string to the builder, taking into account
    ///     the current indent nesting by default.
    /// </summary>
    public void Write(string text = "", bool newline = true, bool skipIndent = false)
    {
        if (!skipIndent)
        {
            for (int i = 0; i < IndentNesting; i++)
                Builder.Append(Indent);
        }

        if (newline)
            Builder.AppendLine(text);
        else
            Builder.Append(text);
    }

    /// <summary>
    ///     Generates a new brace scope for use with this builder.
    /// </summary>
    public BraceScope IndentScope(bool includeBraces = true, bool includeSemicolon = false) => new BraceScope(this, includeBraces, includeSemicolon);

    public override string ToString() => Builder.ToString();
}
