namespace MdViewer.Core.Formulas;

/// <summary>Command tables: symbols, function names, accents and environment fences.</summary>
public static class MathSymbols
{
    private static readonly Dictionary<string, (string Text, MathAtomKind Kind)> Table = new(StringComparer.Ordinal)
    {
        // Greek, lowercase
        ["alpha"] = ("α", MathAtomKind.Ordinary), ["beta"] = ("β", MathAtomKind.Ordinary),
        ["gamma"] = ("γ", MathAtomKind.Ordinary), ["delta"] = ("δ", MathAtomKind.Ordinary),
        ["epsilon"] = ("ϵ", MathAtomKind.Ordinary), ["varepsilon"] = ("ε", MathAtomKind.Ordinary),
        ["zeta"] = ("ζ", MathAtomKind.Ordinary), ["eta"] = ("η", MathAtomKind.Ordinary),
        ["theta"] = ("θ", MathAtomKind.Ordinary), ["vartheta"] = ("ϑ", MathAtomKind.Ordinary),
        ["iota"] = ("ι", MathAtomKind.Ordinary), ["kappa"] = ("κ", MathAtomKind.Ordinary),
        ["lambda"] = ("λ", MathAtomKind.Ordinary), ["mu"] = ("μ", MathAtomKind.Ordinary),
        ["nu"] = ("ν", MathAtomKind.Ordinary), ["xi"] = ("ξ", MathAtomKind.Ordinary),
        ["pi"] = ("π", MathAtomKind.Ordinary), ["varpi"] = ("ϖ", MathAtomKind.Ordinary),
        ["rho"] = ("ρ", MathAtomKind.Ordinary), ["varrho"] = ("ϱ", MathAtomKind.Ordinary),
        ["sigma"] = ("σ", MathAtomKind.Ordinary), ["varsigma"] = ("ς", MathAtomKind.Ordinary),
        ["tau"] = ("τ", MathAtomKind.Ordinary), ["upsilon"] = ("υ", MathAtomKind.Ordinary),
        ["phi"] = ("ϕ", MathAtomKind.Ordinary), ["varphi"] = ("φ", MathAtomKind.Ordinary),
        ["chi"] = ("χ", MathAtomKind.Ordinary), ["psi"] = ("ψ", MathAtomKind.Ordinary),
        ["omega"] = ("ω", MathAtomKind.Ordinary),
        // Greek, uppercase (upright by convention)
        ["Gamma"] = ("Γ", MathAtomKind.Ordinary), ["Delta"] = ("Δ", MathAtomKind.Ordinary),
        ["Theta"] = ("Θ", MathAtomKind.Ordinary), ["Lambda"] = ("Λ", MathAtomKind.Ordinary),
        ["Xi"] = ("Ξ", MathAtomKind.Ordinary), ["Pi"] = ("Π", MathAtomKind.Ordinary),
        ["Sigma"] = ("Σ", MathAtomKind.Ordinary), ["Upsilon"] = ("Υ", MathAtomKind.Ordinary),
        ["Phi"] = ("Φ", MathAtomKind.Ordinary), ["Psi"] = ("Ψ", MathAtomKind.Ordinary),
        ["Omega"] = ("Ω", MathAtomKind.Ordinary),
        // Binary operators
        ["pm"] = ("±", MathAtomKind.Binary), ["mp"] = ("∓", MathAtomKind.Binary),
        ["times"] = ("×", MathAtomKind.Binary), ["div"] = ("÷", MathAtomKind.Binary),
        ["cdot"] = ("⋅", MathAtomKind.Binary), ["ast"] = ("∗", MathAtomKind.Binary),
        ["star"] = ("⋆", MathAtomKind.Binary), ["circ"] = ("∘", MathAtomKind.Binary),
        ["bullet"] = ("∙", MathAtomKind.Binary), ["oplus"] = ("⊕", MathAtomKind.Binary),
        ["ominus"] = ("⊖", MathAtomKind.Binary), ["otimes"] = ("⊗", MathAtomKind.Binary),
        ["oslash"] = ("⊘", MathAtomKind.Binary), ["odot"] = ("⊙", MathAtomKind.Binary),
        ["cup"] = ("∪", MathAtomKind.Binary), ["cap"] = ("∩", MathAtomKind.Binary),
        ["setminus"] = ("∖", MathAtomKind.Binary), ["wedge"] = ("∧", MathAtomKind.Binary),
        ["land"] = ("∧", MathAtomKind.Binary), ["vee"] = ("∨", MathAtomKind.Binary),
        ["lor"] = ("∨", MathAtomKind.Binary), ["dagger"] = ("†", MathAtomKind.Binary),
        ["ddagger"] = ("‡", MathAtomKind.Binary), ["uplus"] = ("⊎", MathAtomKind.Binary),
        ["sqcup"] = ("⊔", MathAtomKind.Binary), ["sqcap"] = ("⊓", MathAtomKind.Binary),
        // Relations
        ["le"] = ("≤", MathAtomKind.Relation), ["leq"] = ("≤", MathAtomKind.Relation),
        ["ge"] = ("≥", MathAtomKind.Relation), ["geq"] = ("≥", MathAtomKind.Relation),
        ["ne"] = ("≠", MathAtomKind.Relation), ["neq"] = ("≠", MathAtomKind.Relation),
        ["approx"] = ("≈", MathAtomKind.Relation), ["equiv"] = ("≡", MathAtomKind.Relation),
        ["cong"] = ("≅", MathAtomKind.Relation), ["sim"] = ("∼", MathAtomKind.Relation),
        ["simeq"] = ("≃", MathAtomKind.Relation), ["propto"] = ("∝", MathAtomKind.Relation),
        ["ll"] = ("≪", MathAtomKind.Relation), ["gg"] = ("≫", MathAtomKind.Relation),
        ["prec"] = ("≺", MathAtomKind.Relation), ["succ"] = ("≻", MathAtomKind.Relation),
        ["subset"] = ("⊂", MathAtomKind.Relation), ["supset"] = ("⊃", MathAtomKind.Relation),
        ["subseteq"] = ("⊆", MathAtomKind.Relation), ["supseteq"] = ("⊇", MathAtomKind.Relation),
        ["in"] = ("∈", MathAtomKind.Relation), ["notin"] = ("∉", MathAtomKind.Relation),
        ["ni"] = ("∋", MathAtomKind.Relation), ["perp"] = ("⊥", MathAtomKind.Relation),
        ["parallel"] = ("∥", MathAtomKind.Relation), ["mid"] = ("∣", MathAtomKind.Relation),
        ["models"] = ("⊨", MathAtomKind.Relation), ["vdash"] = ("⊢", MathAtomKind.Relation),
        // Arrows
        ["to"] = ("→", MathAtomKind.Relation), ["rightarrow"] = ("→", MathAtomKind.Relation),
        ["leftarrow"] = ("←", MathAtomKind.Relation), ["gets"] = ("←", MathAtomKind.Relation),
        ["leftrightarrow"] = ("↔", MathAtomKind.Relation),
        ["Rightarrow"] = ("⇒", MathAtomKind.Relation), ["Leftarrow"] = ("⇐", MathAtomKind.Relation),
        ["Leftrightarrow"] = ("⇔", MathAtomKind.Relation),
        ["longrightarrow"] = ("⟶", MathAtomKind.Relation), ["longleftarrow"] = ("⟵", MathAtomKind.Relation),
        ["mapsto"] = ("↦", MathAtomKind.Relation), ["longmapsto"] = ("⟼", MathAtomKind.Relation),
        ["uparrow"] = ("↑", MathAtomKind.Relation), ["downarrow"] = ("↓", MathAtomKind.Relation),
        ["updownarrow"] = ("↕", MathAtomKind.Relation),
        ["nearrow"] = ("↗", MathAtomKind.Relation), ["searrow"] = ("↘", MathAtomKind.Relation),
        ["rightleftharpoons"] = ("⇌", MathAtomKind.Relation),
        // Large operators
        ["sum"] = ("∑", MathAtomKind.LargeOperator), ["prod"] = ("∏", MathAtomKind.LargeOperator),
        ["coprod"] = ("∐", MathAtomKind.LargeOperator), ["int"] = ("∫", MathAtomKind.LargeOperator),
        ["iint"] = ("∬", MathAtomKind.LargeOperator), ["iiint"] = ("∭", MathAtomKind.LargeOperator),
        ["oint"] = ("∮", MathAtomKind.LargeOperator), ["bigcup"] = ("⋃", MathAtomKind.LargeOperator),
        ["bigcap"] = ("⋂", MathAtomKind.LargeOperator), ["bigoplus"] = ("⨁", MathAtomKind.LargeOperator),
        ["bigotimes"] = ("⨂", MathAtomKind.LargeOperator), ["bigwedge"] = ("⋀", MathAtomKind.LargeOperator),
        ["bigvee"] = ("⋁", MathAtomKind.LargeOperator),
        // Misc ordinary symbols
        ["infty"] = ("∞", MathAtomKind.Ordinary), ["partial"] = ("∂", MathAtomKind.Ordinary),
        ["nabla"] = ("∇", MathAtomKind.Ordinary), ["forall"] = ("∀", MathAtomKind.Ordinary),
        ["exists"] = ("∃", MathAtomKind.Ordinary), ["nexists"] = ("∄", MathAtomKind.Ordinary),
        ["neg"] = ("¬", MathAtomKind.Ordinary), ["lnot"] = ("¬", MathAtomKind.Ordinary),
        ["emptyset"] = ("∅", MathAtomKind.Ordinary), ["varnothing"] = ("∅", MathAtomKind.Ordinary),
        ["aleph"] = ("ℵ", MathAtomKind.Ordinary), ["hbar"] = ("ℏ", MathAtomKind.Ordinary),
        ["ell"] = ("ℓ", MathAtomKind.Ordinary), ["Re"] = ("ℜ", MathAtomKind.Ordinary),
        ["Im"] = ("ℑ", MathAtomKind.Ordinary), ["wp"] = ("℘", MathAtomKind.Ordinary),
        ["angle"] = ("∠", MathAtomKind.Ordinary), ["triangle"] = ("△", MathAtomKind.Ordinary),
        ["square"] = ("□", MathAtomKind.Ordinary), ["blacksquare"] = ("■", MathAtomKind.Ordinary),
        ["prime"] = ("′", MathAtomKind.Ordinary), ["degree"] = ("°", MathAtomKind.Ordinary),
        ["therefore"] = ("∴", MathAtomKind.Ordinary), ["because"] = ("∵", MathAtomKind.Ordinary),
        ["ldots"] = ("…", MathAtomKind.Ordinary), ["dots"] = ("…", MathAtomKind.Ordinary),
        ["cdots"] = ("⋯", MathAtomKind.Ordinary), ["vdots"] = ("⋮", MathAtomKind.Ordinary),
        ["ddots"] = ("⋱", MathAtomKind.Ordinary),
        ["S"] = ("§", MathAtomKind.Ordinary), ["P"] = ("¶", MathAtomKind.Ordinary),
        ["copyright"] = ("©", MathAtomKind.Ordinary), ["checkmark"] = ("✓", MathAtomKind.Ordinary),
        ["lnotx"] = ("¬", MathAtomKind.Ordinary),
        // Punctuation and fences
        ["{"] = ("{", MathAtomKind.Open), ["}"] = ("}", MathAtomKind.Close),
        ["lbrace"] = ("{", MathAtomKind.Open), ["rbrace"] = ("}", MathAtomKind.Close),
        ["langle"] = ("⟨", MathAtomKind.Open), ["rangle"] = ("⟩", MathAtomKind.Close),
        ["lfloor"] = ("⌊", MathAtomKind.Open), ["rfloor"] = ("⌋", MathAtomKind.Close),
        ["lceil"] = ("⌈", MathAtomKind.Open), ["rceil"] = ("⌉", MathAtomKind.Close),
        ["lvert"] = ("|", MathAtomKind.Open), ["rvert"] = ("|", MathAtomKind.Close),
        ["lVert"] = ("‖", MathAtomKind.Open), ["rVert"] = ("‖", MathAtomKind.Close),
        ["vert"] = ("|", MathAtomKind.Ordinary), ["Vert"] = ("‖", MathAtomKind.Ordinary),
        ["backslash"] = ("\\", MathAtomKind.Ordinary), ["#"] = ("#", MathAtomKind.Ordinary),
        ["&"] = ("&", MathAtomKind.Ordinary), ["%"] = ("%", MathAtomKind.Ordinary),
        ["_"] = ("_", MathAtomKind.Ordinary), ["$"] = ("$", MathAtomKind.Ordinary),
        ["|"] = ("‖", MathAtomKind.Ordinary), [":"] = (":", MathAtomKind.Punctuation),
    };

    private static readonly HashSet<string> Functions = new(StringComparer.Ordinal)
    {
        "sin", "cos", "tan", "cot", "sec", "csc", "arcsin", "arccos", "arctan",
        "sinh", "cosh", "tanh", "coth", "log", "ln", "lg", "exp", "det", "dim", "ker",
        "deg", "gcd", "hom", "arg", "lim", "liminf", "limsup", "max", "min", "sup", "inf",
        "Pr", "mod", "bmod",
    };

    /// <summary>Limits are set under a display operator: \\lim, \\max, \\sum …</summary>
    private static readonly HashSet<string> LimitOperators = new(StringComparer.Ordinal)
    {
        "lim", "liminf", "limsup", "max", "min", "sup", "inf", "det", "gcd", "Pr",
    };

    private static readonly Dictionary<string, string> Accents = new(StringComparer.Ordinal)
    {
        ["hat"] = "\u02C6",
        ["widehat"] = "\u02C6",
        ["bar"] = "\u00AF",
        ["overline"] = "\u00AF",
        ["vec"] = "\u20D7",
        ["overrightarrow"] = "\u20D7",
        ["dot"] = "\u02D9",
        ["ddot"] = "\u00A8",
        ["tilde"] = "\u02DC",
        ["widetilde"] = "\u02DC",
        ["check"] = "\u02C7",
        ["breve"] = "\u02D8",
        ["acute"] = "\u00B4",
        ["grave"] = "\u0060",
    };

    public static bool TryGet(string command, out string text, out MathAtomKind kind)
    {
        if (Table.TryGetValue(command, out (string Text, MathAtomKind Kind) entry))
        {
            text = entry.Text;
            kind = entry.Kind;
            return true;
        }

        text = string.Empty;
        kind = MathAtomKind.Ordinary;
        return false;
    }

    public static bool IsFunction(string command) => Functions.Contains(command);

    public static bool TakesLimits(string command) => LimitOperators.Contains(command);

    public static bool TryGetAccent(string command, out string mark, out bool under)
    {
        under = command is "underline";
        if (under)
        {
            mark = "\u005F";
            return true;
        }

        return Accents.TryGetValue(command, out mark!);
    }

    /// <summary>Fences used by \\left … \\right and by the matrix environments.</summary>
    public static bool IsDelimiter(string text) => text is
        "(" or ")" or "[" or "]" or "{" or "}" or "|" or "‖" or "/" or "\\" or
        "⟨" or "⟩" or "⌊" or "⌋" or "⌈" or "⌉" or "." or "" or "↑" or "↓" or "↕" or "⇑" or "⇓" or "||";

    public static string Blackboard(char c) => c switch
    {
        >= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D538 + (c - 'A')),
        >= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D552 + (c - 'a')),
        >= '0' and <= '9' => char.ConvertFromUtf32(0x1D7D8 + (c - '0')),
        _ => c.ToString(),
    };

    public static string Script(char c) => c switch
    {
        >= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D49C + (c - 'A')),
        >= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D4B6 + (c - 'a')),
        _ => c.ToString(),
    };

    public static string Fraktur(char c) => c switch
    {
        >= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D504 + (c - 'A')),
        >= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D51E + (c - 'a')),
        _ => c.ToString(),
    };

    /// <summary>Matrix-like environments: fence pair and whether cells are left aligned.</summary>
    public static bool TryGetEnvironment(string name, out string open, out string close, out bool leftAligned)
    {
        leftAligned = false;
        (open, close) = name switch
        {
            "matrix" => ("", ""),
            "smallmatrix" => ("", ""),
            "pmatrix" => ("(", ")"),
            "bmatrix" => ("[", "]"),
            "Bmatrix" => ("{", "}"),
            "vmatrix" => ("|", "|"),
            "Vmatrix" => ("‖", "‖"),
            "cases" => ("{", ""),
            "rcases" => ("", "}"),
            "aligned" => ("", ""),
            "align" or "align*" => ("", ""),
            "gather" or "gather*" => ("", ""),
            "equation" or "equation*" => ("", ""),
            "array" => ("", ""),
            _ => (null!, null!),
        };

        leftAligned = name is "cases" or "rcases" or "aligned" or "align" or "align*" or "array";
        return open is not null;
    }
}
