using System.Globalization;

namespace MdViewer.Core.Highlighting;

public enum TokenKind
{
    Plain,
    Keyword,
    String,
    Number,
    Comment,
    Type,
    Function,
    Punctuation,
    Property,
    Tag,
    Attribute,
}

public readonly record struct Token(TokenKind Kind, int Start, int Length);

public enum LanguageMode
{
    None,
    CLike,
    Python,
    Shell,
    PowerShell,
    Sql,
    Json,
    Yaml,
    Markup,
    Css,
    Markdown,
    Diff,
    Config,
}

/// <summary>Carry-over state between the lines of one block.</summary>
public sealed class HighlightState
{
    public bool BlockComment { get; set; }

    public string? BlockCommentEnd { get; set; }

    public bool TripleString { get; set; }
}

/// <summary>
/// Small, dependency-free syntax highlighter for the languages that actually
/// show up in documentation. Unknown languages render as plain text — never an
/// error, which is what MD.Scribe does too.
/// </summary>
public static class SyntaxHighlighter
{
    private static readonly Dictionary<string, LanguageMode> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c"] = LanguageMode.CLike, ["h"] = LanguageMode.CLike, ["cpp"] = LanguageMode.CLike,
        ["c++"] = LanguageMode.CLike, ["cxx"] = LanguageMode.CLike, ["hpp"] = LanguageMode.CLike,
        ["cs"] = LanguageMode.CLike, ["csharp"] = LanguageMode.CLike, ["java"] = LanguageMode.CLike,
        ["js"] = LanguageMode.CLike, ["javascript"] = LanguageMode.CLike, ["ts"] = LanguageMode.CLike,
        ["typescript"] = LanguageMode.CLike, ["jsx"] = LanguageMode.CLike, ["tsx"] = LanguageMode.CLike,
        ["go"] = LanguageMode.CLike, ["golang"] = LanguageMode.CLike, ["rust"] = LanguageMode.CLike,
        ["php"] = LanguageMode.CLike, ["swift"] = LanguageMode.CLike, ["kotlin"] = LanguageMode.CLike,
        ["scala"] = LanguageMode.CLike, ["dart"] = LanguageMode.CLike, ["jsonc"] = LanguageMode.CLike,
        ["mermaid"] = LanguageMode.None, ["text"] = LanguageMode.None, ["plaintext"] = LanguageMode.None,
        ["py"] = LanguageMode.Python, ["python"] = LanguageMode.Python, ["python3"] = LanguageMode.Python,
        ["sh"] = LanguageMode.Shell, ["bash"] = LanguageMode.Shell, ["shell"] = LanguageMode.Shell,
        ["zsh"] = LanguageMode.Shell, ["console"] = LanguageMode.Shell, ["shell-session"] = LanguageMode.Shell,
        ["ps1"] = LanguageMode.PowerShell, ["powershell"] = LanguageMode.PowerShell, ["pwsh"] = LanguageMode.PowerShell,
        ["sql"] = LanguageMode.Sql, ["mysql"] = LanguageMode.Sql, ["postgresql"] = LanguageMode.Sql,
        ["pgsql"] = LanguageMode.Sql, ["sqlite"] = LanguageMode.Sql, ["tsql"] = LanguageMode.Sql,
        ["json"] = LanguageMode.Json, ["json5"] = LanguageMode.Json,
        ["yaml"] = LanguageMode.Yaml, ["yml"] = LanguageMode.Yaml,
        ["html"] = LanguageMode.Markup, ["xml"] = LanguageMode.Markup, ["svg"] = LanguageMode.Markup,
        ["xaml"] = LanguageMode.Markup, ["vue"] = LanguageMode.Markup, ["csproj"] = LanguageMode.Markup,
        ["css"] = LanguageMode.Css, ["scss"] = LanguageMode.Css, ["less"] = LanguageMode.Css,
        ["md"] = LanguageMode.Markdown, ["markdown"] = LanguageMode.Markdown,
        ["diff"] = LanguageMode.Diff, ["patch"] = LanguageMode.Diff,
        ["ini"] = LanguageMode.Config, ["toml"] = LanguageMode.Config, ["conf"] = LanguageMode.Config,
        ["cfg"] = LanguageMode.Config, ["env"] = LanguageMode.Config, ["properties"] = LanguageMode.Config,
        ["gitignore"] = LanguageMode.Config, ["editorconfig"] = LanguageMode.Config,
    };

    private static readonly HashSet<string> CLikeKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "ctor", "debugger", "decimal", "default", "delegate", "delete", "do",
        "double", "dynamic", "else", "enum", "event", "explicit", "export", "extends", "extern", "false",
        "finally", "fixed", "float", "fn", "for", "foreach", "func", "function", "goto", "if", "implements",
        "implicit", "import", "in", "instanceof", "int", "interface", "internal", "is", "lambda", "let",
        "lock", "long", "loop", "match", "mod", "mut", "namespace", "new", "nil", "not", "null", "object",
        "operator", "out", "override", "package", "params", "private", "protected", "pub", "public", "readonly",
        "record", "ref", "return", "sealed", "self", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "super", "switch", "this", "throw", "throws", "trait", "true", "try", "type", "typeof",
        "uint", "ulong", "unchecked", "union", "unsafe", "ushort", "use", "using", "var", "virtual", "void",
        "volatile", "where", "while", "with", "yield", "closed", "extension", "inheritdoc",
    };

    private static readonly HashSet<string> CLikeTypes = new(StringComparer.Ordinal)
    {
        "List", "Dictionary", "HashSet", "Task", "Span", "ReadOnlySpan", "Memory", "String", "Object",
        "Action", "Func", "Exception", "EventArgs", "IEnumerable", "IReadOnlyList", "IList", "Console",
        "Math", "File", "Path", "Stream", "StringBuilder", "DateTime", "TimeSpan", "Guid", "Regex",
        "ValueTask", "Nullable", "Array", "Tuple", "KeyValuePair", "Uri", "Encoding", "Bitmap", "Font",
    };

    private static readonly HashSet<string> PythonKeywords = new(StringComparer.Ordinal)
    {
        "and", "as", "assert", "async", "await", "break", "class", "continue", "def", "del", "elif", "else",
        "except", "False", "finally", "for", "from", "global", "if", "import", "in", "is", "lambda", "None",
        "nonlocal", "not", "or", "pass", "raise", "return", "True", "try", "while", "with", "yield", "match",
        "case", "self", "cls", "print", "len", "range", "str", "int", "float", "list", "dict", "set", "tuple",
    };

    private static readonly HashSet<string> SqlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "select", "from", "where", "insert", "into", "values", "update", "set", "delete", "create", "table",
        "alter", "drop", "index", "view", "join", "inner", "left", "right", "outer", "full", "on", "group",
        "by", "order", "having", "limit", "offset", "union", "all", "distinct", "as", "and", "or", "not",
        "null", "is", "in", "case", "when", "then", "else", "end", "primary", "key", "foreign", "references",
        "default", "unique", "constraint", "begin", "commit", "rollback", "transaction", "if", "exists",
        "count", "sum", "avg", "min", "max", "int", "varchar", "text", "datetime", "timestamp", "boolean",
        "add", "column", "grant", "revoke", "with", "recursive", "over", "partition", "returning", "using",
    };

    private static readonly HashSet<string> ShellKeywords = new(StringComparer.Ordinal)
    {
        "if", "then", "else", "elif", "fi", "for", "while", "do", "done", "case", "esac", "function",
        "return", "in", "local", "export", "readonly", "declare", "set", "unset", "shift", "exit", "trap",
        "source", "alias", "echo", "cd", "ls", "grep", "sed", "awk", "cat", "sudo", "apt", "systemctl",
        "curl", "wget", "chmod", "chown", "mkdir", "rm", "cp", "mv", "tar", "ssh", "scp", "docker", "git",
        "python", "python3", "pip", "node", "npm", "pwsh", "bash", "true", "false", "test",
    };

    private static readonly HashSet<string> PowerShellKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "else", "elseif", "switch", "foreach", "for", "while", "do", "until", "function", "param",
        "begin", "process", "end", "try", "catch", "finally", "throw", "return", "break", "continue",
        "filter", "class", "enum", "using", "in", "trap", "exit", "PSCustomObject", "Get-ChildItem",
        "Get-Content", "Set-Content", "Write-Host", "Remove-Item", "New-Item", "Test-Path", "Select-Object",
        "Where-Object", "ForEach-Object", "Out-Null", "Join-Path", "Resolve-Path",
    };

    public static LanguageMode Resolve(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return LanguageMode.None;
        }

        string key = language.Trim().TrimStart('{').TrimEnd('}').ToLowerInvariant();
        return Aliases.TryGetValue(key, out LanguageMode mode) ? mode : LanguageMode.None;
    }

    /// <summary>Human name of the language for the code-card header.</summary>
    public static string DisplayName(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "text" : language.Trim().ToLowerInvariant();

    public static List<Token> Tokenize(string text, LanguageMode mode, HighlightState state) => mode switch
    {
        LanguageMode.CLike => ScanCLike(text, state),
        LanguageMode.Python => ScanPython(text, state),
        LanguageMode.Shell => ScanShell(text, state, PowerShellKeywords, '$'),
        LanguageMode.PowerShell => ScanShell(text, state, PowerShellKeywords, '$'),
        LanguageMode.Sql => ScanSql(text),
        LanguageMode.Json => ScanJson(text),
        LanguageMode.Yaml => ScanYaml(text),
        LanguageMode.Markup => ScanMarkup(text, state),
        LanguageMode.Css => ScanCss(text),
        LanguageMode.Markdown => ScanMarkdown(text),
        LanguageMode.Diff => ScanDiff(text),
        LanguageMode.Config => ScanConfig(text),
        _ => [],
    };

    // ------------------------------------------------------------------ C-like

    private static List<Token> ScanCLike(string text, HighlightState state)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < text.Length)
        {
            if (state.BlockComment)
            {
                int end = text.IndexOf("*/", i, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new Token(TokenKind.Comment, i, text.Length - i));
                    return tokens;
                }

                tokens.Add(new Token(TokenKind.Comment, i, end + 2 - i));
                i = end + 2;
                state.BlockComment = false;
                continue;
            }

            char c = text[i];

            if (c is '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                int end = text.IndexOf('\n', i);
                end = end < 0 ? text.Length : end;
                tokens.Add(new Token(TokenKind.Comment, i, end - i));
                i = end;
                continue;
            }

            if (c is '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new Token(TokenKind.Comment, i, text.Length - i));
                    state.BlockComment = true;
                    return tokens;
                }

                tokens.Add(new Token(TokenKind.Comment, i, end + 2 - i));
                i = end + 2;
                continue;
            }

            if (c is '"' or '\'' or '`')
            {
                int start = i;
                char quote = c;
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }

                    if (text[i] == quote)
                    {
                        i++;
                        break;
                    }

                    if (text[i] == '\n' && quote != '`')
                    {
                        break;
                    }

                    i++;
                }

                tokens.Add(new Token(TokenKind.String, start, Math.Min(i, text.Length) - start));
                continue;
            }

            if (char.IsDigit(c) || c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '.' || text[i] == '_'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Number, start, i - start));
                continue;
            }

            if (char.IsLetter(c) || c == '_' || c == '@' || c == '$')
            {
                int start = i;
                if (c is '@' or '$')
                {
                    i++;
                }

                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                string word = text[start..i];
                string bare = word.TrimStart('@', '$');
                int probe = i;
                while (probe < text.Length && text[probe] == ' ')
                {
                    probe++;
                }

                bool isCall = probe < text.Length && text[probe] == '(';
                TokenKind kind = CLikeKeywords.Contains(bare)
                    ? TokenKind.Keyword
                    : CLikeTypes.Contains(bare)
                        ? TokenKind.Type
                        : isCall && bare.Length > 0
                            ? TokenKind.Function
                            : TokenKind.Plain;

                tokens.Add(new Token(kind, start, i - start));
                continue;
            }

            if (c is ';' or ',' or '.' or '(' or ')' or '{' or '}' or '[' or ']' or '+' or '-' or '*' or '/' or '%' or '=' or '<' or '>' or '!' or '&' or '|' or '?' or ':' or '~' or '^')
            {
                int start = i;
                while (i < text.Length && text[i] is ';' or ',' or '.' or '(' or ')' or '{' or '}' or '[' or ']' or '+' or '-' or '*' or '/' or '%' or '=' or '<' or '>' or '!' or '&' or '|' or '?' or ':' or '~' or '^')
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Punctuation, start, i - start));
                continue;
            }

            i++;
        }

        return tokens;
    }

    // ------------------------------------------------------------------ python

    private static List<Token> ScanPython(string text, HighlightState state)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < text.Length)
        {
            if (state.TripleString)
            {
                int end = text.IndexOf("\"\"\"", i, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new Token(TokenKind.String, i, text.Length - i));
                    return tokens;
                }

                tokens.Add(new Token(TokenKind.String, i, end + 3 - i));
                i = end + 3;
                state.TripleString = false;
                continue;
            }

            char c = text[i];

            if (c == '#')
            {
                int end = text.IndexOf('\n', i);
                end = end < 0 ? text.Length : end;
                tokens.Add(new Token(TokenKind.Comment, i, end - i));
                i = end;
                continue;
            }

            if (c is '"' or '\'')
            {
                if (i + 2 < text.Length && text[i + 1] == c && text[i + 2] == c)
                {
                    int end = text.IndexOf(new string(c, 3), i + 3, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        tokens.Add(new Token(TokenKind.String, i, text.Length - i));
                        state.TripleString = true;
                        return tokens;
                    }

                    tokens.Add(new Token(TokenKind.String, i, end + 3 - i));
                    i = end + 3;
                    continue;
                }

                int start = i;
                char quote = c;
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }

                    if (text[i] == quote)
                    {
                        i++;
                        break;
                    }

                    if (text[i] == '\n')
                    {
                        break;
                    }

                    i++;
                }

                tokens.Add(new Token(TokenKind.String, start, Math.Min(i, text.Length) - start));
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '.' || text[i] == '_'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Number, start, i - start));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                string word = text[start..i];
                int probe = i;
                while (probe < text.Length && text[probe] == ' ')
                {
                    probe++;
                }

                TokenKind kind = PythonKeywords.Contains(word)
                    ? TokenKind.Keyword
                    : probe < text.Length && text[probe] == '('
                        ? TokenKind.Function
                        : char.IsUpper(word[0]) && word.Length > 1
                            ? TokenKind.Type
                            : TokenKind.Plain;

                tokens.Add(new Token(kind, start, i - start));
                continue;
            }

            if (c is '@' && i + 1 < text.Length && char.IsLetter(text[i + 1]))
            {
                int start = i;
                i++;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '.'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Attribute, start, i - start));
                continue;
            }

            i++;
        }

        return tokens;
    }

    // ------------------------------------------------------------------- shell

    private static List<Token> ScanShell(string text, HighlightState state, HashSet<string> keywords, char variablePrefix)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];

            if (c == '#')
            {
                bool atStart = i == 0 || char.IsWhiteSpace(text[i - 1]);
                if (atStart)
                {
                    int end = text.IndexOf('\n', i);
                    end = end < 0 ? text.Length : end;
                    tokens.Add(new Token(TokenKind.Comment, i, end - i));
                    i = end;
                    continue;
                }
            }

            if (c is '"' or '\'')
            {
                int start = i;
                char quote = c;
                i++;
                while (i < text.Length && text[i] != quote)
                {
                    if (quote == '"' && text[i] == '\\')
                    {
                        i++;
                    }

                    i++;
                }

                i = Math.Min(i + 1, text.Length);
                tokens.Add(new Token(TokenKind.String, start, i - start));
                continue;
            }

            if (c == variablePrefix && i + 1 < text.Length)
            {
                int start = i;
                i++;
                if (text[i] == '{')
                {
                    int close = text.IndexOf('}', i);
                    i = close < 0 ? text.Length : close + 1;
                }
                else
                {
                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or ':' or '-'))
                    {
                        i++;
                    }
                }

                tokens.Add(new Token(TokenKind.Property, start, i - start));
                continue;
            }

            if (char.IsLetter(c) || c is '/' or '.' or '-')
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '/' or '.' or '-' or '_'))
                {
                    i++;
                }

                string word = text[start..i];
                if (keywords.Contains(word) || keywords.Contains(word.TrimStart('-', '/')))
                {
                    tokens.Add(new Token(TokenKind.Keyword, start, i - start));
                }

                continue;
            }

            i++;
        }

        return tokens;
    }

    // --------------------------------------------------------------------- sql

    private static List<Token> ScanSql(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];

            if (c is '-' && i + 1 < text.Length && text[i + 1] == '-')
            {
                int end = text.IndexOf('\n', i);
                end = end < 0 ? text.Length : end;
                tokens.Add(new Token(TokenKind.Comment, i, end - i));
                i = end;
                continue;
            }

            if (c == '\'')
            {
                int start = i;
                i++;
                while (i < text.Length && text[i] != '\'')
                {
                    i++;
                }

                i = Math.Min(i + 1, text.Length);
                tokens.Add(new Token(TokenKind.String, start, i - start));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                string word = text[start..i];
                tokens.Add(new Token(SqlKeywords.Contains(word) ? TokenKind.Keyword : TokenKind.Plain, start, i - start));
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Number, start, i - start));
                continue;
            }

            i++;
        }

        return tokens;
    }

    // -------------------------------------------------------------------- json

    private static List<Token> ScanJson(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];

            if (c == '"')
            {
                int start = i;
                i++;
                while (i < text.Length && text[i] != '"')
                {
                    if (text[i] == '\\')
                    {
                        i++;
                    }

                    i++;
                }

                i = Math.Min(i + 1, text.Length);
                int probe = i;
                while (probe < text.Length && char.IsWhiteSpace(text[probe]))
                {
                    probe++;
                }

                bool isKey = probe < text.Length && text[probe] == ':';
                tokens.Add(new Token(isKey ? TokenKind.Property : TokenKind.String, start, i - start));
                continue;
            }

            if (char.IsDigit(c) || c == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                int start = i;
                i++;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] is '.' or 'e' or 'E' or '+' or '-'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Number, start, i - start));
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < text.Length && char.IsLetter(text[i]))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Keyword, start, i - start));
                continue;
            }

            i++;
        }

        return tokens;
    }

    // -------------------------------------------------------------------- yaml

    private static List<Token> ScanYaml(string text)
    {
        var tokens = new List<Token>();
        int lineStart = 0;
        while (lineStart <= text.Length)
        {
            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            int i = lineStart;
            while (i < lineEnd && text[i] == ' ')
            {
                i++;
            }

            if (i < lineEnd && text[i] == '#')
            {
                tokens.Add(new Token(TokenKind.Comment, i, lineEnd - i));
                lineStart = lineEnd + 1;
                continue;
            }

            if (i < lineEnd && text[i] == '-' && (i + 1 >= lineEnd || text[i + 1] == ' '))
            {
                tokens.Add(new Token(TokenKind.Punctuation, i, 1));
                i++;
                while (i < lineEnd && text[i] == ' ')
                {
                    i++;
                }
            }

            int colon = text.IndexOf(':', i);
            if (colon > i && colon < lineEnd)
            {
                tokens.Add(new Token(TokenKind.Property, i, colon - i));
                int valueStart = colon + 1;
                while (valueStart < lineEnd && text[valueStart] == ' ')
                {
                    valueStart++;
                }

                if (valueStart < lineEnd)
                {
                    char first = text[valueStart];
                    if (first is '"' or '\'')
                    {
                        int close = text.IndexOf(first, valueStart + 1);
                        close = close < 0 || close > lineEnd ? lineEnd : close + 1;
                        tokens.Add(new Token(TokenKind.String, valueStart, close - valueStart));
                    }
                    else if (char.IsDigit(first))
                    {
                        tokens.Add(new Token(TokenKind.Number, valueStart, lineEnd - valueStart));
                    }
                    else if (first is 't' or 'f' or 'n' or 'y')
                    {
                        string word = text[valueStart..lineEnd].Trim();
                        if (word is "true" or "false" or "null" or "yes" or "no" or "~")
                        {
                            tokens.Add(new Token(TokenKind.Keyword, valueStart, lineEnd - valueStart));
                        }
                    }
                }
            }

            lineStart = lineEnd + 1;
        }

        return tokens;
    }

    // ------------------------------------------------------------------ markup

    private static List<Token> ScanMarkup(string text, HighlightState state)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            if (state.BlockComment)
            {
                int end = text.IndexOf("-->", i, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new Token(TokenKind.Comment, i, text.Length - i));
                    return tokens;
                }

                tokens.Add(new Token(TokenKind.Comment, i, end + 3 - i));
                i = end + 3;
                state.BlockComment = false;
                continue;
            }

            if (text.AsSpan(i).StartsWith("<!--"))
            {
                int end = text.IndexOf("-->", i, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new Token(TokenKind.Comment, i, text.Length - i));
                    state.BlockComment = true;
                    return tokens;
                }

                tokens.Add(new Token(TokenKind.Comment, i, end + 3 - i));
                i = end + 3;
                continue;
            }

            if (text[i] == '<')
            {
                int tagEnd = text.IndexOf('>', i);
                if (tagEnd < 0)
                {
                    tagEnd = text.Length - 1;
                }

                int j = i;
                tokens.Add(new Token(TokenKind.Punctuation, j, 1));
                j++;
                if (j < text.Length && text[j] == '/')
                {
                    j++;
                }

                int nameStart = j;
                while (j <= tagEnd && (char.IsLetterOrDigit(text[j]) || text[j] is '-' or ':'))
                {
                    j++;
                }

                if (j > nameStart)
                {
                    tokens.Add(new Token(TokenKind.Tag, nameStart, j - nameStart));
                }

                while (j <= tagEnd && j < text.Length)
                {
                    char c = text[j];
                    if (c is '=' || c is '>' or '/' or '"')
                    {
                        if (c is '"' or '\'')
                        {
                            int close = text.IndexOf(c, j + 1);
                            close = close < 0 ? tagEnd : close;
                            tokens.Add(new Token(TokenKind.String, j, close + 1 - j));
                            j = close + 1;
                            continue;
                        }

                        j++;
                        continue;
                    }

                    if (char.IsLetter(c))
                    {
                        int start = j;
                        while (j <= tagEnd && j < text.Length && (char.IsLetterOrDigit(text[j]) || text[j] is '-' or ':' or '.'))
                        {
                            j++;
                        }

                        tokens.Add(new Token(TokenKind.Attribute, start, j - start));
                        continue;
                    }

                    j++;
                }

                i = Math.Min(tagEnd + 1, text.Length);
                continue;
            }

            i++;
        }

        return tokens;
    }

    // --------------------------------------------------------------------- css

    private static List<Token> ScanCss(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c is '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? text.Length : end + 2;
                tokens.Add(new Token(TokenKind.Comment, i, end - i));
                i = end;
                continue;
            }

            if (c is '"' or '\'')
            {
                int start = i;
                int close = text.IndexOf(c, i + 1);
                i = close < 0 ? text.Length : close + 1;
                tokens.Add(new Token(TokenKind.String, start, i - start));
                continue;
            }

            if (char.IsLetter(c) || c is '-' or '.' or '#')
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '-' or '_' or '.' or '#' or '@'))
                {
                    i++;
                }

                int probe = i;
                while (probe < text.Length && char.IsWhiteSpace(text[probe]))
                {
                    probe++;
                }

                bool isProperty = probe < text.Length && text[probe] == ':';
                tokens.Add(new Token(isProperty ? TokenKind.Property : TokenKind.Tag, start, i - start));
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '.'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Number, start, i - start));
                continue;
            }

            i++;
        }

        return tokens;
    }

    // ---------------------------------------------------------------- markdown

    private static List<Token> ScanMarkdown(string text)
    {
        var tokens = new List<Token>();
        int lineStart = 0;
        while (lineStart <= text.Length)
        {
            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            int i = lineStart;
            while (i < lineEnd && text[i] == ' ')
            {
                i++;
            }

            if (i < lineEnd && text[i] == '#')
            {
                tokens.Add(new Token(TokenKind.Keyword, i, lineEnd - i));
            }
            else if (i < lineEnd && (text[i] is '-' or '*' or '+') && i + 1 < lineEnd && text[i + 1] == ' ')
            {
                tokens.Add(new Token(TokenKind.Punctuation, i, 1));
            }
            else if (i < lineEnd && text[i] == '>')
            {
                tokens.Add(new Token(TokenKind.Comment, i, lineEnd - i));
            }

            for (int j = lineStart; j < lineEnd - 1; j++)
            {
                if (text[j] == '`')
                {
                    int close = text.IndexOf('`', j + 1);
                    if (close > j && close <= lineEnd)
                    {
                        tokens.Add(new Token(TokenKind.String, j, close - j + 1));
                        j = close;
                    }
                }
                else if ((text[j] == '*' || text[j] == '_') && j + 1 < lineEnd && text[j + 1] == text[j])
                {
                    int close = text.IndexOf(new string(text[j], 2), j + 2, StringComparison.Ordinal);
                    if (close > 0 && close < lineEnd)
                    {
                        tokens.Add(new Token(TokenKind.Type, j, close - j + 2));
                        j = close + 1;
                    }
                }
            }

            lineStart = lineEnd + 1;
        }

        return tokens;
    }

    // -------------------------------------------------------------------- diff

    private static List<Token> ScanDiff(string text)
    {
        var tokens = new List<Token>();
        int lineStart = 0;
        while (lineStart < text.Length)
        {
            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            if (lineEnd > lineStart)
            {
                char first = text[lineStart];
                TokenKind kind = first switch
                {
                    '+' => TokenKind.String,
                    '-' => TokenKind.Attribute,
                    '@' => TokenKind.Keyword,
                    '#' or 'd' or 'i' => TokenKind.Comment,
                    _ => TokenKind.Plain,
                };

                if (kind != TokenKind.Plain)
                {
                    tokens.Add(new Token(kind, lineStart, lineEnd - lineStart));
                }
            }

            lineStart = lineEnd + 1;
        }

        return tokens;
    }

    // ------------------------------------------------------------------ config

    private static List<Token> ScanConfig(string text)
    {
        var tokens = new List<Token>();
        int lineStart = 0;
        while (lineStart <= text.Length)
        {
            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            int i = lineStart;
            while (i < lineEnd && text[i] == ' ')
            {
                i++;
            }

            if (i < lineEnd && (text[i] == '#' || text[i] == ';'))
            {
                tokens.Add(new Token(TokenKind.Comment, i, lineEnd - i));
                lineStart = lineEnd + 1;
                continue;
            }

            if (i < lineEnd && text[i] == '[')
            {
                tokens.Add(new Token(TokenKind.Keyword, i, lineEnd - i));
                lineStart = lineEnd + 1;
                continue;
            }

            int separator = text.IndexOfAny(['=', ':', ' '], i, Math.Max(0, lineEnd - i));
            if (separator > i)
            {
                tokens.Add(new Token(TokenKind.Property, i, separator - i));
                int valueStart = separator + 1;
                while (valueStart < lineEnd && text[valueStart] == ' ')
                {
                    valueStart++;
                }

                if (valueStart < lineEnd)
                {
                    tokens.Add(new Token(TokenKind.String, valueStart, lineEnd - valueStart));
                }
            }

            lineStart = lineEnd + 1;
        }

        return tokens;
    }
}


